#include <ArduinoBLE.h>
#include "MahonyAHRS.h"
#include "LSM6DS3.h"
#include "Wire.h"

struct __attribute__((packed)) ImuPacket {
    uint32_t timestampMs;
    int16_t ax, ay, az;
    int16_t gx, gy, gz;
    int16_t qw, qx, qy, qz;
};
static_assert(sizeof(ImuPacket) == 24, "Unexpected telemetry packet size");

constexpr unsigned long sampleIntervalUs = 5000;
constexpr unsigned long ledBlinkIntervalMs = 500;
constexpr unsigned long idleTimeoutMs = 180000;
constexpr unsigned long motionCheckIntervalMs = 100;
constexpr unsigned long batteryUpdateIntervalMs = 30000;
constexpr uint8_t odrMask = 0xF0;
constexpr uint8_t accel12_5Hz = 0x10;

#if defined(VBAT_ENABLE) && defined(PIN_VBAT)
constexpr auto batteryEnablePin = VBAT_ENABLE;
constexpr auto batteryReadPin = PIN_VBAT;
#define MOTIONLINK_BATTERY_ADC 1
#elif defined(ARDUINO_SEEED_XIAO_NRF52840_SENSE)
constexpr auto batteryEnablePin = P0_14;
constexpr auto batteryReadPin = P0_31;
#define MOTIONLINK_BATTERY_ADC 1
#endif

struct Acceleration {
    float x, y, z;
};

Acceleration stillAcceleration;
bool hasStillAcceleration = false;
bool idleSleep = false;
bool batteryServiceAvailable = false;
unsigned long lastMotionMs;
unsigned long lastMotionCheckMs;
unsigned long lastBatteryUpdateMs;
uint8_t activeAccelControl;
uint8_t activeGyroControl;
bool canChangeImuRate = false;
unsigned long lastSampleUs;
unsigned long lastLedToggleMs;
bool isRecording = false;
bool recordingRequested = false;
Mahony filter;
LSM6DS3 imu(I2C_MODE, 0x6A);

BLEService motionLinkService("19B10000-E8F2-537E-4F6C-D104768A1214");
BLECharacteristic dataChar("36151377-DDD7-4C2F-9207-0724C52C3CCC", BLENotify, sizeof(ImuPacket));
BLEByteCharacteristic switchCharacteristic("FB7A8DB8-DE34-4C6F-9F98-D1612EE35441", BLEWrite | BLERead);

BLEService batteryService("180F");
BLEByteCharacteristic batteryLevel("2A19", BLERead);

int readBatteryPercent() {
#if defined(MOTIONLINK_BATTERY_ADC)
    digitalWrite(batteryEnablePin, LOW);
    delay(12);
    analogRead(batteryReadPin); // Discard the first conversion after enabling the divider.
    int total = 0;
    for (int sample = 0; sample < 8; ++sample) {
        int reading = analogRead(batteryReadPin);
        if (reading < 0) {
            digitalWrite(batteryEnablePin, HIGH);
            Serial.println("Battery ADC read failed");
            return -1;
        }
        total += reading;
    }
    digitalWrite(batteryEnablePin, HIGH);

    // XIAO's battery divider uses 1 MΩ and 510 kΩ.
    float raw = total / 8.0f;
    float voltage = raw * (2.4f / 4095.0f) * (1510.0f / 510.0f);
    Serial.print("Battery ADC: ");
    Serial.print(raw, 1);
    Serial.print(", estimated VBAT: ");
    Serial.print(voltage, 3);
    Serial.println(" V");
    if (voltage < 3.0f || voltage > 4.35f) return -1;
    return constrain(lroundf((voltage - 3.2f) * 100.0f), 0L, 100L);
#else
    return -1;
#endif
}

void setIdleSleep(bool enabled) {
    if (enabled == idleSleep || !canChangeImuRate) return;

    // Keep the accelerometer at 12.5 Hz for motion detection and power down the gyro.
    uint8_t accel = enabled ? (activeAccelControl & ~odrMask) | accel12_5Hz : activeAccelControl;
    uint8_t gyro = enabled ? activeGyroControl & ~odrMask : activeGyroControl;
    if (imu.writeRegister(LSM6DS3_ACC_GYRO_CTRL1_XL, accel) != 0 ||
        imu.writeRegister(LSM6DS3_ACC_GYRO_CTRL2_G, gyro) != 0) {
        imu.writeRegister(LSM6DS3_ACC_GYRO_CTRL1_XL, activeAccelControl);
        imu.writeRegister(LSM6DS3_ACC_GYRO_CTRL2_G, activeGyroControl);
        canChangeImuRate = false;
        idleSleep = false;
        Serial.println("Could not switch IMU power mode");
        return;
    }

    idleSleep = enabled;
    Serial.println(enabled ? "Idle sleep" : "Idle wake");
}

void serviceIdle() {
    unsigned long now = millis();
    if (now - lastMotionCheckMs >= motionCheckIntervalMs) {
        lastMotionCheckMs = now;
        Acceleration current = { imu.readFloatAccelX(), imu.readFloatAccelY(), imu.readFloatAccelZ() };
        float deltaSquared = sq(current.x - stillAcceleration.x) +
                             sq(current.y - stillAcceleration.y) +
                             sq(current.z - stillAcceleration.z);
        if (!hasStillAcceleration || deltaSquared > sq(0.08f)) {
            stillAcceleration = current;
            hasStillAcceleration = true;
            lastMotionMs = now;
            setIdleSleep(false);
        } else if (now - lastMotionMs >= idleTimeoutMs) {
            setIdleSleep(true);
        }
    }

    if (now - lastBatteryUpdateMs >= batteryUpdateIntervalMs) {
        lastBatteryUpdateMs = now;
        int percent = readBatteryPercent();
        if (batteryServiceAvailable && percent >= 0) batteryLevel.writeValue((uint8_t)percent);
    }

    // Yield the CPU between BLE polls while recording is stopped.
    delay(5);
}

int16_t encode(float value, float scale) {
    return (int16_t)constrain(lroundf(value * scale), -32768L, 32767L);
}

void onSwitchCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic) {
    recordingRequested = switchCharacteristic.value() == 1;
}

void applyRecordingRequest() {
    if (isRecording == recordingRequested) return;

    isRecording = recordingRequested;
    Serial.println(isRecording ? "Recording started" : "Recording stopped");
    hasStillAcceleration = false;
    lastMotionMs = millis();
    if (isRecording) {
        bool wasSleeping = idleSleep;
        setIdleSleep(false);
        filter.begin(200);
        // Give the gyro time to settle when it has just been powered up.
        lastSampleUs = micros() + (wasSleeping ? 30000 : 0);
    }
}

void setup() {
    Serial.begin(9600);
    pinMode(LED_BUILTIN, OUTPUT);
    digitalWrite(LED_BUILTIN, HIGH);
    imu.settings.accelRange = 16;
    imu.settings.gyroRange = 2000;
    imu.settings.accelSampleRate = 416;
    imu.settings.gyroSampleRate = 416;
    if (imu.begin() != 0 || !BLE.begin()) {
        Serial.println("IMU or BLE initialization failed");
        while (true) delay(1000);
    }

    canChangeImuRate = imu.readRegister(&activeAccelControl, LSM6DS3_ACC_GYRO_CTRL1_XL) == 0 &&
                       imu.readRegister(&activeGyroControl, LSM6DS3_ACC_GYRO_CTRL2_G) == 0;
#if defined(MOTIONLINK_BATTERY_ADC)
    pinMode(batteryEnablePin, OUTPUT);
    digitalWrite(batteryEnablePin, HIGH);
    analogReadResolution(12);
#if defined(VBAT_ENABLE)
    analogReference(AR_INTERNAL_2_4);
#else
    analogReference(AR_INTERNAL2V4);
    analogAcquisitionTime(AT_40_US);
#endif
#endif
    BLE.setLocalName("MotionLink");
    BLE.setAdvertisedService(motionLinkService);
    motionLinkService.addCharacteristic(dataChar);
    motionLinkService.addCharacteristic(switchCharacteristic);
    // Keep the telemetry and recording handles in their original positions.
    BLE.addService(motionLinkService);
#if defined(MOTIONLINK_BATTERY_ADC)
    int percent = readBatteryPercent();
    lastBatteryUpdateMs = millis();
    if (percent >= 0) {
        batteryService.addCharacteristic(batteryLevel);
        BLE.addService(batteryService);
        batteryLevel.writeValue((uint8_t)percent);
        batteryServiceAvailable = true;
        Serial.print("BLE battery level: ");
        Serial.println(percent);
    }
#endif
    switchCharacteristic.writeValue((uint8_t)0);
    switchCharacteristic.setEventHandler(BLEWritten, onSwitchCharacteristicWritten);
    BLE.advertise();
    lastLedToggleMs = millis();
}

void loop() {
    BLEDevice central = BLE.central();
    if (!central) {
        unsigned long now = millis();
        if (now - lastLedToggleMs >= ledBlinkIntervalMs) {
            lastLedToggleMs = now;
            digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));
        }
        serviceIdle();
        return;
    }

    digitalWrite(LED_BUILTIN, LOW);

    while (central.connected()) {
        BLE.poll();
        if (!central.connected()) break;
        applyRecordingRequest();
        if (!isRecording) {
            serviceIdle();
            continue;
        }
        unsigned long now = micros();
        unsigned long elapsed = now - lastSampleUs;
        if (elapsed < sampleIntervalUs || elapsed > 0x7FFFFFFFUL) continue;

        lastSampleUs += (elapsed / sampleIntervalUs) * sampleIntervalUs;
        uint32_t timestamp = millis();
        float ax = imu.readFloatAccelX();
        float ay = imu.readFloatAccelY();
        float az = imu.readFloatAccelZ();
        float gx = imu.readFloatGyroX();
        float gy = imu.readFloatGyroY();
        float gz = imu.readFloatGyroZ();
        filter.updateIMU(gx, gy, gz, ax, ay, az);

        ImuPacket packet = {
            timestamp,
            encode(ax, 2048), encode(ay, 2048), encode(az, 2048),
            encode(gx, 16), encode(gy, 16), encode(gz, 16),
            encode(filter.getW(), 32767), encode(filter.getX(), 32767),
            encode(filter.getY(), 32767), encode(filter.getZ(), 32767)
        };
        dataChar.writeValue((byte*)&packet, sizeof(packet));
    }

    if (isRecording) {
        isRecording = false;
        hasStillAcceleration = false;
        lastMotionMs = millis();
    }
    recordingRequested = false;
    switchCharacteristic.writeValue((uint8_t)0);
    digitalWrite(LED_BUILTIN, HIGH);
    lastLedToggleMs = millis();
}

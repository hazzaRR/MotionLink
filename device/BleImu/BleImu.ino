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
unsigned long lastSampleUs;
unsigned long lastLedToggleMs;
bool isRecording = false;
Mahony filter;
LSM6DS3 imu(I2C_MODE, 0x6A);

BLEService motionLinkService("19B10000-E8F2-537E-4F6C-D104768A1214");
BLECharacteristic dataChar("36151377-DDD7-4C2F-9207-0724C52C3CCC", BLENotify, sizeof(ImuPacket));
BLEByteCharacteristic switchCharacteristic("FB7A8DB8-DE34-4C6F-9F98-D1612EE35441", BLEWrite | BLERead);

int16_t encode(float value, float scale) {
    return (int16_t)constrain(lroundf(value * scale), -32768L, 32767L);
}

void onSwitchCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic) {
    isRecording = switchCharacteristic.value() == 1;
    if (isRecording) {
        filter.begin(200);
        lastSampleUs = micros();
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

    BLE.setLocalName("MotionLink");
    BLE.setAdvertisedService(motionLinkService);
    motionLinkService.addCharacteristic(dataChar);
    motionLinkService.addCharacteristic(switchCharacteristic);
    BLE.addService(motionLinkService);
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
        return;
    }

    digitalWrite(LED_BUILTIN, LOW);

    while (central.connected()) {
        BLE.poll();
        unsigned long now = micros();
        unsigned long elapsed = now - lastSampleUs;
        if (!isRecording || elapsed < sampleIntervalUs) continue;

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

    isRecording = false;
    switchCharacteristic.writeValue((uint8_t)0);
    digitalWrite(LED_BUILTIN, HIGH);
    lastLedToggleMs = millis();
}

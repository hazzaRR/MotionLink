#include <ArduinoBLE.h>
#include "MahonyAHRS.h"
#include "LSM6DS3.h"
#include "Wire.h"

struct __attribute__((packed)) ImuPacket {
    float qw, qx, qy, qz; // Quaternions
    float ax, ay, az;     // Raw Accel
    float gx, gy, gz;     // Raw Gyro
    uint8_t impact;       // 1 if hit detected, 0 otherwise
};

ImuPacket sensorData;
Mahony filter;

bool isRecording = false;

unsigned long previousMillis = 0;
const long interval = 500;

unsigned long lastSensorUpdate = 0;
const long sensorInterval = 10;

BLEService motionLinkService("19B10000-E8F2-537E-4F6C-D104768A1214");
BLECharacteristic dataChar("36151377-DDD7-4C2F-9207-0724C52C3CCC", BLENotify, sizeof(ImuPacket));
BLEByteCharacteristic switchCharacteristic("FB7A8DB8-DE34-4C6F-9F98-D1612EE35441", BLEWrite | BLERead);

void onSwitchCharacteristicWritten(BLEDevice central, BLECharacteristic characteristic) {
    if (switchCharacteristic.value() == 0x01) {
        isRecording = true;
        filter.begin(100); // Reset orientation on start
        Serial.println("Recording Started via Callback");
    } else {
        isRecording = false;
        Serial.println("Recording Stopped via Callback");
    }
}

LSM6DS3 imu(I2C_MODE, 0x6A);

void setup() {
    Serial.begin(9600);
    imu.begin();
    BLE.begin();

    filter.begin(100);

    BLE.setLocalName("MotionLink");
    BLE.setAdvertisedService(motionLinkService);
    motionLinkService.addCharacteristic(dataChar);
    motionLinkService.addCharacteristic(switchCharacteristic);
    BLE.addService(motionLinkService);
    BLE.advertise();

    pinMode(LED_BUILTIN, OUTPUT);

    switchCharacteristic.setEventHandler(BLEWritten, onSwitchCharacteristicWritten);
    Serial.println("Golf Swing Analyser Ready");
}

void loop() {
    BLEDevice central = BLE.central();
    
    if (central) {
    //   Serial.println(central.address());
        digitalWrite(LED_BUILTIN, LOW);

        while (central.connected()) {
            // Serial.println("Device connected");
            BLE.poll();
            //Serial.println(isRecording);
            if (isRecording) {
                unsigned long currentMillis = millis();
                // Serial.println("Device recording");
                
                // Only send data if the interval has passed
                if (currentMillis - lastSensorUpdate >= sensorInterval) {
                //   Serial.println("reading data");
                    lastSensorUpdate = currentMillis;

                    // Read sensors
                    sensorData.ax = imu.readFloatAccelX();
                    sensorData.ay = imu.readFloatAccelY();
                    sensorData.az = imu.readFloatAccelZ();
                    sensorData.gx = imu.readFloatGyroX();
                    sensorData.gy = imu.readFloatGyroY();
                    sensorData.gz = imu.readFloatGyroZ();

                    // Serial.print("AX: "); Serial.print(sensorData.ax);
                    // Serial.print(" AY: "); Serial.print(sensorData.ay);
                    // Serial.print(" AZ: "); Serial.print(sensorData.az);
                    // Serial.print(" GX: "); Serial.print(sensorData.gx);
                    // Serial.print(" GY: "); Serial.print(sensorData.gy);
                    // Serial.print(" GZ: "); Serial.println(sensorData.gz);

                    filter.updateIMU(sensorData.gx, sensorData.gy, sensorData.gz, 
                    sensorData.ax, sensorData.ay, sensorData.az);

                    sensorData.qw = filter.getW();
                    sensorData.qx = filter.getX();
                    sensorData.qy = filter.getY();
                    sensorData.qz = filter.getZ();

                    float accelMag = sqrt(sq(sensorData.ax) + sq(sensorData.ay) + sq(sensorData.az));
                    if (accelMag > 12.0) {
                        sensorData.impact = 1;
                    } else {
                        sensorData.impact = 0;
                    }

                    // Send data
                    dataChar.writeValue((byte*)&sensorData, sizeof(sensorData));
                }
            }
        }
        // Reset state on disconnect
        isRecording = false;

    } else {
        // Blinking logic for advertising mode
        Serial.println("awaiting device");
        unsigned long currentMillis = millis();
        if (currentMillis - previousMillis >= interval) {
            previousMillis = currentMillis;
            digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));
        }
    }
}
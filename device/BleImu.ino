#include <ArduinoBLE.h>
#include "LSM6DS3.h"
#include "Wire.h"



// 1. Define the Data Structure

struct __attribute__((packed)) ROMPacket {
    float ax, ay, az; // Accelerometer
    float gx, gy, gz; // Gyroscope
};

ROMPacket sensorData;
bool isRecording = false;



unsigned long previousMillis = 0;
const long interval = 500;
unsigned long lastSensorUpdate = 0;
const long sensorInterval = 100;

BLEService motionLinkService("19B10000-E8F2-537E-4F6C-D104768A1214");
BLECharacteristic dataChar("36151377-DDD7-4C2F-9207-0724C52C3CCC", BLENotify, sizeof(ROMPacket));
BLEByteCharacteristic switchCharacteristic("FB7A8DB8-DE34-4C6F-9F98-D1612EE35441", BLEWrite | BLERead);

LSM6DS3 imu(I2C_MODE, 0x6A);

void setup() {
    Serial.begin(9600);
    imu.begin();
    BLE.begin();

    BLE.setLocalName("MotionLink");
    BLE.setAdvertisedService(motionLinkService);

    motionLinkService.addCharacteristic(dataChar);
    motionLinkService.addCharacteristic(switchCharacteristic);

    BLE.addService(motionLinkService);

    BLE.advertise();

    pinMode(LED_BUILTIN, OUTPUT);

    Serial.println("started up");

}

void loop() {
    BLEDevice central = BLE.central();
    
    if (central) {
      Serial.println(central.address());
        digitalWrite(LED_BUILTIN, LOW); // Solid ON when connected

        while (central.connected()) {
            // Serial.println("Device connected");
            BLE.poll();

            Serial.println(isRecording);

            // Check for commands from the phone
            if (switchCharacteristic.written()) {
              Serial.println("command recieved");
                isRecording = (switchCharacteristic.value() == 0x01);
            }

            if (isRecording) {
                unsigned long currentMillis = millis();

                Serial.println("Device recording");
                
                // Only send data if the interval has passed
                if (currentMillis - lastSensorUpdate >= sensorInterval) {
                  Serial.println("reading data");
                    lastSensorUpdate = currentMillis;

                    // Read sensors
                    sensorData.ax = imu.readFloatAccelX();
                    sensorData.ay = imu.readFloatAccelY();
                    sensorData.az = imu.readFloatAccelZ();
                    sensorData.gx = imu.readFloatGyroX();
                    sensorData.gy = imu.readFloatGyroY();
                    sensorData.gz = imu.readFloatGyroZ();

                    Serial.print("AX: "); Serial.print(sensorData.ax);
                    Serial.print(" AY: "); Serial.print(sensorData.ay);
                    Serial.print(" AZ: "); Serial.print(sensorData.az);
                    Serial.print(" GX: "); Serial.print(sensorData.gx);
                    Serial.print(" GY: "); Serial.print(sensorData.gy);
                    Serial.print(" GZ: "); Serial.println(sensorData.gz);

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
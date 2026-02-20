#include <ArduinoBLE.h>

#include <LSM6DS3.h> // Seeed Arduino LSM6DS3 library



// 1. Define the Data Structure

struct __attribute__((packed)) ROMPacket {
    float ax, ay, az; // Accelerometer
    // float gx, gy, gz; // Gyroscope
};

ROMPacket sensorData;
bool isRecording = false;


// Timing for the flashing LED (non-blocking)

unsigned long previousMillis = 0;
const long interval = 500; // Flash every 500ms


// 2. BLE Setup

BLEService motionLinkService("19B10000-E8F2-537E-4F6C-D104768A1214");
BLECharacteristic dataChar("19B10001-E8F2-537E-4F6C-D104768A1214", BLENotify, sizeof(ROMPacket));
BLEByteCharacteristic controlChar("19B10002-E8F2-537E-4F6C-D104768A1214", BLEWrite);

void setup() {
    IMU.begin();
    BLE.begin();

    BLE.setLocalName("MotionLink");
    BLE.setAdvertisedService(motionLinkService);

    motionLinkService.addCharacteristic(dataChar);
    motionLinkService.addCharacteristic(controlChar);

    BLE.addService(motionLinkService);

    BLE.advertise();

    pinMode(LED_BUILTIN, OUTPUT);

}



void loop() {
    BLEDevice central = BLE.central();
    if (central) {

    digitalWrite(LED_BUILTIN, LOW);



    while (central.connected()) {
        // Check if phone sent Start (1) or Stop (0)
        if (controlChar.written()) {
            isRecording = (controlChar.value() == 0x01);
        }
        if (isRecording) {

            float t_ax, t_ay, t_az, t_gx, t_gy, t_gz;

            IMU.readAcceleration(t_ax, t_ay, t_az);

            IMU.readGyroscope(t_gx, t_gy, t_gz); // Fixed: changed t_az to t_gz

            sensorData.ax = t_ax;
            sensorData.ay = t_ay;
            sensorData.az = t_az;
            sensorData.gx = t_gx;
            sensorData.gy = t_gy;
            sensorData.gz = t_gz;

            // Pack and send

            dataChar.writeValue((byte*)&sensorData, sizeof(sensorData));
            delay(20);

        }

    }

    // Reset recording state on disconnect if desired

    isRecording = false;

    } else {

        // --- DISCONNECTED STATE ---
        // Flash the LED using non-blocking millis()

        unsigned long currentMillis = millis();

        if (currentMillis - previousMillis >= interval) {
            previousMillis = currentMillis;
        // Toggle the LED
            digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));

        }

    }

}
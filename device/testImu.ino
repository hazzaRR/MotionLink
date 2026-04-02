#include "LSM6DS3.h"
#include "Wire.h"

LSM6DS3 imu(I2C_MODE, 0x6A);

void setup() {
    Serial.begin(9600);
    while (!Serial);

    if (imu.begin() != 0) {
        Serial.println("IMU init failed!");
    } else {
        Serial.println("IMU ready!");
    }
}

void loop() {
    float ax = imu.readFloatAccelX();
    float ay = imu.readFloatAccelY();
    float az = imu.readFloatAccelZ();

    float gx = imu.readFloatGyroX();
    float gy = imu.readFloatGyroY();
    float gz = imu.readFloatGyroZ();

    Serial.print("AX: "); Serial.print(ax);
    Serial.print(" AY: "); Serial.print(ay);
    Serial.print(" AZ: "); Serial.print(az);
    Serial.print(" GX: "); Serial.print(gx);
    Serial.print(" GY: "); Serial.print(gy);
    Serial.print(" GZ: "); Serial.println(gz);

    delay(50);
}
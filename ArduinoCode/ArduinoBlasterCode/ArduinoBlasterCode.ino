#include <Wire.h>
#include <Adafruit_MPU6050.h>
#include <Adafruit_Sensor.h>
#include <math.h>

Adafruit_MPU6050 mpu;

const int buttonPin = 7; // your trigger button
int buttonState = HIGH;  // default HIGH with INPUT_PULLUP

float pitch = 0.0, yawRate = 0.0;
unsigned long lastTime = 0;

void setup() {
  Serial.begin(115200);
  while (!Serial) delay(10);

  pinMode(buttonPin, INPUT_PULLUP);

  if (!mpu.begin()) {
    Serial.println("MPU6050 not found!");
    while (1) delay(10);
  }

  mpu.setAccelerometerRange(MPU6050_RANGE_8_G);
  mpu.setGyroRange(MPU6050_RANGE_500_DEG);
  mpu.setFilterBandwidth(MPU6050_BAND_21_HZ);
  delay(100);
  lastTime = millis();
}

void loop() {
  sensors_event_t accel, gyro, temp;
  mpu.getEvent(&accel, &gyro, &temp);

  // Correct pitch from X-axis
  float accelPitch = atan2(-accel.acceleration.x, accel.acceleration.z) * 180 / PI;

  unsigned long now = millis();
  float dt = (now - lastTime) / 1000.0;
  lastTime = now;

  pitch = 0.9 * (pitch + gyro.gyro.x * dt * 180 / PI) + 0.06 * accelPitch;
  yawRate = gyro.gyro.z * 180 / PI;

  buttonState = digitalRead(buttonPin);

  Serial.print(pitch, 2);
  Serial.print(",0,");
  Serial.print(yawRate, 2);
  Serial.print(",");
  Serial.println(buttonState == LOW ? 1 : 0);

  delay(10);
}

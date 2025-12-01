#include <Wire.h>
#include <Adafruit_MPU6050.h>
#include <Adafruit_Sensor.h>
#include <math.h>

Adafruit_MPU6050 mpu;

const int buttonPin = 7; 
int buttonState = HIGH;

float pitch = 0.0;
float pitchOffset = 0.0;
float yawOffset = 0.0;

float yaw   = 0.0;

unsigned long lastTime = 0;

// ---- Drift correction ----
float yawBias = 0.0;
const float STILL_THRESHOLD_RAD = 0.07f;   // rad/s below this = still
const float BIAS_ALPHA = 0.001f;          // slow bias learning
const float DEADZONE_DEG = 0.15f;         // ignore tiny yaw noise

// ---- Auto recenter ----
unsigned long stillStartTime = 0;
bool wasStillLastFrame = false;
const unsigned long RECENTER_TIME_MS = 1500;  // 1.5s

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

  // Time step
  unsigned long now = millis();
  float dt = (now - lastTime) / 1000.0f;
  lastTime = now;
  if (dt <= 0) dt = 0.001f;

  // ----- Pitch complementary filter -----
  float accelPitch = atan2(accel.acceleration.y, accel.acceleration.z) * 180 / PI;
  float gyroPitchRateDeg = gyro.gyro.y * 180 / PI;
  pitch = 0.9f * (pitch + gyroPitchRateDeg * dt) + 0.1f * accelPitch;

  // ----- Yaw drift correction -----
  float gz = gyro.gyro.z;  // rad/s

  // If nearly still: learn bias slowly
  bool isStill = (fabs(gz) < STILL_THRESHOLD_RAD);

  if (isStill) {
    yawBias = yawBias * (1.0f - BIAS_ALPHA) + (BIAS_ALPHA * gz);
  }

  float correctedYawRateRad = gz - yawBias;
  float correctedYawRateDeg = correctedYawRateRad * 180.0f / PI;

  // Deadzone
  if (fabs(correctedYawRateDeg) < DEADZONE_DEG) {
    correctedYawRateDeg = 0;
  }

  yaw += correctedYawRateDeg * dt;

  // ----- AUTO RECENTER: held still for 2 seconds -----
  if (isStill) {
    if (!wasStillLastFrame) {
        stillStartTime = now;
    }

    if (now - stillStartTime >= RECENTER_TIME_MS) {
        pitchOffset = pitch;   // store current natural pitch
        yawOffset   = yaw;     // store current yaw
    }

    wasStillLastFrame = true;
  } else {
    wasStillLastFrame = false;
  }

  // Button
  buttonState = digitalRead(buttonPin);

  float outPitch = pitch - pitchOffset;
  float outYaw   = yaw   - yawOffset;

  Serial.print(outPitch, 2);
  Serial.print(",0,");
  Serial.print(outYaw, 2);
  Serial.print(",");
  Serial.println(buttonState == LOW ? 1 : 0);

  delay(10);
}

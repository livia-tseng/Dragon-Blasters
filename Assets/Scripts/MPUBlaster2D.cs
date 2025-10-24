using UnityEngine;
using System;
using System.IO.Ports;
//using UnityEngine;
//using UnityEngine.InputSystem; // for the new Input System

public class MPUBlaster2D : MonoBehaviour
{
    [Header("Serial Settings")]
    public string serialPortName = "/dev/tty.usbmodem2101";  // Change to your Arduino port
    public int baudRate = 115200;

    [Header("References")]
    public Transform reticle;   // assign your 2D sprite Transform here
    public Camera mainCamera;   // usually your Main Camera

    [Header("Tuning")]
    [Range(0f, 1f)] public float smoothing = 0.95f;
    public float moveScale = 0.01f;
    public float mountPitchOffset = 0f;

    private SerialPort serial;
    private float latestPitch, latestGz;
    private float filteredPitch, filteredYaw;
    private float yaw;
    private float pitchOffset = 0f;
    private float yawOffset = 0f;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        try
        {
            serial = new SerialPort(serialPortName, baudRate);
            serial.ReadTimeout = 100;
            serial.Open();
            Debug.Log("✅ Serial opened: " + serialPortName);
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Could not open serial port: " + e.Message);
        }
    }

    void Update()
    {
        // --- Read serial data ---
        if (serial != null && serial.IsOpen && serial.BytesToRead > 0)
        {
            try
            {
                string line = serial.ReadLine();
                string[] parts = line.Split(',');
                if (parts.Length >= 3)
                {
                    float.TryParse(parts[0], out latestPitch);
                    float.TryParse(parts[2], out latestGz);
                }
            }
            catch { }
        }

        // --- Integrate yaw and smooth both axes ---
        float gz = Mathf.Abs(latestGz) < 0.3f ? 0f : latestGz;
        yaw -= gz * Time.deltaTime;
        filteredPitch = Mathf.Lerp(filteredPitch, latestPitch, 1f - smoothing);
        filteredYaw = Mathf.Lerp(filteredYaw, yaw, 1f - smoothing);

        // --- Calibration (press 'C') ---
        //if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        //{
        //    pitchOffset = filteredPitch;
        //    yawOffset = filteredYaw;
        //    Debug.Log("🔧 Calibrated center! PitchOffset=" + pitchOffset + " YawOffset=" + yawOffset);
        //}

        // --- Apply offsets + mount correction ---
        float displayPitch = -(filteredPitch - pitchOffset - mountPitchOffset);
        float displayYaw = (filteredYaw - yawOffset);

        // --- Move reticle ---
        if (reticle)
        {
            Vector3 pos = new Vector3(displayYaw * moveScale, displayPitch * moveScale, 0f);

            float camHeight = mainCamera.orthographicSize;
            float camWidth = camHeight * mainCamera.aspect;

            pos.x = Mathf.Clamp(pos.x, -camWidth + 0.5f, camWidth - 0.5f);
            pos.y = Mathf.Clamp(pos.y, -camHeight + 0.5f, camHeight - 0.5f);

            reticle.localPosition = pos;
        }
    }

    void OnDisable()
    {
        if (serial != null && serial.IsOpen)
        {
            serial.Close();
            Debug.Log("🔌 Serial closed.");
        }
    }
}
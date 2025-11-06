using UnityEngine;
using System;
using System.IO.Ports;

public class MPUBlaster2D : MonoBehaviour
{
    [Header("Serial Settings")]
    //public string serialPortName = "/dev/tty.usbmodem2101";  // Livia's Mac
    public string serialPortName = "COM5"; // Samuel's Laptop
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

    private int triggerPressed = 0;
    private int lastTrigger = 0;

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
                if (parts.Length >= 4)
                {
                    float.TryParse(parts[0], out latestPitch);
                    float.TryParse(parts[2], out latestGz);
                    int.TryParse(parts[3], out triggerPressed);
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
        if (Input.GetKeyDown(KeyCode.C))
        {
            pitchOffset = filteredPitch;
            yawOffset = filteredYaw;
            Debug.Log($"🔧 Calibrated center! PitchOffset={pitchOffset:F2}, YawOffset={yawOffset:F2}");
        }

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

        if (triggerPressed == 1 && lastTrigger == 0)
        {
            Debug.Log("💥 Trigger pulled!");

            //Fire the projectile
            Vector2 worldPos = reticle.position;
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            if (hit.collider != null)
            {
                // Simulate a click
                SpriteOnClicked clickable = hit.collider.GetComponent<SpriteOnClicked>();
                if (clickable != null)
                {
                    // Directly call the same coroutine
                    clickable.SendMessage("OnMouseDown", SendMessageOptions.DontRequireReceiver);
                }
            }
        }
        lastTrigger = triggerPressed;
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
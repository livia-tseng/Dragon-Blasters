using UnityEngine;
using System;
using System.IO.Ports;

public class MPUBlaster2D : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Which player this blaster belongs to (1, 2, etc.)")]
    public int playerId = 1;

    [Header("Serial Settings")]
    public string serialPortName = "COM5"; // or /dev/tty.usbmodem2101 on liv's mac
    public int baudRate = 115200;

    [Header("References")]
    public Transform reticle;   // assign your 2D sprite Transform here
    public Camera mainCamera;   // usually your Main Camera

    [Header("Tuning")]
    [Range(0f, 1f)] public float smoothing = 0.95f;
    public float moveScale = 0.01f;
    public float mountPitchOffset = 0f;

    [Header("Hit Detection")]
    [Tooltip("Which layers count as shootable (e.g., Dragons)")]
    public LayerMask hitMask = ~0;
    [Tooltip("Radius around the reticle to check for hits (for a bit of forgiveness).")]
    public float hitRadius = 0.15f;

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
            Debug.Log($"✅ P{playerId}: Serial opened on {serialPortName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ P{playerId}: Could not open serial port {serialPortName}: {e.Message}");
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
            Debug.Log($"🔧 P{playerId} calibrated: PitchOffset={pitchOffset:F2}, YawOffset={yawOffset:F2}");
        }

        // --- Apply offsets + mount correction ---
        float displayPitch = -(filteredPitch - pitchOffset - mountPitchOffset);
        float displayYaw = (filteredYaw - yawOffset);

        // --- Move reticle ---
        if (reticle && mainCamera)
        {
            Vector3 pos = new Vector3(displayYaw * moveScale, displayPitch * moveScale, 0f);

            float camHeight = mainCamera.orthographicSize;
            float camWidth = camHeight * mainCamera.aspect;

            pos.x = Mathf.Clamp(pos.x, -camWidth + 0.5f, camWidth - 0.5f);
            pos.y = Mathf.Clamp(pos.y, -camHeight + 0.5f, camHeight - 0.5f);

            reticle.localPosition = pos;
        }

        // --- Trigger edge detection (only on press, not hold) ---
        if (triggerPressed == 1 && lastTrigger == 0)
        {
            Debug.Log($"💥 P{playerId} Trigger pulled!");
            FireAtReticle();
        }
        lastTrigger = triggerPressed;
    }

    private void FireAtReticle()
    {
        if (!reticle) return;

        Vector2 worldPos = reticle.position;

        // Slight forgiving radius instead of perfect pixel hit
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, hitRadius, hitMask);

        foreach (var h in hits)
        {
            SpriteOnClicked clickable = h.GetComponentInParent<SpriteOnClicked>();
            if (clickable != null)
            {
                clickable.TryHit(playerId);  // <-- score + respawn handled inside
                return;
            }
        }
    }

    void OnDisable()
    {
        if (serial != null && serial.IsOpen)
        {
            serial.Close();
            Debug.Log($"🔌 P{playerId} serial closed ({serialPortName}).");
        }
    }
}

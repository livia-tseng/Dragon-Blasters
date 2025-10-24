using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpriteOnClicked : MonoBehaviour
{
    [Header("Respawn")]
    [Tooltip("Delay (seconds) before respawning.")]
    public float respawnDelay = 0.5f;

    [Tooltip("Optional: Area to respawn in (BoxCollider2D bounds). If null, uses camera view.")]
    public BoxCollider2D spawnArea;

    [Tooltip("Padding from screen edges when using camera bounds (world units).")]
    public Vector2 screenPadding = new Vector2(0.3f, 0.3f);

    [Tooltip("Camera to compute bounds. Defaults to Camera.main.")]
    public Camera cam;

    [Header("Visuals (optional)")]
    public SpriteRenderer spriteRenderer;     // if null, auto-grab
    public Animator animator;                 // optional Animator
    public string deathTrigger = "Die";       // leave empty if you don't use it
    public string reviveTrigger = "Revive";   // leave empty if you don't use it

    [Header("Movement (optional)")]
    [Tooltip("Reference to your movement script (e.g., SpriteMover2D) to pause during death.")]
    public Behaviour movementScript;

    private Collider2D col;
    private Rigidbody2D rb;
    private bool busy;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!cam) cam = Camera.main;
    }

    private void OnMouseDown()
    {
        if (!busy) StartCoroutine(DieThenRespawn());
    }

    private IEnumerator DieThenRespawn()
    {
        busy = true;

        // Stop movement
        if (movementScript) movementScript.enabled = false;
        if (rb)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Play death anim (if any)
        if (animator && !string.IsNullOrEmpty(deathTrigger))
            animator.SetTrigger(deathTrigger);

        // Make unclickable/inactive
        col.enabled = false;

        // Hide sprite if you don't have an animation
        bool manuallyHidden = false;
        if (spriteRenderer && (animator == null || string.IsNullOrEmpty(deathTrigger)))
        {
            spriteRenderer.enabled = false;
            manuallyHidden = true;
        }

        yield return new WaitForSeconds(respawnDelay);

        // Move to new position
        Vector2 respawnPos = GetRespawnPosition();
        if (rb) rb.position = respawnPos;
        else transform.position = new Vector3(respawnPos.x, respawnPos.y, transform.position.z);

        // Show again / revive
        if (animator && !string.IsNullOrEmpty(reviveTrigger))
            animator.SetTrigger(reviveTrigger);
        if (manuallyHidden && spriteRenderer)
            spriteRenderer.enabled = true;

        // Re-enable interaction and movement
        col.enabled = true;
        if (movementScript) movementScript.enabled = true;

        busy = false;
    }

    private Vector2 GetRespawnPosition()
    {
        if (spawnArea)
        {
            Bounds b = spawnArea.bounds;
            float x = Random.Range(b.min.x, b.max.x);
            float y = Random.Range(b.min.y, b.max.y);
            return new Vector2(x, y);
        }
        else
        {
            // Use camera visible rect
            Vector2 min, max;
            GetScreenWorldBounds(out min, out max);
            float x = Random.Range(min.x + screenPadding.x, max.x - screenPadding.x);
            float y = Random.Range(min.y + screenPadding.y, max.y - screenPadding.y);
            return new Vector2(x, y);
        }
    }

    private void GetScreenWorldBounds(out Vector2 min, out Vector2 max)
    {
        if (!cam) cam = Camera.main;

        float zDist = cam.orthographic
            ? (transform.position.z - cam.transform.position.z)
            : Mathf.Abs(cam.transform.position.z - transform.position.z);

        Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0f, 0f, zDist));
        Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1f, 1f, zDist));

        min = new Vector2(Mathf.Min(bl.x, tr.x), Mathf.Min(bl.y, tr.y));
        max = new Vector2(Mathf.Max(bl.x, tr.x), Mathf.Max(bl.y, tr.y));
    }
}

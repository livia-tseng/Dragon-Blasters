using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpriteOnClicked : MonoBehaviour
{
    [Header("Respawn")]
    public float respawnDelay = 0.5f;
    public BoxCollider2D spawnArea;
    public Vector2 screenPadding = new Vector2(0.3f, 0.3f);
    public Camera cam;

    [Header("Visuals (optional)")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public string deathTrigger = "Die";
    public string reviveTrigger = "Revive";

    [Header("Movement (optional)")]
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

    // Mouse testing – counts as "no player"
    private void OnMouseDown()
    {
        TryHit(-1);
    }

    /// <summary>Call this from blasters with a playerId (1, 2, etc).</summary>
    public void TryHit(int playerId)
    {
        if (busy || !col.enabled) return;

        // Hook your score system here:
        // Debug.Log($"TryHit" + playerId);
        if(playerId > 0) GameManager.Add(playerId, 1);

        StartCoroutine(DieThenRespawn());
    }

    private IEnumerator DieThenRespawn()
    {
        busy = true;

        if (movementScript) movementScript.enabled = false;
        if (rb)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (animator && !string.IsNullOrEmpty(deathTrigger))
            animator.SetTrigger(deathTrigger);

        col.enabled = false;

        bool hidden = false;
        if (spriteRenderer && (animator == null || string.IsNullOrEmpty(deathTrigger)))
        {
            spriteRenderer.enabled = false;
            hidden = true;
        }

        yield return new WaitForSeconds(respawnDelay);

        Vector2 respawnPos = GetRespawnPosition();
        if (rb) rb.position = respawnPos;
        else transform.position = new Vector3(respawnPos.x, respawnPos.y, transform.position.z);

        if (animator && !string.IsNullOrEmpty(reviveTrigger))
            animator.SetTrigger(reviveTrigger);
        if (hidden && spriteRenderer)
            spriteRenderer.enabled = true;

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

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ObstacleMovement : MonoBehaviour
{
    public enum MovementMode { Bounce, Vertical }
    public MovementMode mode = MovementMode.Bounce;

    public float speed = 3f;
    public Rigidbody2D rb;
    public Camera cam;
    private SpriteRenderer sr;

    private Vector2 velocity;

    public bool rotate = false;
    public float rotationSpeed = 180f;

    //vertical
    private float minRespawnDelay = 5f;
    private float maxRespawnDelay = 10f;

    private bool isRespawning = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (!cam) cam = Camera.main;


        sr.sortingOrder = Random.Range(5, 16);


        // Set initial direction based on mode
        if (mode == MovementMode.Bounce)
        {
            speed = Random.Range(1f, 5f);

            // Any direction
            float angle = Random.Range(0f, 360f);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            velocity = dir.normalized * speed;
        }
        else if (mode == MovementMode.Vertical)
        {
            Vector2 dir = new Vector2(0f, 1f);
            velocity = dir.normalized * speed;
        }
    }

    private void FixedUpdate()
    {
        switch (mode)
        {
            case MovementMode.Bounce:
                BounceMovement();

                if (rotate)
                {
                    DoRotation();
                }
                break;
            case MovementMode.Vertical:
                VerticalMovement();
                break;
        }
    }


    private void BounceMovement()
    {
        if (!cam) cam = Camera.main;

        // Current pos
        Vector2 pos = GetCurrentPos2D();

        // Get screen boundaries
        Vector2 min, max;
        GetScreenWorldBounds(out min, out max);

        // Move object
        pos += velocity * Time.fixedDeltaTime;

        // Bounce off left/right edges
        if (pos.x <= min.x || pos.x >= max.x)
        {
            velocity.x = -velocity.x;     // reverse x direction
            pos.x = Mathf.Clamp(pos.x, min.x, max.x);
        }

        // Bounce off bottom/top edges
        if (pos.y <= min.y || pos.y >= max.y)
        {
            velocity.y = -velocity.y;     // reverse y direction
            pos.y = Mathf.Clamp(pos.y, min.y, max.y);
        }

        // Apply movement
        if (rb) rb.MovePosition(pos);
        else transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

    private void VerticalMovement()
    {
        if (!cam) cam = Camera.main;

        Vector2 pos = GetCurrentPos2D();
        pos += velocity * Time.fixedDeltaTime;

        // Apply movement
        if (rb) rb.MovePosition(pos);
        else transform.position = new Vector3(pos.x, pos.y, transform.position.z);

        // Check if offscreen ABOVE the screen
        Vector2 min, max;
        GetScreenWorldBounds(out min, out max);

        if (!isRespawning && pos.y > max.y + 1f)  // extra padding so sprite fully clears
        {
            StartCoroutine(RespawnAfterDelay());
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

    private float GetCurrentX()
    {
        return rb ? rb.position.x : transform.position.x;
    }

    private Vector2 GetCurrentPos2D()
    {
        return rb ? rb.position : (Vector2)transform.position;
    }

    private void DoRotation()
    {

        float delta = rotationSpeed * Time.fixedDeltaTime;

        transform.Rotate(0f, 0f, delta);
    }

    private IEnumerator RespawnAfterDelay()
    {
        isRespawning = true;

        // Wait random delay
        float wait = Random.Range(minRespawnDelay, maxRespawnDelay);
        yield return new WaitForSeconds(wait);

        // Respawn at bottom
        Vector2 min, max;
        GetScreenWorldBounds(out min, out max);

        float x = Random.Range(min.x, max.x);
        float y = min.y - 1f;   // just below the screen

        Vector2 respawnPos = new Vector2(x, y);

        if (rb)
            rb.position = respawnPos;
        else
            transform.position = new Vector3(respawnPos.x, respawnPos.y, transform.position.z);

        // Randomize sorting order again
        sr.sortingOrder = Random.Range(5, 16);

        Vector2 dir = new Vector2(0f, 1f);

        velocity = dir.normalized * speed;

        isRespawning = false;
    }


}

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteMover2D : MonoBehaviour
{
    public enum MovementMode { Idle, Patrol, RandomWander }

    public MovementMode mode = MovementMode.Patrol;
    public float speed = 3f;
    public Rigidbody2D rb;
    public Camera cam;
    private SpriteRenderer sr;
    private float lastX;

    // patrol
    public float leftX = -6f;
    public float rightX = 6f;
    public bool startMovingRight = true;
    private int patrolDir = +1; 

    // wander
    public float arriveThreshold = 0.05f;
    public Vector2 screenPadding = new Vector2(1f, 1f);
    public float minPause = 0f;
    public float maxPause = 0.1f;

    private Vector2 wanderTarget;
    private float nextMoveTime = 0f;
    private bool hasTarget = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (!cam) cam = Camera.main;

        if (leftX > rightX) { float t = leftX; leftX = rightX; rightX = t; }
        patrolDir = startMovingRight ? +1 : -1;
        flip(patrolDir);

        lastX = GetCurrentX();
        if (mode == MovementMode.RandomWander)
        {
            PickNewWanderTarget();
            nextMoveTime = 0f;
        }
    }

    private void FixedUpdate()
    {
        switch (mode)
        {
            case MovementMode.Idle:
                flipDelta(0f);
                break;

            case MovementMode.Patrol:
                TickPatrol();
                break;

            case MovementMode.RandomWander:
                TickRandomWander();
                break;
        }
    }

    // patrol
    private void TickPatrol()
    {
        float delta = speed * Time.fixedDeltaTime * patrolDir;
        float beforeX = GetCurrentX();

        if (rb)
            rb.MovePosition(new Vector2(rb.position.x + delta, rb.position.y));
        else
            transform.position += new Vector3(delta, 0f, 0f);

        float x = GetCurrentX();
        if (patrolDir > 0 && x >= rightX)
        {
            patrolDir = -1;
            flip(patrolDir);
        }
        else if (patrolDir < 0 && x <= leftX)
        {
            patrolDir = +1;
            flip(patrolDir);
        }

        flipDelta(x - beforeX);
        lastX = x;
    }

    // wander
    private void TickRandomWander()
    {
        if (!cam) cam = Camera.main;

        if (Time.time < nextMoveTime)
        {
            flipDelta(0f);
            return;
        }

        wanderTarget = ClampToScreen(wanderTarget);

        Vector2 current = GetCurrentPos2D();
        Vector2 toTarget = wanderTarget - current;

        if (toTarget.sqrMagnitude <= arriveThreshold * arriveThreshold)
        {
            float pause = Random.Range(minPause, maxPause);
            nextMoveTime = Time.time + pause;
            PickNewWanderTarget();
            flipDelta(0f);
            return;
        }

        float step = speed * Time.fixedDeltaTime;
        Vector2 nextPos = Vector2.MoveTowards(current, wanderTarget, step);

        if (rb) rb.MovePosition(nextPos);
        else transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);

        flipDelta(nextPos.x - lastX);
        lastX = nextPos.x;
    }

    private void PickNewWanderTarget()
    {
        Vector2 min, max;
        GetScreenWorldBounds(out min, out max);

        float x = Random.Range(min.x + screenPadding.x, max.x - screenPadding.x);
        float y = Random.Range(min.y + screenPadding.y, max.y - screenPadding.y);
        wanderTarget = new Vector2(x, y);
        hasTarget = true;
    }

    private Vector2 ClampToScreen(Vector2 p)
    {
        Vector2 min, max;
        GetScreenWorldBounds(out min, out max);
        p.x = Mathf.Clamp(p.x, min.x + screenPadding.x, max.x - screenPadding.x);
        p.y = Mathf.Clamp(p.y, min.y + screenPadding.y, max.y - screenPadding.y);
        return p;
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

    private void flip(int dir)
    {
        sr.flipX = dir < 0;
    }

    private void flipDelta(float dx)
    {
        if (Mathf.Abs(dx) > 0.0001f)
            sr.flipX = dx < 0f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Patrol bounds
        if (mode == MovementMode.Patrol)
        {
            Gizmos.color = Color.yellow;
            Vector3 a = new Vector3(leftX, transform.position.y, 0f);
            Vector3 b = new Vector3(rightX, transform.position.y, 0f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(a + Vector3.down * 0.4f, a + Vector3.up * 0.4f);
            Gizmos.DrawLine(b + Vector3.down * 0.4f, b + Vector3.up * 0.4f);
        }

        // Random wander bounds + target
        if (mode == MovementMode.RandomWander)
        {
            if (!cam) cam = Camera.main;
            if (cam)
            {
                Vector2 min, max;
                GetScreenWorldBounds(out min, out max);
                Vector3 A = new Vector3(min.x + screenPadding.x, min.y + screenPadding.y, 0f);
                Vector3 B = new Vector3(max.x - screenPadding.x, min.y + screenPadding.y, 0f);
                Vector3 C = new Vector3(max.x - screenPadding.x, max.y - screenPadding.y, 0f);
                Vector3 D = new Vector3(min.x + screenPadding.x, max.y - screenPadding.y, 0f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(A, B); Gizmos.DrawLine(B, C); Gizmos.DrawLine(C, D); Gizmos.DrawLine(D, A);

                if (hasTarget)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(new Vector3(wanderTarget.x, wanderTarget.y, 0f), 0.07f);
                }
            }
        }
    }
#endif
}

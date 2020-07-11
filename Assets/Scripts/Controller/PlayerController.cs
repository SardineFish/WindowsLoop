using UnityEngine;
using System.Collections;
using UnityRawInput;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    const float FIXED_DELTA_TIME = 0.02f;
    [SerializeField]
    [Delayed]
    private float m_Speed = 10;
    [SerializeField]
    [Range(0, 1)]
    private float m_MoveDamping = 1f;
    [SerializeField]
    [Delayed]
    private float m_JumpHeight = 3;
    [SerializeField]
    [Delayed]
    private float m_JumpTime = 0.5f;

    [SerializeField]
    private float m_FallGravityScale = 1;

    [SerializeField]
    private float m_JumpCacheTime = 0.2f;

    [SerializeField]
    private float m_CoyoteTime = 0.1f;


    float gravity
    {
        get
        {
            var dt = Time.fixedDeltaTime;
            var n = m_JumpTime / dt;
            return 2 * m_JumpHeight / (n * (n + 1) * dt * dt);
        }
    }

    float jumpVelocity => gravity * m_JumpTime;


    bool focused = true;
    Vector2 rawMovementInput;
    Vector2 dampedInput = Vector2.zero;
    Vector2 velocity;
    bool onGround = false;
    new BoxCollider2D collider;
    new Rigidbody2D rigidbody;
    StateCache jumpCache = new StateCache(0.2f);
    StateCache onGroundCache = new StateCache(0.1f);

    private void Awake()
    {
        collider = GetComponent<BoxCollider2D>();
        rigidbody = GetComponent<Rigidbody2D>();
        
    }
    private void OnEnable()
    {
        RawKeyInput.Start(true);
    }

    private void OnDisable()
    {
        RawKeyInput.Stop();
    }

    // Use this for initialization
    void Start()
    {
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        OnCollision2D(collision);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        OnCollision2D(collision);
    }

    void OnCollision2D(Collision2D collision)
    {
        var footheight = (collider.transform.position.ToVector2() + collider.offset - collider.size / 2).y - collider.edgeRadius;
        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f && Mathf.Abs(contact.point.y - footheight) < 0.1f)
            {
                onGround = true;
                onGroundCache.Renew(Time.time);
            }
            if (Mathf.Abs(contact.normal.y) > 0.5f)
                velocity.y = 0;
            Debug.DrawLine(contact.point, contact.point + contact.normal, Color.red);

        }
    }

    // Update is called once per frame
    void Update()
    {
        var movement = new Vector2();
        if (!focused)
        {
            if (RawKeyInput.IsKeyDown(RawKey.A))
                movement += Vector2.left;
            if (RawKeyInput.IsKeyDown(RawKey.D))
                movement += Vector2.right;
            if (RawKeyInput.IsKeyDown(RawKey.W))
                movement += Vector2.up;
            if (RawKeyInput.IsKeyDown(RawKey.S))
                movement += Vector2.down;
            if (RawKeyInput.IsKeyDown(RawKey.Space))
            {
                jumpCache.Renew(Time.time);
            }
               
        }
        else
        {
            if (Input.GetKey(KeyCode.A))
                movement += Vector2.left;
            if (Input.GetKey(KeyCode.D))
                movement += Vector2.right;
            if (Input.GetKey(KeyCode.W))
                movement += Vector2.up;
            if (Input.GetKey(KeyCode.S))
                movement += Vector2.down;
            if (Input.GetKey(KeyCode.Space))
                jumpCache.Renew(Time.time);
        }
        rawMovementInput = movement;
        jumpCache.CacheTime = m_JumpCacheTime;
        jumpCache.Update(Time.time);
        onGroundCache.CacheTime = m_CoyoteTime;
        onGroundCache.Update(Time.time);
    }

    private void LateUpdate()
    {
        
    }

    public void Jump()
    {
        if (onGroundCache)
            velocity.y = jumpVelocity;
        onGround = false;
        onGroundCache.Clear();
    }
    private void FixedUpdate()
    {
        if (jumpCache.Value)
            Jump();

        onGround = false;

        velocity = new Vector2(
            Mathf.Lerp(velocity.x, rawMovementInput.x * m_Speed, (1 - m_MoveDamping)),
            velocity.y
        );

        if(rigidbody.velocity.y < 0)
            Physics2D.gravity = Vector2.down * gravity * m_FallGravityScale;
        else
            Physics2D.gravity = Vector2.down * gravity;


        Vector2 v;
        v.x = velocity.x;

        if (velocity.y > 0)
            v.y = velocity.y;
        else
            v.y = rigidbody.velocity.y;


        rigidbody.velocity = v;

        velocity.y = 0;
        
    }

    (Vector2 penetration, float time) CollisionCheck(float dt, Vector2 velocity, Vector2 normal, Vector2 tangent)
    {
        var halfSize = collider.size / 2;
        var pointA = collider.transform.position.ToVector2() + collider.offset + halfSize * normal + halfSize * tangent;
        var pointB = collider.transform.position.ToVector2() + collider.offset + halfSize * normal - halfSize * tangent;

        var offsetA = pointA + velocity * dt;
        var offsetB = pointB + velocity * dt;

        var tileA = GameMap.Instance.GetTileAt(offsetA);
        var tileB = GameMap.Instance.GetTileAt(offsetB);

        float penetration = 0;
        bool penetrated = false;

        float time = -1;

        if(tileA)
        {
            var offsetDistance = ((offsetA - pointA) * normal).magnitude;
            var penA = (ClampToDir(offsetA, -normal) - offsetA).magnitude;
            var timeA = (offsetDistance - penA) / (velocity * normal).magnitude;
            penetration = penA;
            time = timeA;
            penetrated = true;
        }
        if(tileB)
        {
            var offsetDistance = ((offsetB - pointB) * normal).magnitude;
            var penB = (ClampToDir(offsetB, -normal) - offsetB).magnitude;
            var timeB = (offsetDistance - penB) / (velocity * normal).magnitude;
            if(timeB < time || !penetrated)
            {
                penetration = penB;
                time = timeB;
            }
        }


        return (normal * penetration, time);
    }

    Vector2 ClampToDir(Vector2 v, Vector2 normal)
    {
        if (normal.x > 0)
            v.x = Mathf.Ceil(v.x);
        else if (normal.x < 0)
            v.x = Mathf.Floor(v.x);

        if (normal.y > 0)
            v.y = Mathf.Ceil(v.y);
        else if (normal.y < 0)
            v.y = Mathf.Floor(v.y);

        return v;
    }

    private void OnApplicationFocus(bool focus)
    {
        focused = focus;
    }
}

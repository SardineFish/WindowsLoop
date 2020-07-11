using UnityEngine;
using System.Collections;
using UnityRawInput;

[RequireComponent(typeof(BoxCollider2D))]
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

    private void Awake()
    {
        collider = GetComponent<BoxCollider2D>();
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
                Jump();
               
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
                Jump();
        }
        rawMovementInput = movement;
    }

    public void Jump()
    {
        if (onGround)
            velocity.y = jumpVelocity;
        onGround = false;
    }
    private void FixedUpdate()
    {
        onGround = false;

        velocity = new Vector2(
            Mathf.Lerp(velocity.x, rawMovementInput.x * m_Speed, (1 - m_MoveDamping)),
            velocity.y - gravity * Time.fixedDeltaTime
        );

        //velocity.y = 0;

        Vector2 peneration = Vector2.zero;
        Vector2 time = -Vector2.one;
        var clampedVelocity = velocity;

        float TIME_THRESHOLD = -0.0001f;

        // down
        var (downPenetration, downTime) = CollisionCheck(Time.fixedDeltaTime, velocity, Vector2.down, Vector2.left);
        if(downTime > TIME_THRESHOLD)
        {
            time.y = downTime;
            peneration.y = downPenetration.y;
            clampedVelocity.y = velocity.y - downPenetration.y / Time.fixedDeltaTime;
        }
        var (upPenetration, upTime) = CollisionCheck(Time.fixedDeltaTime, velocity, Vector2.up, Vector2.left);
        if(upTime > TIME_THRESHOLD)
        {
            time.y = upTime;
            peneration.y = upPenetration.y;
            clampedVelocity.y = velocity.y - upPenetration.y / Time.fixedDeltaTime;
        }
        var (leftPen, leftTime) = CollisionCheck(Time.fixedDeltaTime, velocity, Vector2.left, Vector2.up);
        if(leftTime > TIME_THRESHOLD)
        {
            time.x = leftTime;
            clampedVelocity.x = velocity.x - leftPen.x / Time.fixedDeltaTime;
        }
        var (rightPen, rightTime) = CollisionCheck(Time.fixedDeltaTime, velocity, Vector2.right, Vector2.up);
        if (rightTime > TIME_THRESHOLD)
        {
            time.x = rightTime;
            clampedVelocity.x = velocity.x - rightPen.x / Time.fixedDeltaTime;
        }

        
        if((time.y <= time.x || time.x < 0) && time.y >= TIME_THRESHOLD)
        {
            if (velocity.y < 0)
                onGround = true;

            velocity.y = clampedVelocity.y;

            time.x = -1;
            (leftPen, leftTime) = CollisionCheck(Time.fixedDeltaTime, velocity, Vector2.left, Vector2.up);
            if (leftTime > TIME_THRESHOLD)
            {
                time.x = leftTime;
                clampedVelocity.x = velocity.x - leftPen.x / Time.fixedDeltaTime;
            }
            (rightPen, rightTime) = CollisionCheck(Time.fixedDeltaTime, velocity, Vector2.right, Vector2.up);
            if (rightTime > TIME_THRESHOLD)
            {
                time.x = rightTime;
                clampedVelocity.x = velocity.x - rightPen.x / Time.fixedDeltaTime;
            }

            if(time.x > TIME_THRESHOLD && time.x < Time.fixedDeltaTime)
            {
                velocity.x = clampedVelocity.x;
            }
        }
        else if ((time.x < time.y || time.y < 0) && time.x >= TIME_THRESHOLD)
        {
            velocity.x = clampedVelocity.x;

            time.y = -1;
            (downPenetration, downTime) = CollisionCheck(Time.fixedDeltaTime, velocity, Vector2.down, Vector2.left);
            if (downTime > TIME_THRESHOLD)
            {
                time.y = downTime;
                peneration.y = downPenetration.y;
                clampedVelocity.y = velocity.y - downPenetration.y / Time.fixedDeltaTime;
            }
            (upPenetration, upTime) = CollisionCheck(Time.fixedDeltaTime, velocity, Vector2.up, Vector2.left);
            if (upTime > TIME_THRESHOLD)
            {
                time.y = upTime;
                peneration.y = upPenetration.y;
                clampedVelocity.y = velocity.y - upPenetration.y / Time.fixedDeltaTime;
            }

            if(time.y > TIME_THRESHOLD && time.y < Time.fixedDeltaTime)
            {
                if (velocity.y < 0)
                    onGround = true;
                velocity.y = clampedVelocity.y;
            }
        }
        else
        {
            
        }

        transform.Translate(velocity.ToVector3() * Time.fixedDeltaTime);

        // Debug.Log(velocity);
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

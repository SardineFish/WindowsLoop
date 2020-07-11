using UnityEngine;
using System.Collections;
using UnityRawInput;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float m_Speed = 10;
    [SerializeField]
    [Range(0, 1)]
    private float m_MoveDamping = 1f;
    [SerializeField]
    private float m_JumpHeight = 3;
    [SerializeField]
    private float m_JumpTime = 0.5f;



    bool focused = true;
    Vector2 rawMovementInput;
    Vector2 movementInput;
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
        }
        rawMovementInput = movement;
    }
    private void FixedUpdate()
    {
        movementInput = new Vector2(
            Mathf.Lerp(movementInput.x, rawMovementInput.x, (1 - m_MoveDamping)),
            0
        );
        transform.Translate(movementInput.ToVector3() * m_Speed * Time.fixedDeltaTime);
        Debug.Log(movementInput);
    }
    private void OnApplicationFocus(bool focus)
    {
        focused = focus;
    }
}

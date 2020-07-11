using UnityEngine;
using System.Collections;

public class PlayerTracker : MonoBehaviour
{
    public bool EnableTrack = true;
    public Transform Target;
    [Range(0, 1)]
    public float Damping;
    public float MaxSpeed = 30;
    public Vector2 DeadRange;
    Vector2 snapPos;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void LateUpdate()
    {
        if(EnableTrack)
        {

            var halfSize = CameraManager.Instance.worldScreenSize / 2;
            var offset = MathUtility.Frac(halfSize);
            Debug.Log(offset);
            snapPos = MathUtility.Floor(Target.position.ToVector2()) + offset;
        }

        var pos = new Vector2(
            Mathf.Lerp(transform.position.x, snapPos.x, (1 - Damping)),
            Mathf.Lerp(transform.position.y, snapPos.y, (1 - Damping))
            );
        transform.position = pos;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, DeadRange * 2);
    }
}

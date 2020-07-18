using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class RaycastTest : MonoBehaviour
{
    public BoxCollider2D box;
    public Transform start;
    public Transform end;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(box && start && end)
        {
            Utility.DebugDrawRect(new Rect(box.transform.position.ToVector2() - box.size / 2, box.size), Color.green);

            var dir = (end.position - start.position).normalized.ToVector2();
            var (hit, distance, normal) = Utility.BoxRaycast(new Rect(box.transform.position.ToVector2() - box.size / 2, box.size), start.position, dir);
            Debug.DrawLine(start.position, end.position, Color.red);
            if (hit)
            {
                Debug.DrawLine(start.position.ToVector2(), start.position.ToVector2() + dir * distance, Color.red);
                Debug.DrawLine(start.position.ToVector2() + dir * distance, start.position.ToVector2() + dir * distance + normal);
            }
        }
    }
}

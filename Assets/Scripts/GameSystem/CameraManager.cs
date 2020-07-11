using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraManager : Singleton<CameraManager>
{
    [SerializeField]
    private int m_PreloadExtend = 2;

    [SerializeField]
    private bool m_EnablePreload = true;

    public RectInt outterViewRect
    {
        get
        {
            var halfSize = worldScreenSize / 2;
            RectInt rect = new RectInt();
            rect.min = MathUtility.FloorToInt(transform.position.ToVector2() - halfSize);
            rect.max = MathUtility.CeilToInt(transform.position.ToVector2() + halfSize);
            return rect;
        }
    }

    public RectInt innerViewRect
    {
        get
        {
            var halfSize = worldScreenSize / 2;
            RectInt rect = new RectInt();
            rect.min = MathUtility.CeilToInt(transform.position.ToVector2() - halfSize);
            rect.max = MathUtility.FloorToInt(transform.position.ToVector2() + halfSize);
            return rect;
        }
    }

    public Vector2 worldScreenSize => 
        camera 
        ? new Vector2(camera.pixelWidth / GameSystem.RenderTileSize, camera.pixelHeight / GameSystem.RenderTileSize) 
        : Vector2.zero;

    public RectInt preloadRect
    {
        get
        {
            var rect = outterViewRect;
            rect.min -= new Vector2Int(m_PreloadExtend, m_PreloadExtend);
            rect.max += new Vector2Int(m_PreloadExtend, m_PreloadExtend);
            return rect;
        }
    }

    public bool enablePreload => m_EnablePreload;

    public new Camera camera;

    private void Awake()
    {
        camera = GetComponent<Camera>();
    }

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
        GameMap.Instance.UpdateVisibleArea(preloadRect);
    }

    private void OnDrawGizmos()
    {
        var rect = outterViewRect;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(rect.center.ToVector3(), rect.size.ToVector3());

        rect = preloadRect;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(rect.center.ToVector3(), rect.size.ToVector3());

        rect = innerViewRect;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(rect.center.ToVector3(), rect.size.ToVector3());
    }
}

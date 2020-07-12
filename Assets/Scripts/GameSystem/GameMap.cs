using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameMap : Singleton<GameMap>
{
    public TileBase BlankTile;
    public Tilemap BaseMap;
    public Tilemap RuntimeMap;
    public Tilemap AttachMap;
    public Tilemap BackgroundMap;
    public Tilemap RuntimeBackgroundMap;
    public RectInt LoopArea;

    private RectInt previousActiveRect;
    // Start is called before the first frame update
    void Start()
    {
        RuntimeMap = CreateTilemapLayer("RuntimeTileMap");
        AttachMap = CreateTilemapLayer("AttachMap");
        RuntimeBackgroundMap = CreateTilemapLayer("RuntimeBackGround", false);
        RuntimeBackgroundMap.transform.Translate(new Vector3(0, 0, 10));
        BaseMap.gameObject.SetActive(false);


        if (!BackgroundMap)
            BackgroundMap = CreateTilemapLayer("Background", false);
        BackgroundMap.gameObject.SetActive(false);

    }

    Tilemap CreateTilemapLayer(string name, bool enableCollider = true)
    {
        var obj = new GameObject(name);
        var tilemap = obj.AddComponent<Tilemap>();
        obj.transform.parent = transform;
        obj.AddComponent<TilemapRenderer>();
        if(enableCollider)
        {
            var collider = obj.AddComponent<TilemapCollider2D>();
            collider.usedByComposite = true;
            var rigidbody = obj.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Static;
            var composite = obj.AddComponent<CompositeCollider2D>();
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.offsetDistance = 0.01f;
        }
        return tilemap;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position + LoopArea.center.ToVector3(), LoopArea.size.ToVector3());
    }

    public void UpdateVisibleArea(RectInt activeRect)
    {
        var min = MathUtility.Min(activeRect.min, previousActiveRect.min);
        var max = MathUtility.Max(activeRect.max, previousActiveRect.max);

        {
            int deltaXMin = previousActiveRect.xMin - activeRect.xMin;
            var sign = MathUtility.SignInt(deltaXMin);
            if(deltaXMin > 0)
            {
                for (int x = activeRect.xMin; x < previousActiveRect.xMin; ++x)
                    for (int y = activeRect.yMin; y < activeRect.yMax; y++)
                        SetRuntimTileAt(x, y);
            }
            else if (deltaXMin < 0)
            {
                for (int x = previousActiveRect.xMin; x < activeRect.xMin; ++x)
                    for (int y = min.y; y < max.y; y++)
                        RemoveRuntimeTileAt(x, y);
            }
        }

        {
            int deltaYMin = previousActiveRect.yMin - activeRect.yMin;
            if (deltaYMin > 0)
            {
                for (int x = activeRect.xMin; x < activeRect.xMax; x++)
                    for (int y = activeRect.yMin; y < previousActiveRect.yMax; ++y)
                        SetRuntimTileAt(x, y);
            }
            else if (deltaYMin < 0)
            {
                for (int x = min.x; x < max.x; x++)
                    for (int y = previousActiveRect.yMin; y < activeRect.yMin; ++y)
                        RemoveRuntimeTileAt(x, y);
            }
        }

        {
            var deltaXMax = activeRect.xMax - previousActiveRect.xMax;
            if(deltaXMax > 0)
            {
                for (int x = previousActiveRect.xMax; x < activeRect.xMax; ++x)
                    for (int y = activeRect.yMin; y < activeRect.yMax; y++)
                        SetRuntimTileAt(x, y);
            }
            else if (deltaXMax < 0)
            {
                for (int x = activeRect.xMax; x < previousActiveRect.xMax; ++x)
                    for (int y = min.y; y < max.y; y++)
                        RemoveRuntimeTileAt(x, y);
            }
        }

        {
            var deltaYMax = activeRect.yMax - previousActiveRect.yMax;
            if(deltaYMax > 0)
            {
                for (int x = activeRect.xMin; x < activeRect.xMax; x++)
                    for (int y = previousActiveRect.yMax; y < activeRect.yMax; ++y)
                        SetRuntimTileAt(x, y);
            }
            else if (deltaYMax < 0)
            {
                for (int x = min.x; x < max.x; ++x)
                    for (int y = activeRect.yMax; y < previousActiveRect.yMax; y++)
                        RemoveRuntimeTileAt(x, y);
            }
        }

        previousActiveRect = activeRect;
    }

    void SetRuntimTileAt(int x, int y)
    {
        var pos = new Vector3Int(
            FloorReminder(x - LoopArea.xMin, LoopArea.size.x),
            FloorReminder(y - LoopArea.yMin, LoopArea.size.y),
            0);
        pos += LoopArea.min.ToVector3Int();
        var tile = BaseMap.GetTile(pos);
        RuntimeMap.SetTile(new Vector3Int(x, y, 0), tile);
        tile = BackgroundMap.GetTile(pos);
        RuntimeBackgroundMap.SetTile(new Vector3Int(x, y, 0), tile);
    }

    void RemoveRuntimeTileAt(int x, int y)
    {
        RuntimeMap.SetTile(new Vector3Int(x, y, 0), null);
        RuntimeBackgroundMap.SetTile(new Vector3Int(x, y, 0), null);
    }

    public static int FloorReminder(int x, int m) =>
        x >= 0
        ? x % m
        : (m + x % m) % m;

    public TileBase GetTileAt(Vector2 pos)
    {
        return RuntimeMap.GetTile(pos.ToVector3Int());
    }

    public TileBase GetBaseTileAt(Vector2Int pos)
    {
        var pos3 = new Vector3Int(
            FloorReminder(pos.x - LoopArea.xMin, LoopArea.size.x),
            FloorReminder(pos.y - LoopArea.yMin, LoopArea.size.y),
            0);
        pos3 += LoopArea.min.ToVector3Int();

        var tile = BaseMap.GetTile(pos3);
        return tile;
    }

    public void SetAttachedTile(Vector2Int pos, TileBase tile)
    {
        RuntimeMap.SetTile(pos.ToVector3Int(), tile);
    }

    public void FillBorder(RectInt viewportRect)
    {
        var extend = CameraManager.Instance.preloadExtend;
        FillArea(new RectInt(viewportRect.xMin - extend, viewportRect.yMin - extend, extend, viewportRect.size.y + 2 * extend), BlankTile);
        FillArea(new RectInt(viewportRect.xMin, viewportRect.yMin - extend, viewportRect.size.x, extend), BlankTile);
        FillArea(new RectInt(viewportRect.xMax, viewportRect.yMin - extend, extend, viewportRect.size.y + 2 * extend), BlankTile);
        FillArea(new RectInt(viewportRect.xMin, viewportRect.yMax, viewportRect.size.x, extend), BlankTile);

    }

    public void ResetBorder(RectInt viewportRect)
    {
        previousActiveRect = viewportRect;
        UpdateVisibleArea(CameraManager.Instance.preloadRect);
    }

    void FillArea(RectInt rect, TileBase tile)
    {
        for(int x = rect.xMin; x < rect.xMax; ++x)
        {
            for(int y = rect.yMin; y < rect.yMax; ++y)
            {
                RuntimeMap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }
}

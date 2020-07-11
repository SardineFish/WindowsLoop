using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameMap : Singleton<GameMap>
{
    public Tilemap BaseMap;
    public Tilemap RuntimeMap;
    public RectInt LoopArea;

    private RectInt previousActiveRect;
    // Start is called before the first frame update
    void Start()
    {
        var obj = new GameObject("RuntimeTileMap");
        RuntimeMap = obj.AddComponent<Tilemap>();
        obj.transform.parent = transform;
        obj.AddComponent<TilemapRenderer>();
        BaseMap.gameObject.SetActive(false);
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
    }

    void RemoveRuntimeTileAt(int x, int y)
        => RuntimeMap.SetTile(new Vector3Int(x, y, 0), null);

    int FloorReminder(int x, int m) =>
        x >= 0
        ? x % m
        : (m + x % m) % m;

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameTileData
{
    int PID;
    public RectInt TileRange;
    public int[,] TileData;

    public TileBase GetTileAt(Vector2Int pos)
    {
        pos -= TileRange.min;
        var basePos = new Vector2Int(
            GameMap.FloorReminder(pos.x, TileRange.size.x),
            GameMap.FloorReminder(pos.y, TileRange.size.y));
        var instanceID = TileData[basePos.x, basePos.y];
        if (AssetManager.Instance.Assets.ContainsKey(instanceID))
            return AssetManager.Instance.Assets[instanceID] as TileBase;
        return null;
    }
}
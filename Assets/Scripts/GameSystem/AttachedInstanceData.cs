using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AttachedInstanceData
{
    public int PID;
    public RectInt TileRange;
    public RectInt ViewportTileRect;
    public RectInt RelativeTileRect;
    public int[,] TileData;
    public Vector2Int AttachPoint;
    public RectInt SyncTilesArea;

    public TileBase GetTileAt(Vector2Int pos)
    {
        pos -= TileRange.min;
        var basePos = new Vector2Int(
            GameMap.FloorReminder(pos.x, TileRange.size.x),
            GameMap.FloorReminder(pos.y, TileRange.size.y));
        var assetID = TileData[basePos.x, basePos.y];
        if (AssetManager.Instance.Assets.ContainsKey(assetID))
            return AssetManager.Instance.Assets[assetID] as TileBase;
        return null;
    }
}
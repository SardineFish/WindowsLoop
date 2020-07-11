using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowSnap;
using System.IO.MemoryMappedFiles;
using UnityEngine;

public struct GameInstanceData
{
    MemoryMappedViewAccessor page;
    public GameInstanceData(MemoryMappedViewAccessor page)
    {
        this.page = page;
    }

    public int PID
    {
        get => page.ReadInt32(GameDataAddr.PID);
        set => page.Write(GameDataAddr.PID, value);
    }


    public int AttachedPID
    {
        get => page.ReadInt32(GameDataAddr.AttachedWindowPID);
        set => page.Write(GameDataAddr.AttachedWindowPID, value);
    }

    public Vector2 PlayerPosition
    {
        get => ReadVector2(GameDataAddr.PlayerPosition);
        set => WriteVector2(GameDataAddr.PlayerPosition, value);
    }

    public Vector2 PlayerVelocity
    {
        get => ReadVector2(GameDataAddr.PlayerVelocity);
        set => WriteVector2(GameDataAddr.PlayerVelocity, value);
    }

    public Vector2Int WindowViewportScreenPosition
    {
        get => new Vector2Int(
            page.ReadInt32(GameDataAddr.ScreenSnapRectX),
            page.ReadInt32(GameDataAddr.ScreenSnapRectY));
    }

    public bool IsActiveInstance
    {
        get => page.ReadBoolean(GameDataAddr.ActiveInstance);
        set => page.Write(GameDataAddr.ActiveInstance, value);
    }

    public RectInt TileRange
    {
        get
        {
            RectInt rect = new RectInt();
            rect.min = ReadVector2Int(GameDataAddr.TileRangeMin);
            rect.max = ReadVector2Int(GameDataAddr.TileRangeMax);
            return rect;
        }
        set
        {
            WriteVector2Int(GameDataAddr.TileRangeMin, value.min);
            WriteVector2Int(GameDataAddr.TileRangeMax, value.max);
        }
    }

    

    public RectInt ViewRect
    {
        get
        {
            RectInt rect = new RectInt();
            rect.min = ReadVector2Int(GameDataAddr.ViewRectMin);
            rect.max = ReadVector2Int(GameDataAddr.ViewRectMax);

            return rect;
        }
        set
        {
            WriteVector2Int(GameDataAddr.ViewRectMin, value.min);
            WriteVector2Int(GameDataAddr.ViewRectMax, value.max);
        }
    }


    private void WriteVector2(long addr, Vector2 v)
    {
        page.Write(addr, v.x);
        page.Write(addr + 4, v.y);
    }

    private Vector2 ReadVector2(long addr)
    {
        Vector2 v;
        v.x = page.ReadSingle(addr);
        v.y = page.ReadSingle(addr + 4);
        return v;
    }

    private void WriteVector2Int(long addr, Vector2Int v)
    {
        page.Write(addr, v.x);
        page.Write(addr + 4, v.y);
    }

    private Vector2Int ReadVector2Int(long addr)
    {
        Vector2Int v = new Vector2Int();
        v.x = page.ReadInt32(addr);
        v.y = page.ReadInt32(addr + 4);
        return v;
    }

    public int[,] ReadTileData()
    {
        var range = TileRange;
        var data = new int[range.size.x, range.size.y];
        var expand = page.ReadArray<int>(GameDataAddr.TileData, data.Length);
        Buffer.BlockCopy(expand, 0, data, 0, data.Length);

        return data;
    }

    public void WriteTileData(int [,] data)
    {
        var expand = new int[data.Length];
        Buffer.BlockCopy(data, 0, expand, 0, data.Length);
        page.WriteArray(GameDataAddr.TileData, expand, 0, data.Length);

    }

    public void Flush()
    {
        page.Flush();
    }
}

public static class PublicData
{
    static Lazy<MemoryMappedViewAccessor> cachedPage = new Lazy<MemoryMappedViewAccessor>(() => SharedMemory.GetPage(0));
    static MemoryMappedViewAccessor page => cachedPage.Value;
    public static int ActiveInstancePID
    {
        get => page.ReadInt32(PublicDataAddr.ActiveInstancePID);
        set => page.Write(PublicDataAddr.ActiveInstancePID, value);
    }

    public static void Flush() => page.Flush();
}


public class GameDataAddr : WindowSnap.Address
{
    public const int ViewRectMin =      PreserveEnd + 0x0;
    public const int ViewRectMax =      PreserveEnd + 0x8;
    public const int PlayerPosition =   PreserveEnd + 0x10;
    public const int PlayerVelocity =   PreserveEnd + 0x18;
    public const int PlayerAnimTime =   PreserveEnd + 0x20;
    public const int CameraPos =        PreserveEnd + 0x24;
    public const int ActiveInstance =   PreserveEnd + 0x2c;
    public const int TileRangeMin =     PreserveEnd + 0x30;
    public const int TileRangeMax =     PreserveEnd + 0x38;

    public const int TileData =         PreserveEnd + 0x100;
}

public class PublicDataAddr
{
    public const int UserData = 2048;
    public const int ActiveInstancePID = UserData + 0x0;
}

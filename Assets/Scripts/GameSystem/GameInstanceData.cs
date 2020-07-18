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

    public bool Valid => page != null;

    public int PID
    {
        get => page.ReadInt32(GameDataAddr.PID);
        set => page.Write(GameDataAddr.PID, value);
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

    public int ConnectActiveThrough
    {
        get => page.ReadInt32(GameDataAddr.ConnectToActiveThrough);
        set => page.Write(GameDataAddr.ConnectToActiveThrough, value);
    }

    public Vector2Int TileOffsetToActiveInstance
    {
        get => ReadVector2Int(GameDataAddr.TileOffsetToActiveInstance);
        set => WriteVector2Int(GameDataAddr.TileOffsetToActiveInstance, value);
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
        Buffer.BlockCopy(expand, 0, data, 0, data.Length * sizeof(int));

        return data;
    }

    public void WriteTileData(int [,] data)
    {
        var expand = new int[data.Length];
        Buffer.BlockCopy(data, 0, expand, 0, data.Length * sizeof(int));
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

    public static bool IsWalking
    {
        get => page.ReadBoolean(PublicDataAddr.IsWalking);
        set => page.Write(PublicDataAddr.IsWalking, value);
    }

    public static bool IsJumped
    {
        get => page.ReadBoolean(PublicDataAddr.IsJumped);
        set => page.Write(PublicDataAddr.IsJumped, value);

    }
    public static bool IsLanded
    {
        get => page.ReadBoolean(PublicDataAddr.IsLanded);
        set => page.Write(PublicDataAddr.IsLanded, true);
    }

    public static int GameStage
    {
        get => page.ReadInt32(PublicDataAddr.LevelState);
        set => page.Write(PublicDataAddr.LevelState, value);
    }

    public static int GetScenePID(int index)
    {
        if(index >= PublicDataAddr.MaxScenes)
            throw new IndexOutOfRangeException();

        return page.ReadInt32(PublicDataAddr.ScenesPID + index * sizeof(int));
    }

    public static void SetScenePID(int index, int pid)
    {
        page.Write(PublicDataAddr.ScenesPID + index * sizeof(int), pid);
    }

    public static void Flush() => page.Flush();
}


public class GameDataAddr : WindowSnap.Address
{
    public const int ViewRectMin =      Preserve + 0x0;     // int2
    public const int ViewRectMax =      Preserve + 0x8;     // int2
    public const int PlayerPosition =   Preserve + 0x10;    // float2
    public const int PlayerVelocity =   Preserve + 0x18;    // float2
    public const int PlayerAnimTime =   Preserve + 0x20;    // float
    public const int CameraPos =        Preserve + 0x24;    // float2
    public const int ActiveInstance =   Preserve + 0x2c;    // bool
    public const int TileRangeMin =     Preserve + 0x30;    // int2
    public const int TileRangeMax =     Preserve + 0x38;    // int2
    public const int ConnectToActiveThrough =       Preserve + 0x40; // int
    public const int TileOffsetToActiveInstance =   Preserve + 0x44; // int2

    public const int TileData =         Preserve + 0x100;
}

public class PublicDataAddr
{
    public const int UserData = 2048;
    public const int ActiveInstancePID = UserData + 0x0; // int
    public const int IsWalking = UserData + 0x4; // bool
    public const int IsJumped = UserData + 0x8; // bool
    public const int IsLanded = UserData + 0xc; // bool
    public const int LevelState = UserData + 0x10; // int

    public const int MaxScenes = 4;
    public const int ScenesPID = UserData + 0x14; // int[MaxScenes]
}

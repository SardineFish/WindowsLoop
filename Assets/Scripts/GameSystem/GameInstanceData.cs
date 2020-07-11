using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowSnap;
using System.IO.MemoryMappedFiles;
using UnityEngine;

public class GameInstanceData
{
    MemoryMappedViewAccessor page;
    public GameInstanceData(MemoryMappedViewAccessor page)
    {
        this.page = page;
    }

    public int PID
    {
        get => page.ReadInt32(Address.PID);
        set => page.Write(Address.PID, value);
    }


    public int AttachedPID
    {
        get => page.ReadInt32(Address.AttachedWindowPID);
        set => page.Write(Address.AttachedWindowPID, value);
    }

    public Vector2 PlayerPosition
    {
        get => ReadVector2(Address.PlayerPosition);
        set => WriteVector2(Address.PlayerPosition, value);
    }

    public Vector2 PlayerVelocity
    {
        get => ReadVector2(Address.PlayerVelocity);
        set => WriteVector2(Address.PlayerVelocity, value);
    }

    public Vector2Int WindowViewportScreenPosition
    {
        get => new Vector2Int(
            page.ReadInt32(Address.ScreenSnapRectX),
            page.ReadInt32(Address.ScreenSnapRectY));
    }

    public RectInt ViewRect
    {
        get
        {
            RectInt rect = new RectInt();
            rect.xMin = page.ReadInt32(Address.ViewRectMin);
            rect.yMin = page.ReadInt32(Address.ViewRectMin + 4);
            rect.xMax = page.ReadInt32(Address.ViewRectMax);
            rect.xMin = page.ReadInt32(Address.ViewRectMax + 4);

            return rect;
        }
        set
        {
            page.Write(Address.ViewRectMin, value.xMin);
            page.Write(Address.ViewRectMin + 4, value.yMin);
            page.Write(Address.ViewRectMax, value.xMax);
            page.Write(Address.ViewRectMax + 4, value.yMax);
        }
    }


    private void WriteVector2(long addr, Vector2 v)
    {
        page.Write(addr, v.x);
        page.Write(addr, v.y);
    }

    private Vector2 ReadVector2(long addr)
    {
        Vector2 v;
        v.x = page.ReadSingle(addr);
        v.y = page.ReadSingle(addr + 4);
        return v;
    }

    public void Flush()
    {
        page.Flush();
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using static LibTest.Native;

namespace LibTest
{
    public static class Foo
    {
        const int SnapThreshold = 40;
        const int ClientWidth = 600;
        const int ClientHeight = 400;
        const int SnapRectWidth = ClientWidth;
        const int SnapRectHeight = ClientHeight;
        const int TileSize = 40;
        const string UnityWindowClassName = "UnityWndClass";
        public static readonly int WindowWidth;
        public static readonly int WindowHeight;
        public static readonly int ClientOffsetX;
        public static readonly int ClientOffsetY;
        public static readonly int PID;
        public static bool SnapWhileMoving { get; set; } = false;
        static readonly IntPtr originalWndProcPtr;
        static readonly WndProc wndProcDelegate;
        static IntPtr hWnd;
        static RECT clientSnapRect;
        //static RECT screenSnapRect;
        static Action<int, Vec2> Attached; // (pid, position)
        internal static Action<string> Log;
        static int dragOffsetX;
        static int dragOffsetY;

        static Foo()
        {
            PID = Process.GetCurrentProcess().Id;
            var threadId = GetCurrentThreadId();
            EnumThreadWindows(threadId, (hWnd, lParam) =>
            {
                var classText = new StringBuilder(UnityWindowClassName.Length + 1);
                GetClassName(hWnd, classText, classText.Capacity);
                if (classText.ToString() == UnityWindowClassName)
                {
                    Foo.hWnd = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            GetWindowRect(hWnd, out var windowRect);
            GetClientScreenRect(hWnd, out var clientRect);
            WindowWidth = windowRect.Width;
            WindowHeight = windowRect.Height;
            ClientOffsetX = clientRect.Left - windowRect.Left;
            ClientOffsetY = clientRect.Top - windowRect.Top;
            wndProcDelegate = new WndProc(WndProc);
            originalWndProcPtr = SetWindowLongPtr(hWnd, -4, Marshal.GetFunctionPointerForDelegate(wndProcDelegate));
        }

        public static void Init()
        {
            SharedMemory.Init();
            SharedMemory.Self.Write(Address.PID, PID);
            SharedMemory.Self.Flush();
            UpdateScreenSnapRect();
            Log($"Window Handle: {hWnd}");
            Log($"Page: {SharedMemory.selfIndex}");
            Log($"Others Count: {SharedMemory.Others.Count}");
        }

        public static List<Rect> GetAdjacentWindowBounds()
        {
            throw new NotImplementedException();
        }
        public static void SetSnapRect(Rect clientSnapRect)
        {
            Foo.clientSnapRect = new RECT
            {
                Left = (int)Math.Round(clientSnapRect.Min.X),
                Top = (int)Math.Round(clientSnapRect.Min.Y),
                Right = (int)Math.Round(clientSnapRect.Max.X),
                Bottom = (int)Math.Round(clientSnapRect.Max.Y),
            };
            Foo.clientSnapRect = new RECT
            {
                Left = 0,
                Top = 0,
                Right = 600,
                Bottom = 400,
            };
            UpdateScreenSnapRect();
        }
        static void Snap()
        {

        }
        static void UpdateScreenSnapRect()
        {
            var screenSnapRect = GetScreenSnapRect();
            SharedMemory.Self.Write(Address.ScreenSnapRectX, screenSnapRect.Left);
            SharedMemory.Self.Write(Address.ScreenSnapRectY, screenSnapRect.Top);
            SharedMemory.Self.Flush();
        }
        static RECT GetScreenSnapRect()
        {
            GetClientScreenRect(hWnd, out var clientRect);
            return new RECT
            {
                Left = clientSnapRect.Left + clientRect.Left,
                Top = clientSnapRect.Top + clientRect.Top,
                Right = clientSnapRect.Right + clientRect.Left,
                Bottom = clientSnapRect.Right + clientRect.Top,
            };
        }

        static bool InSnapRange(int pos, int edge)
        {
            int delta = Math.Abs(pos - edge);
            return delta <= SnapThreshold;
        }

        static List<MemoryMappedViewAccessor> otherPages;
        static IntPtr WndProc(IntPtr hWnd, WM msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
            case WM.ENTERSIZEMOVE:
            case WM.SIZE:
                {
                    GetWindowRect(hWnd, out var windowRect);
                    GetCursorPos(out var cursor);
                    dragOffsetX = cursor.X - windowRect.Left;
                    dragOffsetY = cursor.Y - windowRect.Top;
                    otherPages = SharedMemory.Others;
                    break;
                }
            case WM.MOVING:
                if (SnapWhileMoving)
                    goto case WM.EXITSIZEMOVE;
                break;
            case WM.EXITSIZEMOVE:
                {
                    GetCursorPos(out var cursor);
                    var windowRect = new RECT
                    {
                        Left = cursor.X - dragOffsetX,
                        Top = cursor.Y - dragOffsetY,
                        Right = cursor.X - dragOffsetX + WindowWidth,
                        Bottom = cursor.Y - dragOffsetY + WindowHeight,
                    };
                    var screenSnapRect = windowRect
                        .Translate(ClientOffsetX, ClientOffsetY)
                        .Translate(clientSnapRect.Left, clientSnapRect.Top);

                    var snaped = false;
                    var targetScreenSnapRect = new RECT();
                    var attachedWindowPID = 0;
                    foreach (var page in otherPages)
                    {
                        targetScreenSnapRect = new RECT
                        {
                            Left = page.ReadInt32(Address.ScreenSnapRectX),
                            Top = page.ReadInt32(Address.ScreenSnapRectY),
                            Right = page.ReadInt32(Address.ScreenSnapRectX) + SnapRectWidth,
                            Bottom = page.ReadInt32(Address.ScreenSnapRectY) + SnapRectHeight,
                        };
                        if (InSnapRange(screenSnapRect.Left, targetScreenSnapRect.Right) &&
                            screenSnapRect.Top < targetScreenSnapRect.Bottom &&
                            screenSnapRect.Bottom > targetScreenSnapRect.Top)
                            screenSnapRect = screenSnapRect.MoveTo(
                                x: targetScreenSnapRect.Right);
                        else if (InSnapRange(screenSnapRect.Top, targetScreenSnapRect.Bottom) &&
                            screenSnapRect.Left < targetScreenSnapRect.Right &&
                            screenSnapRect.Right > targetScreenSnapRect.Left)
                            screenSnapRect = screenSnapRect.MoveTo(
                                y: targetScreenSnapRect.Bottom);
                        else if (InSnapRange(screenSnapRect.Right, targetScreenSnapRect.Left) &&
                            screenSnapRect.Top < targetScreenSnapRect.Bottom &&
                            screenSnapRect.Bottom > targetScreenSnapRect.Top)
                            screenSnapRect = screenSnapRect.MoveTo(
                                x: targetScreenSnapRect.Left - SnapRectWidth);
                        else if (InSnapRange(screenSnapRect.Bottom, targetScreenSnapRect.Top) &&
                            screenSnapRect.Left < targetScreenSnapRect.Right &&
                            screenSnapRect.Right > targetScreenSnapRect.Left)
                            screenSnapRect = screenSnapRect.MoveTo(
                                y: targetScreenSnapRect.Top - SnapRectHeight);
                        else
                            continue;
                        snaped = true;
                        attachedWindowPID = page.ReadInt32(Address.PID);
                        break;
                    }
                    if (snaped)
                    {
                        var offsetX = (double)targetScreenSnapRect.Left % TileSize;
                        var offsetY = (double)targetScreenSnapRect.Top % TileSize;
                        var x = (int)(
                            Math.Round((screenSnapRect.Left - offsetX) / TileSize) * TileSize + offsetX);
                        var y = (int)(
                            Math.Round((screenSnapRect.Top - offsetY) / TileSize) * TileSize + offsetY);
                        windowRect = screenSnapRect
                            .MoveTo(x, y)
                            .Translate(-ClientOffsetX, -ClientOffsetY)
                            .Translate(-clientSnapRect.Left, -clientSnapRect.Top);
                    }
                    if (msg == WM.MOVING)
                    {
                        Marshal.StructureToPtr(windowRect, lParam, false);
                    }
                    else if (snaped)
                    {
                        SetWindowPos(
                            hWnd,
                            (IntPtr)SpecialWindowHandles.HWND_TOP,
                            windowRect.Left, windowRect.Top,
                            windowRect.Width, windowRect.Height,
                            SetWindowPosFlags.ShowWindow);
                    }
                    UpdateScreenSnapRect();
                    var lastAttachedWindowPage =
                        SharedMemory.GetPageByPID(SharedMemory.Self.ReadInt32(Address.AttachedWindowPID));
                    lastAttachedWindowPage.Write(Address.AttachedWindowPID, 0);
                    lastAttachedWindowPage.Write(Address.AttachmentChanged, true);
                    lastAttachedWindowPage.Flush();
                    var attachedWindowPage = SharedMemory.GetPageByPID(attachedWindowPID);
                    attachedWindowPage.Write(Address.AttachedWindowPID, PID);
                    attachedWindowPage.Write(Address.AttachmentChanged, true);
                    attachedWindowPage.Flush();
                    SharedMemory.Self.Write(Address.AttachedWindowPID, attachedWindowPID);
                    SharedMemory.Self.Flush();
                    Log($"Attached({attachedWindowPID})");
                    Attached(
                        attachedWindowPID,
                        attachedWindowPID == 0
                        ? new Vec2()
                        : new Vec2(
                            targetScreenSnapRect.Left,
                            targetScreenSnapRect.Top));
                    break;
                }
            case WM.CLOSE:
                Process.GetCurrentProcess().Kill();
                break;
            }
            return CallWindowProc(originalWndProcPtr, hWnd, msg, wParam, lParam);
        }

        public static void SetSnapCallback(Action<int, Vec2> callback)
        {
            Attached = callback;
        }
        public static void SetLogCallback(Action<string> callback)
        {
            Log = callback;
            Init();
        }
        public static void TickPerSecond()
        {
            //Log($"Screen Snap Rect: {GetScreenSnapRect().Left}, {GetScreenSnapRect().Top}");
            TickPerFrame();
        }
        public static void TickPerFrame()
        {
            if (SharedMemory.Self.ReadBoolean(Address.AttachmentChanged))
            {
                SharedMemory.Self.Write(Address.AttachmentChanged, false);
                SharedMemory.Self.Flush();
                var attachedWindowPID = SharedMemory.Self.ReadInt32(Address.AttachedWindowPID);
                var page = SharedMemory.GetPageByPID(attachedWindowPID);
                Log($"Attached({attachedWindowPID})");
                Attached(
                    attachedWindowPID,
                    attachedWindowPID == 0
                    ? new Vec2()
                    : new Vec2(
                        page.ReadInt32(Address.ScreenSnapRectX),
                        page.ReadInt32(Address.ScreenSnapRectY)));
            }
        }
    }
}

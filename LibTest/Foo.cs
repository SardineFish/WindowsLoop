using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using static LibTest.Native;

namespace LibTest
{
    public class Foo
    {
        const int SnapThreshold = 40;
        const int ClientWidth = 600;
        const int ClientHeight = 400;
        const int SnapRectWidth = ClientWidth - TileSize;
        const int SnapRectHeight = ClientHeight - TileSize;
        const int TileSize = 40;
        const string UnityWindowClassName = "UnityWndClass";
        static readonly int WindowWidth;
        static readonly int WindowHeight;
        static readonly int ClientOffsetX;
        static readonly int ClientOffsetY;
        static readonly int PID;
        static readonly IntPtr originalWndProcPtr;
        static readonly WndProc wndProcDelegate;
        static IntPtr hWnd;
        static RECT clientSnapRect;
        //static RECT screenSnapRect;
        static Action<int, Vec2> Snaped; // (pid, position)
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

        static void Init()
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
    Left = 20,
    Top = 20,
    Right = 580,
    Bottom = 380,
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
                    Marshal.StructureToPtr(windowRect, lParam, false);
                    SharedMemory.Self.Write(Address.Snaped, snaped);
                    SharedMemory.Self.Flush();
                    UpdateScreenSnapRect();
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
            Snaped = callback;
        }
        public static void SetLogCallback(Action<string> callback)
        {
            Log = callback;
            Init();
        }
        public static void TickPerSecond()
        {
            Log($"Screen Snap Rect: {GetScreenSnapRect().Left}, {GetScreenSnapRect().Top}");
        }
    }
}

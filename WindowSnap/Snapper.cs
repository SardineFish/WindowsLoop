using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static WindowSnap.Native;

namespace WindowSnap
{
    public static class Snapper
    {
        const int SnapThreshold = 40;
        const int ClientWidth = 600;
        const int ClientHeight = 400;
        const int AttachedWindowPIDsCapacity = 8;
        const int SnapRectWidth = ClientWidth;
        const int SnapRectHeight = ClientHeight;
        const int TileSize = 40;
        const string UnityWindowClassName = "UnityWndClass";
        public static readonly int WindowWidth;
        public static readonly int WindowHeight;
        public static readonly int ClientOffsetX;
        public static readonly int ClientOffsetY;
        public static readonly int PID;
        public static SharedMemoryDataPage SelfPage;
        public static bool SnapWhileMoving { get; set; } = false;
        static readonly IntPtr originalWndProcPtr;
        static readonly WndProc wndProcDelegate;
        static IntPtr hWnd;
        static RECT clientSnapRect;
        public static event Action<int, Vec2> OnAttached;
        public static event Action<int> OnDetached;
        internal static Action<string> Log;
        static int dragOffsetX;
        static int dragOffsetY;

        static Snapper()
        {
            PID = Process.GetCurrentProcess().Id;
            var threadId = GetCurrentThreadId();
            EnumThreadWindows(threadId, (hWnd, lParam) =>
            {
                var classText = new StringBuilder(UnityWindowClassName.Length + 1);
                GetClassName(hWnd, classText, classText.Capacity);
                if (classText.ToString() == UnityWindowClassName)
                {
                    Snapper.hWnd = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            if (hWnd != IntPtr.Zero)
            {
                GetWindowRect(hWnd, out var windowRect);
                GetClientScreenRect(hWnd, out var clientRect);
                WindowWidth = windowRect.Width;
                WindowHeight = windowRect.Height;
                ClientOffsetX = clientRect.Left - windowRect.Left;
                ClientOffsetY = clientRect.Top - windowRect.Top;
                wndProcDelegate = new WndProc(WndProc);
                originalWndProcPtr = SetWindowLongPtr(hWnd, -4, Marshal.GetFunctionPointerForDelegate(wndProcDelegate));
            }
        }

        public static void Init()
        {
            SharedMemory.Init();
            SelfPage = SharedMemoryDataPage.Self;
            SelfPage.PID = PID;
            clientSnapRect = new RECT
            {
                Left = 0,
                Top = 0,
                Right = 600,
                Bottom = 400,
            };
            UpdateScreenSnapRect();
            Log($"Window Handle: {hWnd}");
            Log($"Page: {SharedMemory.selfIndex}");
            Log($"Others Count: {SharedMemory.Others.Count}");
        }

        static void UpdateScreenSnapRect()
        {
            var screenSnapRect = GetScreenSnapRect();
            SelfPage.ScreenSnapRectX = screenSnapRect.Left;
            SelfPage.ScreenSnapRectY = screenSnapRect.Top;
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
            try
            {
                switch (msg)
                {
                case WM.ENTERSIZEMOVE:
                case WM.SIZE:
                    {
                        Log($"EnterMove");
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
                        if (otherPages == null)
                        {
                            break;
                        }
                        Log($"ExitMove");
                        GetCursorPos(out var cursor);
                        var windowRect = new RECT
                        {
                            Left = cursor.X - dragOffsetX,
                            Top = cursor.Y - dragOffsetY,
                            Right = cursor.X - dragOffsetX + WindowWidth,
                            Bottom = cursor.Y - dragOffsetY + WindowHeight,
                        };
                        var screenSnapRect = WindowRectToScreenSnapRect(windowRect);

                        var snaped = false;
                        var targetScreenSnapRect = new RECT();
                        var attachedWindowPID = 0;
                        foreach (var accessor in otherPages)
                        {
                            var page = new SharedMemoryDataPage(accessor);
                            targetScreenSnapRect = new RECT
                            {
                                Left = page.ScreenSnapRectX,
                                Top = page.ScreenSnapRectY,
                                Right = page.ScreenSnapRectX + SnapRectWidth,
                                Bottom = page.ScreenSnapRectY + SnapRectHeight,
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
                            attachedWindowPID = page.PID;
                            break;
                        }
                        Log($"EdgeSnap");
                        if (snaped)
                        {
                            var offsetX = targetScreenSnapRect.Left % TileSize;
                            var offsetY = targetScreenSnapRect.Top % TileSize;
                            screenSnapRect = screenSnapRect.SnapToGrid(TileSize, TileSize, offsetX, offsetY);
                            windowRect = ScreenSnapRectToWindowRect(screenSnapRect);
                        }
                        Log($"GridSnap");
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
                        if (msg == WM.EXITSIZEMOVE)
                        {
                            Log(SharedMemory.PageTable.Aggregate("", (a, b) => $"{a}{b} "));
                            // Notify all attached windows
                            foreach (var remotePID in
                                SharedMemory.Self.ReadArray<int>(Address.AttachedWindowPIDs, AttachedWindowPIDsCapacity)
                                .Where(pid => pid > 0))
                            {
                                var remotePage = new SharedMemoryDataPage(SharedMemory.GetPageByPID(remotePID));
                                if (remotePage != null)
                                {
                                    remotePage.DetachmentChanged = true;
                                    remotePage.MessageParam2 = PID;
                                    OnDetached?.Invoke(remotePID);
                                    Log($"Detached({remotePID})");
                                }
                            }
                            Log($"Notify other windows");
                            // Clear attached windows list
                            var newAttachedWindowPIDs = new int[AttachedWindowPIDsCapacity];
                            SharedMemory.Self.WriteArray(
                                Address.AttachedWindowPIDs,
                                newAttachedWindowPIDs, 0, AttachedWindowPIDsCapacity);
                            SharedMemory.Self.Flush();
                            Log($"Clear attached windows list");

                            if (attachedWindowPID > 0)
                            {
                                var attachedWindowPage =
                                    new SharedMemoryDataPage(SharedMemory.GetPageByPID(attachedWindowPID));
                                if (attachedWindowPage == null)
                                {
                                    Log($"Fatal: page table corrupted");
                                }
                                attachedWindowPage.AttachmentChanged = true;
                                attachedWindowPage.MessageParam1 = PID;

                                SharedMemory.Self.Write(Address.AttachedWindowPIDs, attachedWindowPID);
                                SharedMemory.Self.Flush();

                                var relativePosition = new Vec2(
                                targetScreenSnapRect.Left - screenSnapRect.Left,
                                targetScreenSnapRect.Top - screenSnapRect.Top);
                                OnAttached?.Invoke(attachedWindowPID, relativePosition);
                                Log($"Attached({attachedWindowPID}, [{relativePosition.X}, {relativePosition.Y}])");
                            }
                        }
                        break;
                    }
                case WM.CLOSE:
                    Process.GetCurrentProcess().Kill();
                    break;
                }
            }
            catch (Exception ex)
            {
                Log($"Exception occured in WndProc: {ex.Message}");
            }
            return CallWindowProc(originalWndProcPtr, hWnd, msg, wParam, lParam);
        }

        public static void SetLogCallback(Action<string> callback)
        {
            Log = callback;
        }
        public static void TickPerFrame()
        {
            if (SelfPage.DetachmentChanged)
            {
                SelfPage.DetachmentChanged = false;
                var remotePID = SelfPage.MessageParam2;
                var attachedWindowPIDs =
                    SharedMemory.Self.ReadArray<int>(Address.AttachedWindowPIDs, AttachedWindowPIDsCapacity);
                attachedWindowPIDs.Unset(remotePID);
                SharedMemory.Self.WriteArray(Address.AttachedWindowPIDs, attachedWindowPIDs, 0, AttachedWindowPIDsCapacity);
                SharedMemory.Self.Flush();
                OnDetached?.Invoke(remotePID);
                Log($"Detached({remotePID})");
            }
            if (!SelfPage.DetachmentChanged && SelfPage.AttachmentChanged)
            {
                SelfPage.AttachmentChanged = false;
                var remotePID = SelfPage.MessageParam1;
                var relativePosition = GetRelativePos(remotePID);
                var attachedWindowPIDs =
                    SharedMemory.Self.ReadArray<int>(Address.AttachedWindowPIDs, AttachedWindowPIDsCapacity);
                attachedWindowPIDs.Set(remotePID);
                SharedMemory.Self.WriteArray(Address.AttachedWindowPIDs, attachedWindowPIDs, 0, AttachedWindowPIDsCapacity);
                SharedMemory.Self.Flush();
                OnAttached?.Invoke(remotePID, relativePosition);
                Log($"Attached({remotePID}, [{relativePosition.X}, {relativePosition.Y}])");
            }
        }

        public static Vec2 GetRelativePos(int pid)
        {
            var page = SharedMemory.GetPageByPID(pid);
            return page == null ? new Vec2() : new Vec2(
                page.ReadInt32(Address.ScreenSnapRectX) - GetScreenSnapRect().Left,
                page.ReadInt32(Address.ScreenSnapRectY) - GetScreenSnapRect().Top);
        }

        static RECT ScreenSnapRectToWindowRect(RECT screenSnapRect) =>
            screenSnapRect
                .Translate(-ClientOffsetX, -ClientOffsetY)
                .Translate(-clientSnapRect.Left, -clientSnapRect.Top);
        static RECT WindowRectToScreenSnapRect(RECT windowRect) =>
            windowRect
                .Translate(ClientOffsetX, ClientOffsetY)
                .Translate(clientSnapRect.Left, clientSnapRect.Top);
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static WindowSnap.Logger;
using static WindowSnap.Native;

namespace WindowSnap
{
    public static class Snapper
    {
        public const int SnapThreshold = 40;
        public const int ClientWidth = 600;
        public const int ClientHeight = 400;
        public const int SnapRectWidth = ClientWidth;
        public const int SnapRectHeight = ClientHeight;
        const int AttachedWindowPIDsCapacity = 8;
        const int TileSize = 40;
        const string UnityWindowClassName = "UnityWndClass";
        public static readonly int WindowWidth;
        public static readonly int WindowHeight;
        public static readonly int ClientOffsetX;
        public static readonly int ClientOffsetY;
        public static readonly int PID;
        public static SharedMemoryDataPage SelfPage;
        public static bool SnapWhileMoving { get => true; set { } }
        static readonly IntPtr originalWndProcPtr;
        static readonly WndProc wndProcDelegate;
        static IntPtr hWnd;
        static RECT clientSnapRect = new RECT
        {
            Left = 0,
            Top = 0,
            Right = 600,
            Bottom = 400,
        };
        public static event Action<int, Vec2> OnAttached;
        public static event Action<int> OnDetached;
        static int dragOffsetX;
        static int dragOffsetY;

        static Snapper()
        {
            SharedMemory.Init();
            SelfPage = SharedMemoryDataPage.Self;

            SelfPage.PID = PID = Process.GetCurrentProcess().Id;
            SelfPage.WindowHandle = (int)(hWnd = GetWindowHandle());

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
            UpdateScreenSnapRect();

            LogInfo($"Page ID: {SharedMemory.selfIndex}");
            LogInfo($"Others Count: {SharedMemory.Others.Count}");
        }

        [Obsolete("This method is no longer used.")]
        public static void Init() { }

        static IntPtr GetWindowHandle()
        {
            var returnHWnd = IntPtr.Zero;
            var threadId = GetCurrentThreadId();
            EnumThreadWindows(threadId, (hWnd, lParam) =>
            {
                var classText = new StringBuilder(UnityWindowClassName.Length + 1);
                GetClassName(hWnd, classText, classText.Capacity);
                if (classText.ToString() == UnityWindowClassName)
                {
                    returnHWnd = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return returnHWnd;
        }

        static void UpdateScreenSnapRect()
        {
            if (SelfPage.WindowHandle > 0)
            {
                var screenSnapRect = GetScreenSnapRect();
                SelfPage.ScreenSnapRectX = screenSnapRect.Left;
                SelfPage.ScreenSnapRectY = screenSnapRect.Top;
            }
            else
            {
                SelfPage.ScreenSnapRectX = -32000;
                SelfPage.ScreenSnapRectY = -32000;
            }
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
                            break;

                        GetCursorPos(out var cursor);
                        var windowRect = new RECT
                        {
                            Left = cursor.X - dragOffsetX,
                            Top = cursor.Y - dragOffsetY,
                            Right = cursor.X - dragOffsetX + WindowWidth,
                            Bottom = cursor.Y - dragOffsetY + WindowHeight,
                        };
                        var screenSnapRect = WindowRectToScreenSnapRect(windowRect);

                        var snapped = false;
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
                            if (snapped = screenSnapRect.SnapToRect(targetScreenSnapRect, out var snappedRect))
                            {
                                screenSnapRect = snappedRect;
                                attachedWindowPID = page.PID;
                                break;
                            }
                        }

                        if (snapped)
                        {
                            var offsetX = targetScreenSnapRect.Left % TileSize;
                            var offsetY = targetScreenSnapRect.Top % TileSize;
                            screenSnapRect = screenSnapRect.SnapToGrid(TileSize, offsetX, offsetY);
                            windowRect = ScreenSnapRectToWindowRect(screenSnapRect);
                        }

                        if (msg == WM.MOVING)
                            Marshal.StructureToPtr(windowRect, lParam, false);
                        else if (snapped)
                            SetWindowPos(hWnd, windowRect);

                        UpdateScreenSnapRect();

                        if (msg == WM.EXITSIZEMOVE)
                        {
                            LogInfo(SharedMemory.PageTable.Aggregate("", (a, b) => $"{a}{b} "));

                            DetachAll();

                            if (attachedWindowPID > 0)
                            {
                                Attach(attachedWindowPID);
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
                LogError($"Exception occured in WndProc: {ex.Message}");
            }
            return CallWindowProc(originalWndProcPtr, hWnd, msg, wParam, lParam);
        }

        static void Attach(int attachedWindowPID)
        {
            var attachedWindowPage = new SharedMemoryDataPage(SharedMemory.GetPageByPID(attachedWindowPID));
            if (attachedWindowPage != null)
            {
                attachedWindowPage.AttachmentChanged = true;
                attachedWindowPage.MessageParam1 = PID;
                SharedMemory.Self.Write(Address.AttachedWindowPIDs, attachedWindowPID);
                SharedMemory.Self.Flush();
                var relativePosition = GetRelativePos(attachedWindowPID);
                OnAttached?.Invoke(attachedWindowPID, relativePosition);
                LogInfo($"Attached({attachedWindowPID}, [{relativePosition.X}, {relativePosition.Y}])");
            }
            else
            {
                LogError($"Fatal: page table corrupted");
            }
        }
        static void DetachAll()
        {
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
                    LogInfo($"Detached({remotePID})");
                }
            }
            // Clear attached windows list
            var newAttachedWindowPIDs = new int[AttachedWindowPIDsCapacity];
            SharedMemory.Self.WriteArray(
                Address.AttachedWindowPIDs,
                newAttachedWindowPIDs, 0, AttachedWindowPIDsCapacity);
            SharedMemory.Self.Flush();
        }

        [Obsolete("Please assign the delegates in the Logger class directly.")]
        public static void SetLogCallback(Action<string> callback)
        {
            LogError = callback;
            LogWarn = callback;
            LogInfo = callback;
            Logger.Ready();
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
                LogInfo($"Detached({remotePID})");
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
                LogInfo($"Attached({remotePID}, [{relativePosition.X}, {relativePosition.Y}])");
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

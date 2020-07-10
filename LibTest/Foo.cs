using System;

namespace LibTest
{
    public class Foo
    {
        static Rect SnapRect;
        static Action<int, Vec2> SnapCallback; //(pid, position)
        static Action<string> LogCallback;
        public static void SetSnapRect(Rect rect)
        {
            SnapRect = rect;
            rect.min.x += 0.1f;
            rect.min.y += 0.2f;
            SnapCallback?.Invoke(0, rect.min);
        }
        public static void SetSnapCallback(Action<int, Vec2> callback)
        {
            SnapCallback = callback;
        }
        public static void SetLogCallback(Action<string> callback)
        {
            LogCallback = callback;
        }
        public static void TickPerSecond()
        {
            LogCallback?.Invoke("Tick in dll"); // will output log in unity game instance
            // will be call every second for test
        }
    }
}

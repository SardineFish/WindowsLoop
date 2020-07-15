using System;
using System.Collections.Generic;

namespace WindowSnap
{
    public static class Logger
    {
        static List<string> delayedErrors = new List<string>();
        static List<string> delayedWarns = new List<string>();
        static List<string> delayedInfos = new List<string>();

        public static Action<string> LogError = message => delayedErrors.Add(message);
        public static Action<string> LogWarn = message => delayedWarns.Add(message);
        public static Action<string> LogInfo = message => delayedInfos.Add(message);

        public static void Ready()
        {
            delayedErrors.ForEach(message => LogError(message));
            delayedWarns.ForEach(message => LogWarn(message));
            delayedInfos.ForEach(message => LogInfo(message));
        }
    }
}

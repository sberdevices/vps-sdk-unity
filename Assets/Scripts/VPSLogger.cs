using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public enum LogLevel { ERROR, NONE, DEBUG, VERBOSE };

    /// <summary>
    /// Custom logger with verbose levels
    /// </summary>
    public static class VPSLogger
    {
#if VPS_DEBUG
        private static LogLevel currentLogLevel = LogLevel.DEBUG;
#elif VPS_VERBOSE
        private static LogLevel currentLogLevel = LogLevel.VERBOSE;
#else
        private static LogLevel currentLogLevel = LogLevel.NONE;
#endif

        public static void Log(LogLevel level, object message)
        {
            if (level == LogLevel.ERROR)
            {
                Debug.LogError(message);
            }
            else if (level <= currentLogLevel)
            {
                Debug.Log(message);
            }
        }

        public static void LogFormat(LogLevel level, string format, params object[] args)
        {
            if (level == LogLevel.ERROR)
            {
                Debug.LogErrorFormat(format, args);
            }
            else if (level <= currentLogLevel)
            {
                Debug.LogFormat(format, args);
            }
        }

        public static void SetLogLevel(LogLevel newLevel)
        {
            currentLogLevel = newLevel;
        }

        public static LogLevel GetLogLevel()
        {
            return currentLogLevel;
        }
    }
}

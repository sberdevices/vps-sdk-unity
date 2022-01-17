using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        private static string path = Path.Combine(Application.persistentDataPath, "Log.txt");

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

            AddToLogFile(message.ToString());
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

            AddToLogFile(string.Format(format, args));
        }

        public static void SetLogLevel(LogLevel newLevel)
        {
            currentLogLevel = newLevel;
        }

        public static LogLevel GetLogLevel()
        {
            return currentLogLevel;
        }

        private static void AddToLogFile(string logString)
        {
            string finalString = string.Format("[{0}] {1}\n", System.DateTime.Now, logString);
            File.AppendAllText(path, finalString);
#if UNITY_ANDROID && !UNITY_EDITOR
        RefreshAndroidFile(path);
#endif
        }
#if UNITY_ANDROID && !UNITY_EDITOR
    static void RefreshAndroidFile(string path) 
    {
        if (!File.Exists(path))
        {
            return;
        }

        using (AndroidJavaClass jcUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject joActivity = jcUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject joContext = joActivity.Call<AndroidJavaObject>("getApplicationContext"))
        using (AndroidJavaClass jcMediaScannerConnection = new AndroidJavaClass("android.media.MediaScannerConnection"))
            jcMediaScannerConnection.CallStatic("scanFile", joContext, new string[] { path }, null, null);

    }
#endif
    }
}
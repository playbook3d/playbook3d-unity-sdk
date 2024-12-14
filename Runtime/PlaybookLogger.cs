using System;
using UnityEngine;

namespace PlaybookUnitySDK.Runtime
{
    public enum DebugLevel
    {
        Errors,
        Default,
        All,
    }

    public static class PlaybookLogger
    {
        public static void Log(string text, DebugLevel level, Color textColor)
        {
            Log($"<color=#{ColorUtility.ToHtmlStringRGB(textColor)}>{text}</color>", level);
        }

        public static void Log(string text, DebugLevel level)
        {
            if (PlaybookSDK.debugLevel == DebugLevel.Errors)
                return;

            if (PlaybookSDK.debugLevel < level)
                return;

            Debug.Log(text);
        }

        public static void LogError(string text)
        {
            Debug.LogError(text);
        }
    }
}

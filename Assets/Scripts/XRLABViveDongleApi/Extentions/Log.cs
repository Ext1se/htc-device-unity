using UnityEngine;

namespace VIVE_Trackers
{
    internal class Log
    {
        public enum LogType
        {
            Log,
            Warning,
            Error,
            Blue,
            Green,
            Magenta
        }

        static string logStr = "";

        public static void Write(object msg, LogType type = LogType.Log)
        {
            if (msg != null && !string.IsNullOrEmpty(msg.ToString()))
            {
                switch (type)
                {
                    case LogType.Log:
                        logStr += msg.ToString();
                        break;
                    case LogType.Warning:
                        logStr += $"<color=yellow>{msg.ToString()}</color>";
                        break;
                    case LogType.Error:
                        logStr += $"<color=red>{msg.ToString()}</color>";
                        break;
                    case LogType.Blue:
                        logStr += $"<color=blue>{msg.ToString()}</color>";
                        break;
                    case LogType.Green:
                        logStr += $"<color=green>{msg.ToString()}</color>";
                        break;
                    case LogType.Magenta:
                        logStr += $"<color=magenta>{msg.ToString()}</color>";
                        break;
                    default:
                        break;
                }
            }
        }
        public static void WriteLine(object msg, LogType type = LogType.Log)
        {
            if (msg != null && !string.IsNullOrEmpty(msg.ToString()))
            {
                if(!string.IsNullOrEmpty(logStr))
                {
                    msg = logStr + msg.ToString();
                    logStr = "";
                }
                switch (type)
                {
                    case LogType.Log:
                        Debug.Log(msg.ToString());
                        break;
                    case LogType.Warning:
                        Debug.LogWarning(msg.ToString());
                        break;
                    case LogType.Error:
                        Debug.LogError(msg.ToString());
                        break;
                    case LogType.Blue:
                        Debug.Log($"<color=blue>{msg.ToString()}</color>");
                        break;
                    case LogType.Green:
                        Debug.Log($"<color=green>{msg.ToString()}</color>");
                        break;
                    case LogType.Magenta:
                        Debug.Log($"<color=magenta>{msg.ToString()}</color>");
                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(logStr))
                {
                    msg = logStr + msg.ToString();
                    logStr = "";
                    Debug.Log(msg.ToString());
                }
            }
        }

        public static void Warning(object msg)
        {
            Write(msg, LogType.Warning);
        }
        public static void WarningLine(object msg)
        {
            WriteLine(msg, LogType.Warning);
        }

        public static void Error(object msg)
        {
            Write(msg, LogType.Error);
        }
        public static void ErrorLine(object msg)
        {
            WriteLine(msg, LogType.Error);
        }

        public static void Blue(object msg)
        {
            Write(msg, LogType.Blue);
        }
        public static void BlueLine(object msg)
        {
            WriteLine(msg, LogType.Blue);
        }

        public static void Green(object msg)
        {
            Write(msg, LogType.Green);
        }
        public static void GreenLine(object msg)
        {
            WriteLine(msg, LogType.Green);
        }
    }
}

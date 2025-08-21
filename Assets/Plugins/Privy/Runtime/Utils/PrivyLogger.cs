using System.Collections.Generic;
using UnityEngine;

namespace Privy
{
    public static class PrivyLogger
    {
        private static PrivyLogLevel _level = PrivyLogLevel.NONE; // Default log level
        private static string _appId = "";

        private static readonly List<string> _internalAppIds = new List<string>
        {
            "clyyj8pea001mq5zqm11zkdl5",
            "clpijy3tw0001kz0g6ixs9z15",
            "cla06f34x0001mh08l8nsr496"
        };

        private static bool _printInternalLogs;

        public static void Configure(PrivyConfig config)
        {
            _level = config.LogLevel;
            _appId = config.AppId;
            _printInternalLogs = _internalAppIds.Contains(_appId);
        }

        public static void Debug(string message)
        {
            if (_level == PrivyLogLevel.DEBUG || _level == PrivyLogLevel.VERBOSE)
            {
                PrintMessage(message);
            }
        }

        public static void Info(string message)
        {
            if (_level == PrivyLogLevel.INFO || _level == PrivyLogLevel.VERBOSE)
            {
                PrintMessage(message);
            }
        }

        public static void Warning(string message)
        {
            if (_level == PrivyLogLevel.WARNING || _level == PrivyLogLevel.VERBOSE)
            {
                PrintMessage(message, LogType.Warning);
            }
        }

        public static void Error(string message, System.Exception error = null)
        {
            if (_level == PrivyLogLevel.ERROR || _level == PrivyLogLevel.VERBOSE)
            {
                PrintMessage(message, LogType.Error);
            }
        }

        public static void Internal(string message)
        {
            if (_printInternalLogs)
            {
                PrintMessage(message);
            }
        }

        private static void PrintMessage(string message, LogType logType = LogType.Log)
        {
            switch (logType)
            {
                case LogType.Error:
                    UnityEngine.Debug.LogError($"Privy: {message}");
                    break;
                case LogType.Warning:
                    UnityEngine.Debug.LogWarning($"Privy: {message}");
                    break;
                default:
                    UnityEngine.Debug.Log($"Privy: {message}");
                    break;
            }
        }
    }
}
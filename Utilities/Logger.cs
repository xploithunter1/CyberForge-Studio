using System;

namespace CyberForgeStudio.Utilities
{
    public static class Logger
    {
        public static event Action<string>? OnLog;

        public static void Info(string message)
        {
            OnLog?.Invoke($"[INFO {DateTime.Now:HH:mm:ss}] {message}");
        }

        public static void Error(string message)
        {
            OnLog?.Invoke($"[ERROR {DateTime.Now:HH:mm:ss}] {message}");
        }

        public static void Success(string message)
        {
            OnLog?.Invoke($"[SUCCESS {DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
using System;
using System.IO;

namespace EliteRetro.Core.Utilities
{
    /// <summary>
    /// Simple thread-safe file logger for runtime debugging.
    /// Writes to 'EliteRetro.log' in the application execution directory.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogPath = "EliteRetro.log";
        private static readonly object LockObj = new object();

        static Logger()
        {
            try
            {
                // Clear log file on startup
                File.WriteAllText(LogPath, $"--- EliteRetro Log Started: {DateTime.Now} ---\n");
            }
            catch { /* Ignore logging failures */ }
        }

        public static void Log(string message)
        {
            lock (LockObj)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    File.AppendAllText(LogPath, $"[{timestamp}] {message}\n");
                }
                catch { /* Ignore logging failures */ }
            }
        }

        public static void LogCollision(float dist, float radius, float dot, float speed)
        {
            Log($"[PLANET COLLISION] dist={dist:F0}, radius={radius:F0}, dot={dot:F4}, speed={speed:F1}");
        }

        public static void LogAltitude(float dist, int barValue, float speed)
        {
            Log($"[HUD ALT] dist={dist:F0}, AL_BAR={barValue}, speed={speed:F1}");
        }
    }
}

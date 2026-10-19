using System;
using System.IO;
using Microsoft.Win32;

namespace ArabicTextExtractor
{
    public static class StartupManager
    {
        private const string RUN_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "ArabicTextExtractor";

        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, false))
                {
                    if (key == null) return false;
                    object val = key.GetValue(APP_NAME);
                    return val != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
                {
                    if (key == null) return;
                    if (enable)
                    {
                        // Point to root run.vbs
                        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        string runVbs = Path.Combine(baseDir, "run.vbs");
                        if (!File.Exists(runVbs))
                        {
                            runVbs = Path.Combine(baseDir, "..", "run.vbs");
                        }
                        runVbs = Path.GetFullPath(runVbs);
                        key.SetValue(APP_NAME, "\"" + runVbs + "\"");
                    }
                    else
                    {
                        key.DeleteValue(APP_NAME, false);
                    }
                }
            }
            catch { }
        }
    }
}

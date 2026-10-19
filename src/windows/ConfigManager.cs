using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ArabicTextExtractor
{
    public class HotkeyConfig
    {
        public bool Alt;
        public bool Ctrl;
        public bool Shift;
        public bool Win;
        public Keys Key;

        public HotkeyConfig()
        {
            Alt = true;
            Shift = true;
            Key = Keys.A;
        }

        public HotkeyConfig(bool alt, bool ctrl, bool shift, bool win, Keys key)
        {
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
            Win = win;
            Key = key;
        }

        public int GetModifiers()
        {
            int mod = 0;
            if (Alt) mod |= 0x0001;
            if (Ctrl) mod |= 0x0002;
            if (Shift) mod |= 0x0004;
            if (Win) mod |= 0x0008;
            return mod;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Ctrl) sb.Append("Ctrl+");
            if (Alt) sb.Append("Alt+");
            if (Shift) sb.Append("Shift+");
            if (Win) sb.Append("Win+");
            sb.Append(Key.ToString());
            return sb.ToString();
        }

        public static HotkeyConfig Parse(string str, HotkeyConfig defaultVal)
        {
            if (string.IsNullOrEmpty(str)) return defaultVal;
            try
            {
                string[] parts = str.Split(new char[] { '+', '-' }, StringSplitOptions.RemoveEmptyEntries);
                bool alt = false, ctrl = false, shift = false, win = false;
                Keys key = Keys.None;

                for (int i = 0; i < parts.Length; i++)
                {
                    string p = parts[i].Trim();
                    if (string.Equals(p, "Alt", StringComparison.OrdinalIgnoreCase)) alt = true;
                    else if (string.Equals(p, "Ctrl", StringComparison.OrdinalIgnoreCase) || string.Equals(p, "Control", StringComparison.OrdinalIgnoreCase)) ctrl = true;
                    else if (string.Equals(p, "Shift", StringComparison.OrdinalIgnoreCase)) shift = true;
                    else if (string.Equals(p, "Win", StringComparison.OrdinalIgnoreCase) || string.Equals(p, "Windows", StringComparison.OrdinalIgnoreCase)) win = true;
                    else
                    {
                        try
                        {
                            key = (Keys)Enum.Parse(typeof(Keys), p, true);
                        }
                        catch { }
                    }
                }

                if (key != Keys.None)
                {
                    return new HotkeyConfig(alt, ctrl, shift, win, key);
                }
            }
            catch { }
            return defaultVal;
        }
    }

    public class AppConfig
    {
        public HotkeyConfig Snip = new HotkeyConfig(true, false, true, false, Keys.A); // Alt+Shift+A
        public HotkeyConfig History = new HotkeyConfig(true, false, true, false, Keys.H); // Alt+Shift+H
    }

    public static class ConfigManager
    {
        private static readonly object lockObj = new object();
        private static AppConfig currentConfig;

        public static string FilePath
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string inData = Path.Combine(baseDir, "data", "config.json");
                if (File.Exists(inData)) return inData;

                string parentData = Path.Combine(baseDir, "..", "data", "config.json");
                if (File.Exists(parentData)) return Path.GetFullPath(parentData);

                string dataFolder = Path.Combine(baseDir, "data");
                if (!Directory.Exists(dataFolder))
                {
                    try { Directory.CreateDirectory(dataFolder); } catch { }
                }
                return Path.Combine(dataFolder, "config.json");
            }
        }

        public static AppConfig GetConfig()
        {
            lock (lockObj)
            {
                if (currentConfig != null) return currentConfig;
                currentConfig = Load();
                return currentConfig;
            }
        }

        public static AppConfig Load()
        {
            AppConfig cfg = new AppConfig();
            if (!File.Exists(FilePath)) return cfg;

            try
            {
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                string snipStr = ExtractField(json, "snipShortcut");
                string histStr = ExtractField(json, "historyShortcut");

                if (!string.IsNullOrEmpty(snipStr))
                {
                    cfg.Snip = HotkeyConfig.Parse(snipStr, cfg.Snip);
                }
                if (!string.IsNullOrEmpty(histStr))
                {
                    cfg.History = HotkeyConfig.Parse(histStr, cfg.History);
                }
            }
            catch { }
            return cfg;
        }

        public static void Save(AppConfig cfg)
        {
            lock (lockObj)
            {
                currentConfig = cfg;
                try
                {
                    string dir = Path.GetDirectoryName(FilePath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    StringBuilder sb = new StringBuilder();
                    sb.Append("{\n");
                    sb.AppendFormat("  \"snipShortcut\": \"{0}\",\n", cfg.Snip.ToString());
                    sb.AppendFormat("  \"historyShortcut\": \"{0}\"\n", cfg.History.ToString());
                    sb.Append("}\n");

                    File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
                }
                catch { }
            }
        }

        private static string ExtractField(string json, string field)
        {
            string search = "\"" + field + "\"";
            int idx = json.IndexOf(search);
            if (idx == -1) return "";
            int colon = json.IndexOf(':', idx + search.Length);
            if (colon == -1) return "";
            int quoteStart = json.IndexOf('\"', colon + 1);
            if (quoteStart == -1) return "";
            int quoteEnd = json.IndexOf('\"', quoteStart + 1);
            if (quoteEnd == -1) return "";
            return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
        }
    }
}

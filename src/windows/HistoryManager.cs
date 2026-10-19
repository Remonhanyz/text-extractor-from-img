using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ArabicTextExtractor
{
    public class HistoryItem
    {
        public string Id;
        public string Timestamp;
        public string Text;

        public HistoryItem() { }
        public HistoryItem(string text)
        {
            Id = Guid.NewGuid().ToString("N");
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Text = text;
        }
    }

    public static class HistoryManager
    {
        private static readonly object lockObj = new object();

        private static string FilePath
        {
            get
            {
                // Look for data/history.json relative to base directory
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string inData = Path.Combine(baseDir, "data", "history.json");
                if (File.Exists(inData)) return inData;

                string parentData = Path.Combine(baseDir, "..", "data", "history.json");
                if (File.Exists(parentData)) return Path.GetFullPath(parentData);

                // Default location inside data/ folder
                string dataFolder = Path.Combine(baseDir, "data");
                if (!Directory.Exists(dataFolder))
                {
                    try { Directory.CreateDirectory(dataFolder); } catch { }
                }
                return Path.Combine(dataFolder, "history.json");
            }
        }

        public static List<HistoryItem> Load()
        {
            lock (lockObj)
            {
                List<HistoryItem> list = new List<HistoryItem>();
                if (!File.Exists(FilePath)) return list;
                try
                {
                    string json = File.ReadAllText(FilePath, Encoding.UTF8);
                    list = ParseJsonList(json);
                }
                catch { }
                return list;
            }
        }

        public static void Add(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            lock (lockObj)
            {
                List<HistoryItem> list = Load();
                list.Insert(0, new HistoryItem(text));
                if (list.Count > 200)
                {
                    list.RemoveRange(200, list.Count - 200);
                }
                Save(list);
            }
        }

        public static void Delete(string id)
        {
            lock (lockObj)
            {
                List<HistoryItem> list = Load();
                list.RemoveAll(x => x.Id == id);
                Save(list);
            }
        }

        public static void Clear()
        {
            lock (lockObj)
            {
                Save(new List<HistoryItem>());
            }
        }

        private static void Save(List<HistoryItem> list)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[\n");
            for (int i = 0; i < list.Count; i++)
            {
                HistoryItem item = list[i];
                sb.Append("  {\n");
                sb.AppendFormat("    \"Id\": \"{0}\",\n", EscapeJson(item.Id));
                sb.AppendFormat("    \"Timestamp\": \"{0}\",\n", EscapeJson(item.Timestamp));
                sb.AppendFormat("    \"Text\": \"{0}\"\n", EscapeJson(item.Text));
                sb.Append("  }");
                if (i < list.Count - 1) sb.Append(",");
                sb.Append("\n");
            }
            sb.Append("]");
            File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                if (c == '\\') sb.Append("\\\\");
                else if (c == '\"') sb.Append("\\\"");
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static List<HistoryItem> ParseJsonList(string json)
        {
            List<HistoryItem> list = new List<HistoryItem>();
            if (string.IsNullOrEmpty(json)) return list;

            int cur = 0;
            while (cur < json.Length)
            {
                int objStart = json.IndexOf('{', cur);
                if (objStart == -1) break;
                int objEnd = json.IndexOf('}', objStart);
                if (objEnd == -1) break;

                string objStr = json.Substring(objStart, objEnd - objStart + 1);
                string id = ExtractField(objStr, "Id");
                string time = ExtractField(objStr, "Timestamp");
                string text = ExtractField(objStr, "Text");

                if (!string.IsNullOrEmpty(text))
                {
                    HistoryItem it = new HistoryItem();
                    it.Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
                    it.Timestamp = string.IsNullOrEmpty(time) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : time;
                    it.Text = text;
                    list.Add(it);
                }
                cur = objEnd + 1;
            }
            return list;
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

            StringBuilder sb = new StringBuilder();
            bool esc = false;
            for (int i = quoteStart + 1; i < json.Length; i++)
            {
                char c = json[i];
                if (esc)
                {
                    if (c == 'n') sb.Append('\n');
                    else if (c == 'r') sb.Append('\r');
                    else if (c == 't') sb.Append('\t');
                    else if (c == '\"') sb.Append('\"');
                    else if (c == '\\') sb.Append('\\');
                    else sb.Append(c);
                    esc = false;
                }
                else if (c == '\\')
                {
                    esc = true;
                }
                else if (c == '\"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}

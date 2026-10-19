using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ArabicTextExtractor
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SnipperContext());
        }
    }

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
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.json");
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
                        string appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run.vbs");
                        key.SetValue(APP_NAME, "\"" + appPath + "\"");
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

    public class SnipperContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem startupMenuItem;
        private HotkeyWindow hotkeyWindow;
        private Process nodeProcess;
        private HistoryForm openHistoryForm;
        private const int OCR_PORT = 48821;

        public SnipperContext()
        {
            EnsureNodeServiceRunning();

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Capture Text (Alt+Shift+A)", null, (s, e) => StartSnipping());
            trayMenu.Items.Add("Text History (Alt+Shift+H)", null, (s, e) => OpenHistory());
            trayMenu.Items.Add("-");

            startupMenuItem = new ToolStripMenuItem("Start with Windows");
            startupMenuItem.CheckOnClick = true;
            startupMenuItem.Checked = StartupManager.IsStartupEnabled();
            startupMenuItem.Click += (s, e) =>
            {
                StartupManager.SetStartup(startupMenuItem.Checked);
                string msg = startupMenuItem.Checked ? "Extractor will now start with Windows." : "Extractor removed from Windows startup.";
                trayIcon.ShowBalloonTip(2000, "Startup Setting Updated", msg, ToolTipIcon.Info);
            };
            trayMenu.Opening += (s, e) =>
            {
                startupMenuItem.Checked = StartupManager.IsStartupEnabled();
            };
            trayMenu.Items.Add(startupMenuItem);

            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Exit", null, (s, e) => Exit());

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Arabic & English Text Extractor";
            
            // Set custom icon if available
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(icoPath))
            {
                try { trayIcon.Icon = new Icon(icoPath); }
                catch { trayIcon.Icon = SystemIcons.Information; }
            }
            else
            {
                trayIcon.Icon = SystemIcons.Information;
            }

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => StartSnipping();

            hotkeyWindow = new HotkeyWindow(this);

            trayIcon.ShowBalloonTip(3000, "Screen Text Extractor Ready",
                "Alt+Shift+A: Snip screen to copy text\nAlt+Shift+H: Open text history",
                ToolTipIcon.Info);
        }

        public void StartSnipping()
        {
            try
            {
                Rectangle bounds = SystemInformation.VirtualScreen;
                Bitmap screenBmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(screenBmp))
                {
                    g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                }

                OverlayForm overlay = new OverlayForm(screenBmp, bounds, this);
                overlay.Show();
                overlay.Activate();
            }
            catch (Exception ex)
            {
                trayIcon.ShowBalloonTip(3000, "Error starting capture", ex.Message, ToolTipIcon.Error);
            }
        }

        public void OpenHistory()
        {
            if (openHistoryForm != null && !openHistoryForm.IsDisposed)
            {
                openHistoryForm.BringToFront();
                openHistoryForm.Activate();
                return;
            }

            openHistoryForm = new HistoryForm(this);
            openHistoryForm.Show();
            openHistoryForm.Activate();
        }

        public void ProcessImage(Bitmap cropped)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    EnsureNodeServiceRunning();

                    using (MemoryStream ms = new MemoryStream())
                    {
                        cropped.Save(ms, ImageFormat.Png);
                        byte[] imgBytes = ms.ToArray();
                        cropped.Dispose();

                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + OCR_PORT + "/ocr");
                        request.Method = "POST";
                        request.ContentType = "image/png";
                        request.ContentLength = imgBytes.Length;
                        request.Timeout = 15000;

                        using (Stream reqStream = request.GetRequestStream())
                        {
                            reqStream.Write(imgBytes, 0, imgBytes.Length);
                        }

                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                        {
                            string json = reader.ReadToEnd();
                            string text = ExtractJsonStringField(json, "text");
                            if (!string.IsNullOrEmpty(text))
                            {
                                HistoryManager.Add(text);

                                if (openHistoryForm != null && !openHistoryForm.IsDisposed)
                                {
                                    openHistoryForm.Invoke((MethodInvoker)(() => openHistoryForm.ReloadHistory()));
                                }

                                Thread staThread = new Thread(() =>
                                {
                                    try
                                    {
                                        Clipboard.SetDataObject(text, true, 5, 100);
                                    }
                                    catch { }
                                });
                                staThread.SetApartmentState(ApartmentState.STA);
                                staThread.Start();
                                staThread.Join();

                                try { SystemSounds.Asterisk.Play(); } catch { }

                                string preview = text.Length > 80 ? text.Substring(0, 80) + "..." : text;
                                trayIcon.ShowBalloonTip(2500, "Text Extracted & Copied (" + text.Length + " chars)",
                                    preview, ToolTipIcon.Info);
                            }
                            else
                            {
                                trayIcon.ShowBalloonTip(2000, "No text recognized", "Could not detect Arabic or English text in the selected area.", ToolTipIcon.Warning);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    trayIcon.ShowBalloonTip(3000, "OCR Service Error", ex.Message, ToolTipIcon.Error);
                }
            });
        }

        private string ExtractJsonStringField(string json, string field)
        {
            string search = "\"" + field + "\":\"";
            int idx = json.IndexOf(search);
            if (idx == -1) return "";
            idx += search.Length;
            StringBuilder sb = new StringBuilder();
            bool esc = false;
            for (int i = idx; i < json.Length; i++)
            {
                char c = json[i];
                if (esc)
                {
                    if (c == 'n') sb.Append('\n');
                    else if (c == 'r') sb.Append('\r');
                    else if (c == 't') sb.Append('\t');
                    else if (c == '"') sb.Append('"');
                    else if (c == '\\') sb.Append('\\');
                    else if (c == 'u' && i + 4 < json.Length)
                    {
                        string hex = json.Substring(i + 1, 4);
                        int code = Convert.ToInt32(hex, 16);
                        sb.Append((char)code);
                        i += 4;
                    }
                    else sb.Append(c);
                    esc = false;
                }
                else if (c == '\\')
                {
                    esc = true;
                }
                else if (c == '"')
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

        private void EnsureNodeServiceRunning()
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + OCR_PORT + "/health");
                req.Timeout = 1000;
                using (HttpWebResponse res = (HttpWebResponse)req.GetResponse())
                {
                    if (res.StatusCode == HttpStatusCode.OK) return;
                }
            }
            catch
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                string script = Path.Combine(dir, "ocr-service.js");
                if (File.Exists(script))
                {
                    string nodeExe = FindNodeExe();
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = nodeExe;
                    psi.Arguments = "\"" + script + "\"";
                    psi.WorkingDirectory = dir;
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    try
                    {
                        nodeProcess = Process.Start(psi);
                    }
                    catch { }
                }
            }
        }

        private string FindNodeExe()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string userNode = Path.Combine(localAppData, "nodejs", "node.exe");
            if (File.Exists(userNode)) return userNode;

            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string sysNode = Path.Combine(progFiles, "nodejs", "node.exe");
            if (File.Exists(sysNode)) return sysNode;

            return "node";
        }

        private void Exit()
        {
            trayIcon.Visible = false;
            if (nodeProcess != null && !nodeProcess.HasExited)
            {
                try { nodeProcess.Kill(); } catch { }
            }
            Application.Exit();
        }
    }

    public class HistoryForm : Form
    {
        private SnipperContext context;
        private List<HistoryItem> allItems = new List<HistoryItem>();
        private ListBox listBox;
        private TextBox txtSearch;
        private TextBox txtDetail;
        private Label lblStatus;
        private Button btnCopy;
        private Button btnDelete;
        private Button btnClear;

        public HistoryForm(SnipperContext ctx)
        {
            context = ctx;
            InitializeUI();
            ReloadHistory();
        }

        private void InitializeUI()
        {
            this.Text = "Text Extraction History";
            this.Size = new Size(720, 500);
            this.MinimumSize = new Size(550, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            
            // Set custom icon
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(icoPath))
            {
                try { this.Icon = new Icon(icoPath); }
                catch { this.Icon = SystemIcons.Information; }
            }
            else
            {
                this.Icon = SystemIcons.Information;
            }

            this.KeyPreview = true;

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
            };

            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 44;
            topPanel.Padding = new Padding(12, 8, 12, 8);

            Label lblSearch = new Label();
            lblSearch.Text = "Search:";
            lblSearch.AutoSize = true;
            lblSearch.Location = new Point(12, 12);
            topPanel.Controls.Add(lblSearch);

            txtSearch = new TextBox();
            txtSearch.Location = new Point(68, 9);
            txtSearch.Width = 350;
            txtSearch.TextChanged += (s, e) => FilterList();
            topPanel.Controls.Add(txtSearch);

            Panel bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 48;
            bottomPanel.Padding = new Padding(12, 8, 12, 8);

            lblStatus = new Label();
            lblStatus.Text = "Tip: Double-click any item to re-copy to clipboard.";
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.DarkSlateGray;
            lblStatus.Location = new Point(12, 14);
            bottomPanel.Controls.Add(lblStatus);

            btnCopy = new Button();
            btnCopy.Text = "Copy Selected";
            btnCopy.Width = 120;
            btnCopy.Height = 30;
            btnCopy.Location = new Point(360, 9);
            btnCopy.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCopy.Click += (s, e) => CopySelectedItem();
            bottomPanel.Controls.Add(btnCopy);

            btnDelete = new Button();
            btnDelete.Text = "Delete";
            btnDelete.Width = 80;
            btnDelete.Height = 30;
            btnDelete.Location = new Point(488, 9);
            btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDelete.Click += (s, e) => DeleteSelectedItem();
            bottomPanel.Controls.Add(btnDelete);

            btnClear = new Button();
            btnClear.Text = "Clear All";
            btnClear.Width = 80;
            btnClear.Height = 30;
            btnClear.Location = new Point(576, 9);
            btnClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClear.Click += (s, e) => ClearAllHistory();
            bottomPanel.Controls.Add(btnClear);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Vertical;
            split.SplitterDistance = 280;

            listBox = new ListBox();
            listBox.Dock = DockStyle.Fill;
            listBox.Font = new Font("Segoe UI", 9.5f);
            listBox.IntegralHeight = false;
            listBox.SelectedIndexChanged += (s, e) => ShowSelectedDetail();
            listBox.DoubleClick += (s, e) => CopySelectedItem();
            split.Panel1.Controls.Add(listBox);

            txtDetail = new TextBox();
            txtDetail.Dock = DockStyle.Fill;
            txtDetail.Multiline = true;
            txtDetail.ScrollBars = ScrollBars.Vertical;
            txtDetail.Font = new Font("Segoe UI", 10.5f);
            txtDetail.ReadOnly = false;
            split.Panel2.Controls.Add(txtDetail);

            this.Controls.Add(split);
            this.Controls.Add(topPanel);
            this.Controls.Add(bottomPanel);
        }

        public void ReloadHistory()
        {
            allItems = HistoryManager.Load();
            FilterList();
        }

        private void FilterList()
        {
            string query = txtSearch.Text.Trim().ToLowerInvariant();
            listBox.BeginUpdate();
            listBox.Items.Clear();

            List<HistoryItem> filtered = new List<HistoryItem>();
            foreach (HistoryItem item in allItems)
            {
                if (string.IsNullOrEmpty(query) || (item.Text != null && item.Text.ToLowerInvariant().Contains(query)))
                {
                    filtered.Add(item);
                }
            }

            foreach (HistoryItem it in filtered)
            {
                string firstLine = (it.Text ?? "").Replace("\r\n", " ").Replace("\n", " ").Trim();
                if (firstLine.Length > 36) firstLine = firstLine.Substring(0, 36) + "...";
                string display = string.Format("[{0}] {1}", it.Timestamp.Length >= 16 ? it.Timestamp.Substring(5, 11) : it.Timestamp, firstLine);
                listBox.Items.Add(new ListBoxItem(it, display));
            }

            listBox.EndUpdate();
            if (listBox.Items.Count > 0)
            {
                listBox.SelectedIndex = 0;
            }
            else
            {
                txtDetail.Clear();
                lblStatus.Text = "No history items.";
            }
        }

        private void ShowSelectedDetail()
        {
            ListBoxItem selected = listBox.SelectedItem as ListBoxItem;
            if (selected != null && selected.Item != null)
            {
                txtDetail.Text = selected.Item.Text;
                lblStatus.Text = string.Format("Characters: {0} | Captured: {1}", selected.Item.Text.Length, selected.Item.Timestamp);
            }
            else
            {
                txtDetail.Clear();
            }
        }

        private void CopySelectedItem()
        {
            ListBoxItem selected = listBox.SelectedItem as ListBoxItem;
            if (selected != null && selected.Item != null && !string.IsNullOrEmpty(selected.Item.Text))
            {
                try
                {
                    Clipboard.SetDataObject(selected.Item.Text, true, 5, 100);
                    SystemSounds.Asterisk.Play();
                    lblStatus.Text = "Copied to clipboard!";
                }
                catch { }
            }
        }

        private void DeleteSelectedItem()
        {
            ListBoxItem selected = listBox.SelectedItem as ListBoxItem;
            if (selected != null && selected.Item != null)
            {
                HistoryManager.Delete(selected.Item.Id);
                ReloadHistory();
                lblStatus.Text = "Item deleted.";
            }
        }

        private void ClearAllHistory()
        {
            if (MessageBox.Show(this, "Are you sure you want to clear all extraction history?", "Clear History", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                HistoryManager.Clear();
                ReloadHistory();
                lblStatus.Text = "History cleared.";
            }
        }

        private class ListBoxItem
        {
            public HistoryItem Item;
            public string Display;

            public ListBoxItem(HistoryItem it, string disp)
            {
                Item = it;
                Display = disp;
            }

            public override string ToString()
            {
                return Display;
            }
        }
    }

    public class OverlayForm : Form
    {
        private Bitmap backgroundScreenshot;
        private Rectangle virtualBounds;
        private SnipperContext context;
        private Point startPoint;
        private Rectangle selectionRect;
        private bool isSelecting = false;

        public OverlayForm(Bitmap screenshot, Rectangle bounds, SnipperContext ctx)
        {
            backgroundScreenshot = screenshot;
            virtualBounds = bounds;
            context = ctx;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = bounds;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Cross;
            this.KeyPreview = true;

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Close();
                }
            };
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                startPoint = e.Location;
                selectionRect = new Rectangle(e.Location, new Size(0, 0));
                Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                Close();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isSelecting)
            {
                int x = Math.Min(startPoint.X, e.X);
                int y = Math.Min(startPoint.Y, e.Y);
                int width = Math.Abs(startPoint.X - e.X);
                int height = Math.Abs(startPoint.Y - e.Y);
                selectionRect = new Rectangle(x, y, width, height);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (isSelecting && e.Button == MouseButtons.Left)
            {
                isSelecting = false;
                if (selectionRect.Width > 5 && selectionRect.Height > 5)
                {
                    Bitmap cropped = new Bitmap(selectionRect.Width, selectionRect.Height, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(cropped))
                    {
                        g.DrawImage(backgroundScreenshot,
                            new Rectangle(0, 0, selectionRect.Width, selectionRect.Height),
                            selectionRect,
                            GraphicsUnit.Pixel);
                    }
                    this.Hide();
                    context.ProcessImage(cropped);
                }
                Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.DrawImage(backgroundScreenshot, 0, 0);

            using (SolidBrush darkBrush = new SolidBrush(Color.FromArgb(130, 0, 0, 0)))
            {
                g.FillRectangle(darkBrush, 0, 0, Width, Height);
            }

            if (isSelecting && selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                g.DrawImage(backgroundScreenshot, selectionRect, selectionRect, GraphicsUnit.Pixel);

                using (Pen borderPen = new Pen(Color.FromArgb(0, 153, 255), 2))
                {
                    borderPen.Alignment = PenAlignment.Outset;
                    g.DrawRectangle(borderPen, selectionRect);
                }

                string dimText = string.Format("{0} x {1}", selectionRect.Width, selectionRect.Height);
                using (Font font = new Font("Segoe UI", 9, FontStyle.Bold))
                using (SolidBrush badgeBrush = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    SizeF textSize = g.MeasureString(dimText, font);
                    int badgeX = selectionRect.Left;
                    int badgeY = selectionRect.Bottom + 6;
                    if (badgeY + textSize.Height > Height)
                    {
                        badgeY = selectionRect.Top - (int)textSize.Height - 8;
                    }
                    Rectangle badgeRect = new Rectangle(badgeX, badgeY, (int)textSize.Width + 10, (int)textSize.Height + 4);
                    g.FillRectangle(badgeBrush, badgeRect);
                    g.DrawString(dimText, font, textBrush, badgeX + 5, badgeY + 2);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && backgroundScreenshot != null)
            {
                backgroundScreenshot.Dispose();
                backgroundScreenshot = null;
            }
            base.Dispose(disposing);
        }
    }

    public class HotkeyWindow : NativeWindow, IDisposable
    {
        private SnipperContext context;
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_SNIP_ID = 9001;
        private const int HOTKEY_HIST_ID = 9002;

        private const int MOD_ALT = 0x0001;
        private const int MOD_SHIFT = 0x0004;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public HotkeyWindow(SnipperContext ctx)
        {
            context = ctx;
            CreateHandle(new CreateParams());
            RegisterHotKey(this.Handle, HOTKEY_SNIP_ID, MOD_ALT | MOD_SHIFT, (int)Keys.A);
            RegisterHotKey(this.Handle, HOTKEY_HIST_ID, MOD_ALT | MOD_SHIFT, (int)Keys.H);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_SNIP_ID)
                {
                    context.StartSnipping();
                }
                else if (id == HOTKEY_HIST_ID)
                {
                    context.OpenHistory();
                }
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            UnregisterHotKey(this.Handle, HOTKEY_SNIP_ID);
            UnregisterHotKey(this.Handle, HOTKEY_HIST_ID);
            DestroyHandle();
        }
    }
}

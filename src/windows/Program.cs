using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Media;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

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

            // Load app icon
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string icoPath = Path.Combine(baseDir, "assets", "app.ico");
            if (!File.Exists(icoPath))
            {
                icoPath = Path.Combine(baseDir, "..", "assets", "app.ico");
            }
            if (!File.Exists(icoPath))
            {
                icoPath = Path.Combine(baseDir, "app.ico");
            }

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
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string script = Path.Combine(baseDir, "src", "server", "server.js");
                string workDir = baseDir;

                if (!File.Exists(script))
                {
                    script = Path.Combine(baseDir, "..", "src", "server", "server.js");
                    workDir = Path.GetFullPath(Path.Combine(baseDir, ".."));
                }
                if (!File.Exists(script))
                {
                    script = Path.Combine(baseDir, "ocr-service.js");
                }

                if (File.Exists(script))
                {
                    string nodeExe = FindNodeExe();
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = nodeExe;
                    psi.Arguments = "\"" + script + "\"";
                    psi.WorkingDirectory = workDir;
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
}

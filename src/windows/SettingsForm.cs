using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace ArabicTextExtractor
{
    public class SettingsForm : Form
    {
        private SnipperContext context;
        private HotkeyConfig snipConfig;
        private HotkeyConfig histConfig;
        private string ocrEngine;
        private string ocrLanguage;
        private string geminiKey;

        private ComboBox cmbEngine;
        private ComboBox cmbLanguage;
        private TextBox txtGeminiKey;
        private Button btnTestKey;
        private Label lblKeyHelp;
        private LinkLabel lnkGetKey;
        private GroupBox grpCloud;

        private TextBox txtSnip;
        private TextBox txtHistory;
        private Button btnSave;
        private Button btnReset;
        private Button btnCancel;

        public SettingsForm(SnipperContext ctx)
        {
            this.context = ctx;
            this.Text = "Settings - Arabic Screen OCR";
            this.Size = new Size(560, 560);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            AppConfig cfg = ConfigManager.Load();
            this.snipConfig = cfg.Snip;
            this.histConfig = cfg.History;
            this.ocrEngine = cfg.OcrEngine ?? "paddle";
            this.ocrLanguage = cfg.OcrLanguage ?? "auto";
            this.geminiKey = cfg.GeminiApiKey ?? "";

            InitUI();
        }

        private void InitUI()
        {
            // Title & Subtitle
            Label lblTitle = new Label();
            lblTitle.Text = "Engine & Shortcut Settings";
            lblTitle.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblTitle.Location = new Point(24, 16);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Group 1: OCR Engine & Language Selection
            GroupBox grpEngine = new GroupBox();
            grpEngine.Text = "OCR Engine & Language";
            grpEngine.Location = new Point(24, 52);
            grpEngine.Size = new Size(496, 100);

            Label lblEng = new Label();
            lblEng.Text = "Engine:";
            lblEng.Location = new Point(20, 28);
            lblEng.AutoSize = true;
            grpEngine.Controls.Add(lblEng);

            cmbEngine = new ComboBox();
            cmbEngine.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEngine.Location = new Point(140, 25);
            cmbEngine.Width = 330;
            cmbEngine.Items.Add("PaddleOCR (Offline - High Accuracy Recommended)");
            cmbEngine.Items.Add("Gemini 1.5 Flash (Online - 100% Accuracy Free Key)");
            cmbEngine.Items.Add("Google Cloud Vision (Online)");
            cmbEngine.Items.Add("Legacy Tesseract (Offline Fallback)");

            if (ocrEngine == "gemini") cmbEngine.SelectedIndex = 1;
            else if (ocrEngine == "vision") cmbEngine.SelectedIndex = 2;
            else if (ocrEngine == "tesseract") cmbEngine.SelectedIndex = 3;
            else cmbEngine.SelectedIndex = 0;

            cmbEngine.SelectedIndexChanged += (s, e) => UpdateEngineUIState();
            grpEngine.Controls.Add(cmbEngine);

            Label lblLang = new Label();
            lblLang.Text = "Language Mode:";
            lblLang.Location = new Point(20, 64);
            lblLang.AutoSize = true;
            grpEngine.Controls.Add(lblLang);

            cmbLanguage = new ComboBox();
            cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLanguage.Location = new Point(140, 61);
            cmbLanguage.Width = 330;
            cmbLanguage.Items.Add("Arabic & English (Auto)");
            cmbLanguage.Items.Add("Arabic Only");
            cmbLanguage.Items.Add("English Only");

            if (ocrLanguage == "ara") cmbLanguage.SelectedIndex = 1;
            else if (ocrLanguage == "eng") cmbLanguage.SelectedIndex = 2;
            else cmbLanguage.SelectedIndex = 0;

            grpEngine.Controls.Add(cmbLanguage);
            this.Controls.Add(grpEngine);

            // Group 2: Cloud API Key
            grpCloud = new GroupBox();
            grpCloud.Text = "Cloud API Key (Optional for Gemini 1.5 Flash / Cloud Vision)";
            grpCloud.Location = new Point(24, 160);
            grpCloud.Size = new Size(496, 105);

            Label lblKey = new Label();
            lblKey.Text = "API Key:";
            lblKey.Location = new Point(20, 28);
            lblKey.AutoSize = true;
            grpCloud.Controls.Add(lblKey);

            txtGeminiKey = new TextBox();
            txtGeminiKey.Location = new Point(140, 25);
            txtGeminiKey.Width = 240;
            txtGeminiKey.Text = geminiKey;
            txtGeminiKey.UseSystemPasswordChar = true;
            grpCloud.Controls.Add(txtGeminiKey);

            btnTestKey = new Button();
            btnTestKey.Text = "Test Key";
            btnTestKey.Location = new Point(388, 23);
            btnTestKey.Size = new Size(82, 26);
            btnTestKey.Click += (s, e) => TestApiKeyAsync();
            grpCloud.Controls.Add(btnTestKey);

            lblKeyHelp = new Label();
            lblKeyHelp.Text = "Free tier: 15 requests/min.";
            lblKeyHelp.Location = new Point(140, 60);
            lblKeyHelp.AutoSize = true;
            lblKeyHelp.ForeColor = Color.DimGray;
            grpCloud.Controls.Add(lblKeyHelp);

            lnkGetKey = new LinkLabel();
            lnkGetKey.Text = "Get Free API Key";
            lnkGetKey.Location = new Point(365, 60);
            lnkGetKey.AutoSize = true;
            lnkGetKey.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://aistudio.google.com/app/apikey") { UseShellExecute = true }); }
                catch { }
            };
            grpCloud.Controls.Add(lnkGetKey);

            this.Controls.Add(grpCloud);

            // Group 3: Screen Snipper Shortcut
            GroupBox grpSnip = new GroupBox();
            grpSnip.Text = "Screen Snipper Shortcut";
            grpSnip.Location = new Point(24, 273);
            grpSnip.Size = new Size(496, 75);

            Label lblSnipHelp = new Label();
            lblSnipHelp.Text = "Active Shortcut:";
            lblSnipHelp.Location = new Point(20, 28);
            lblSnipHelp.AutoSize = true;
            grpSnip.Controls.Add(lblSnipHelp);

            txtSnip = new TextBox();
            txtSnip.Location = new Point(140, 25);
            txtSnip.Width = 330;
            txtSnip.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            txtSnip.TextAlign = HorizontalAlignment.Center;
            txtSnip.Text = snipConfig.ToString();
            txtSnip.ReadOnly = true;
            txtSnip.BackColor = Color.White;
            txtSnip.KeyDown += (s, e) => HandleKeyRecord(e, ref snipConfig, txtSnip);
            grpSnip.Controls.Add(txtSnip);

            this.Controls.Add(grpSnip);

            // Group 4: History Shortcut
            GroupBox grpHist = new GroupBox();
            grpHist.Text = "Text History Window Shortcut";
            grpHist.Location = new Point(24, 356);
            grpHist.Size = new Size(496, 75);

            Label lblHistHelp = new Label();
            lblHistHelp.Text = "Active Shortcut:";
            lblHistHelp.Location = new Point(20, 28);
            lblHistHelp.AutoSize = true;
            grpHist.Controls.Add(lblHistHelp);

            txtHistory = new TextBox();
            txtHistory.Location = new Point(140, 25);
            txtHistory.Width = 330;
            txtHistory.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            txtHistory.TextAlign = HorizontalAlignment.Center;
            txtHistory.Text = histConfig.ToString();
            txtHistory.ReadOnly = true;
            txtHistory.BackColor = Color.White;
            txtHistory.KeyDown += (s, e) => HandleKeyRecord(e, ref histConfig, txtHistory);
            grpHist.Controls.Add(txtHistory);

            this.Controls.Add(grpHist);

            // Bottom Buttons
            btnSave = new Button();
            btnSave.Text = "Save & Apply";
            btnSave.Size = new Size(130, 36);
            btnSave.Location = new Point(110, 450);
            btnSave.Click += (s, e) => SaveAndApply();
            this.Controls.Add(btnSave);

            btnReset = new Button();
            btnReset.Text = "Reset Defaults";
            btnReset.Size = new Size(130, 36);
            btnReset.Location = new Point(255, 450);
            btnReset.Click += (s, e) => ResetDefaults();
            this.Controls.Add(btnReset);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(100, 36);
            btnCancel.Location = new Point(400, 450);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            UpdateEngineUIState();
        }

        private void TestApiKeyAsync()
        {
            string key = txtGeminiKey.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show(this, "Please enter an API key to test.", "Missing Key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnTestKey.Enabled = false;
            btnTestKey.Text = "Testing...";

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                string statusMsg = "";
                bool isValid = false;

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:48821/api/test-key");
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.Timeout = 12000;

                    string json = "{\"apiKey\":\"" + key.Replace("\"", "\\\"") + "\"}";
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    request.ContentLength = bytes.Length;

                    using (Stream stream = request.GetRequestStream())
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string respText = reader.ReadToEnd();
                        if (respText.Contains("\"valid\":true"))
                        {
                            isValid = true;
                            statusMsg = "API Key is valid and active!";
                        }
                        else
                        {
                            statusMsg = "Key verification failed. Please check your key.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    statusMsg = "Verification error: " + ex.Message;
                }

                this.BeginInvoke((MethodInvoker)delegate
                {
                    btnTestKey.Enabled = true;
                    btnTestKey.Text = "Test Key";
                    if (isValid)
                    {
                        lblKeyHelp.Text = "Key Valid & Ready!";
                        lblKeyHelp.ForeColor = Color.ForestGreen;
                        MessageBox.Show(this, "Your Google AI Studio API Key is valid and ready to use!", "Key Verified", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        lblKeyHelp.Text = "Invalid API Key";
                        lblKeyHelp.ForeColor = Color.Crimson;
                        MessageBox.Show(this, statusMsg, "Key Verification Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            });
        }

        private void UpdateEngineUIState()
        {
            bool isCloud = (cmbEngine.SelectedIndex == 1 || cmbEngine.SelectedIndex == 2);
            grpCloud.Enabled = isCloud;
            txtGeminiKey.BackColor = isCloud ? Color.White : Color.FromArgb(245, 245, 245);
            btnTestKey.Enabled = isCloud;

            if (cmbEngine.SelectedIndex == 1)
            {
                grpCloud.Text = "Gemini 1.5 Flash API Key";
                lnkGetKey.Visible = true;
                lblKeyHelp.Text = "Free tier: 15 requests/min.";
                lblKeyHelp.ForeColor = Color.DimGray;
            }
            else if (cmbEngine.SelectedIndex == 2)
            {
                grpCloud.Text = "Google Cloud Vision API Key";
                lnkGetKey.Visible = false;
                lblKeyHelp.Text = "Google Cloud Vision Key";
                lblKeyHelp.ForeColor = Color.DimGray;
            }
            else
            {
                grpCloud.Text = "Cloud API Key (Not required for offline engine)";
                lnkGetKey.Visible = false;
                lblKeyHelp.Text = "PaddleOCR is ready (100% Offline, No API key needed).";
                lblKeyHelp.ForeColor = Color.ForestGreen;
            }
        }

        private void HandleKeyRecord(KeyEventArgs e, ref HotkeyConfig target, TextBox box)
        {
            e.SuppressKeyPress = true;

            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey ||
                e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
            {
                return;
            }

            bool alt = e.Alt;
            bool ctrl = e.Control;
            bool shift = e.Shift;
            Keys key = e.KeyCode;

            if (!alt && !ctrl && !shift)
            {
                MessageBox.Show(this, "Please include at least one modifier (Alt, Ctrl, or Shift) with your shortcut key.", "Invalid Shortcut", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            target = new HotkeyConfig(alt, ctrl, shift, false, key);
            box.Text = target.ToString();
        }

        private void SaveAndApply()
        {
            AppConfig cfg = new AppConfig();
            cfg.Snip = snipConfig;
            cfg.History = histConfig;

            if (cmbEngine.SelectedIndex == 1) cfg.OcrEngine = "gemini";
            else if (cmbEngine.SelectedIndex == 2) cfg.OcrEngine = "vision";
            else if (cmbEngine.SelectedIndex == 3) cfg.OcrEngine = "tesseract";
            else cfg.OcrEngine = "paddle";

            if (cmbLanguage.SelectedIndex == 1) cfg.OcrLanguage = "ara";
            else if (cmbLanguage.SelectedIndex == 2) cfg.OcrLanguage = "eng";
            else cfg.OcrLanguage = "auto";

            cfg.GeminiApiKey = txtGeminiKey.Text.Trim();

            ConfigManager.Save(cfg);
            context.ApplyUpdatedShortcuts();
            this.Close();
        }

        private void ResetDefaults()
        {
            snipConfig = new HotkeyConfig(true, false, true, false, Keys.A);
            histConfig = new HotkeyConfig(true, false, true, false, Keys.H);
            cmbEngine.SelectedIndex = 0; // PaddleOCR
            cmbLanguage.SelectedIndex = 0; // Auto
            txtSnip.Text = snipConfig.ToString();
            txtHistory.Text = histConfig.ToString();
            UpdateEngineUIState();
        }
    }
}

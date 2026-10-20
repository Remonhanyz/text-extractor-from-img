using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ArabicTextExtractor
{
    public class SettingsForm : Form
    {
        private SnipperContext context;
        private TextBox txtSnip;
        private TextBox txtHistory;
        private ComboBox cmbEngine;
        private ComboBox cmbLanguage;
        private TextBox txtGeminiKey;
        private Label lblKeyHelp;
        private LinkLabel lnkGetKey;
        private GroupBox grpCloud;

        private HotkeyConfig snipConfig;
        private HotkeyConfig histConfig;
        private string ocrEngine;
        private string ocrLanguage;
        private string geminiKey;

        private Button btnSave;
        private Button btnReset;
        private Button btnCancel;

        public SettingsForm(SnipperContext ctx)
        {
            context = ctx;
            AppConfig current = ConfigManager.GetConfig();
            snipConfig = new HotkeyConfig(current.Snip.Alt, current.Snip.Ctrl, current.Snip.Shift, current.Snip.Win, current.Snip.Key);
            histConfig = new HotkeyConfig(current.History.Alt, current.History.Ctrl, current.History.Shift, current.History.Win, current.History.Key);
            ocrEngine = (current.OcrEngine ?? "paddle").ToLowerInvariant();
            ocrLanguage = current.OcrLanguage ?? "auto";
            geminiKey = current.GeminiApiKey ?? "";

            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Text Extractor - Settings & Engine";
            this.Size = new Size(560, 560);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Icon
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string icoPath = Path.Combine(baseDir, "assets", "app.ico");
            if (!File.Exists(icoPath)) icoPath = Path.Combine(baseDir, "..", "assets", "app.ico");
            if (File.Exists(icoPath))
            {
                try { this.Icon = new Icon(icoPath); } catch { }
            }

            // Top Instructions
            Label lblHeader = new Label();
            lblHeader.Text = "Switch OCR recognition engines, configure API keys, and customize shortcuts.";
            lblHeader.Location = new Point(24, 14);
            lblHeader.AutoSize = true;
            lblHeader.ForeColor = Color.DarkSlateGray;
            this.Controls.Add(lblHeader);

            // Group 1: OCR Engine & Language
            GroupBox grpEngine = new GroupBox();
            grpEngine.Text = "OCR Recognition Engine";
            grpEngine.Location = new Point(24, 42);
            grpEngine.Size = new Size(496, 105);

            Label lblEng = new Label();
            lblEng.Text = "Active Engine:";
            lblEng.Location = new Point(20, 26);
            lblEng.AutoSize = true;
            grpEngine.Controls.Add(lblEng);

            cmbEngine = new ComboBox();
            cmbEngine.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEngine.Location = new Point(140, 23);
            cmbEngine.Width = 330;
            cmbEngine.Items.Add("PaddleOCR (Offline - High Arabic Accuracy) [Default]");
            cmbEngine.Items.Add("Gemini 1.5 Flash (Cloud - Perfect Human Accuracy)");
            cmbEngine.Items.Add("Google Cloud Vision (Cloud - Vision API)");
            cmbEngine.Items.Add("Tesseract (Offline - Classic)");

            if (ocrEngine == "gemini") cmbEngine.SelectedIndex = 1;
            else if (ocrEngine == "vision" || ocrEngine == "cloud-vision") cmbEngine.SelectedIndex = 2;
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
            grpCloud.Location = new Point(24, 155);
            grpCloud.Size = new Size(496, 95);

            Label lblKey = new Label();
            lblKey.Text = "API Key:";
            lblKey.Location = new Point(20, 28);
            lblKey.AutoSize = true;
            grpCloud.Controls.Add(lblKey);

            txtGeminiKey = new TextBox();
            txtGeminiKey.Location = new Point(140, 25);
            txtGeminiKey.Width = 330;
            txtGeminiKey.Text = geminiKey;
            txtGeminiKey.UseSystemPasswordChar = true;
            grpCloud.Controls.Add(txtGeminiKey);

            lblKeyHelp = new Label();
            lblKeyHelp.Text = "Free tier: 15 requests/min.";
            lblKeyHelp.Location = new Point(140, 58);
            lblKeyHelp.AutoSize = true;
            lblKeyHelp.ForeColor = Color.DimGray;
            grpCloud.Controls.Add(lblKeyHelp);

            lnkGetKey = new LinkLabel();
            lnkGetKey.Text = "Get Free Google AI Studio API Key";
            lnkGetKey.Location = new Point(290, 58);
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
            grpSnip.Location = new Point(24, 258);
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
            grpHist.Location = new Point(24, 341);
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
            btnSave.Location = new Point(110, 440);
            btnSave.Click += (s, e) => SaveAndApply();
            this.Controls.Add(btnSave);

            btnReset = new Button();
            btnReset.Text = "Reset Defaults";
            btnReset.Size = new Size(130, 36);
            btnReset.Location = new Point(255, 440);
            btnReset.Click += (s, e) => ResetDefaults();
            this.Controls.Add(btnReset);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(100, 36);
            btnCancel.Location = new Point(400, 440);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            UpdateEngineUIState();
        }

        private void UpdateEngineUIState()
        {
            bool isCloud = (cmbEngine.SelectedIndex == 1 || cmbEngine.SelectedIndex == 2);
            grpCloud.Enabled = isCloud;
            txtGeminiKey.BackColor = isCloud ? Color.White : Color.FromArgb(245, 245, 245);
            if (cmbEngine.SelectedIndex == 1)
            {
                grpCloud.Text = "Gemini 1.5 Flash API Key";
                lnkGetKey.Visible = true;
            }
            else if (cmbEngine.SelectedIndex == 2)
            {
                grpCloud.Text = "Google Cloud Vision API Key";
                lnkGetKey.Visible = false;
            }
            else
            {
                grpCloud.Text = "Cloud API Key (Not required for offline engine)";
                lnkGetKey.Visible = false;
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

using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
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

        // Recording states
        private bool isRecordingSnip = false;
        private bool isRecordingHist = false;

        private ComboBox cmbEngine;
        private ComboBox cmbLanguage;
        private TextBox txtGeminiKey;
        private Button btnTestKey;
        private Label lblKeyHelp;
        private LinkLabel lnkGetKey;
        private GroupBox grpCloud;

        // Snip controls
        private CheckBox chkSnipCtrl;
        private CheckBox chkSnipAlt;
        private CheckBox chkSnipShift;
        private CheckBox chkSnipWin;
        private ComboBox cmbSnipKey;
        private Button btnRecordSnip;
        private Label lblSnipPreview;

        // History controls
        private CheckBox chkHistCtrl;
        private CheckBox chkHistAlt;
        private CheckBox chkHistShift;
        private CheckBox chkHistWin;
        private ComboBox cmbHistKey;
        private Button btnRecordHist;
        private Label lblHistPreview;

        private Button btnSave;
        private Button btnReset;
        private Button btnCancel;

        public SettingsForm(SnipperContext ctx)
        {
            this.context = ctx;
            this.Text = "Settings - Screen OCR Extractor";
            this.Size = new Size(580, 680);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.KeyPreview = true;

            AppConfig cfg = ConfigManager.Load();
            this.snipConfig = cfg.Snip ?? new HotkeyConfig(false, true, true, false, Keys.A);
            this.histConfig = cfg.History ?? new HotkeyConfig(false, true, true, false, Keys.H);
            this.ocrEngine = cfg.OcrEngine ?? "paddle";
            this.ocrLanguage = cfg.OcrLanguage ?? "auto";
            this.geminiKey = cfg.GeminiApiKey ?? "";

            InitUI();
        }

        private void InitUI()
        {
            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "Settings & Preferences";
            lblTitle.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblTitle.Location = new Point(24, 16);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Group 1: OCR Engine & Language Selection
            GroupBox grpEngine = new GroupBox();
            grpEngine.Text = "OCR Engine & Language";
            grpEngine.Location = new Point(24, 50);
            grpEngine.Size = new Size(516, 102);

            Label lblEng = new Label();
            lblEng.Text = "Engine:";
            lblEng.Location = new Point(18, 28);
            lblEng.AutoSize = true;
            grpEngine.Controls.Add(lblEng);

            cmbEngine = new ComboBox();
            cmbEngine.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEngine.Location = new Point(135, 25);
            cmbEngine.Width = 360;
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
            lblLang.Text = "Language:";
            lblLang.Location = new Point(18, 64);
            lblLang.AutoSize = true;
            grpEngine.Controls.Add(lblLang);

            cmbLanguage = new ComboBox();
            cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLanguage.Location = new Point(135, 61);
            cmbLanguage.Width = 360;
            cmbLanguage.Items.Add("All Languages (Auto Detect)");
            cmbLanguage.Items.Add("Arabic (العربية)");
            cmbLanguage.Items.Add("English");
            cmbLanguage.Items.Add("French (Français)");
            cmbLanguage.Items.Add("German (Deutsch)");
            cmbLanguage.Items.Add("Greek (Ελληνικά)");

            if (ocrLanguage == "ara") cmbLanguage.SelectedIndex = 1;
            else if (ocrLanguage == "eng") cmbLanguage.SelectedIndex = 2;
            else if (ocrLanguage == "fra") cmbLanguage.SelectedIndex = 3;
            else if (ocrLanguage == "deu") cmbLanguage.SelectedIndex = 4;
            else if (ocrLanguage == "ell") cmbLanguage.SelectedIndex = 5;
            else cmbLanguage.SelectedIndex = 0;

            grpEngine.Controls.Add(cmbLanguage);
            this.Controls.Add(grpEngine);

            // Group 2: Cloud API Key
            grpCloud = new GroupBox();
            grpCloud.Text = "Cloud API Key (Optional for Gemini 1.5 Flash / Cloud Vision)";
            grpCloud.Location = new Point(24, 160);
            grpCloud.Size = new Size(516, 102);

            Label lblKey = new Label();
            lblKey.Text = "API Key:";
            lblKey.Location = new Point(18, 28);
            lblKey.AutoSize = true;
            grpCloud.Controls.Add(lblKey);

            txtGeminiKey = new TextBox();
            txtGeminiKey.Location = new Point(135, 25);
            txtGeminiKey.Width = 265;
            txtGeminiKey.Text = geminiKey;
            txtGeminiKey.UseSystemPasswordChar = true;
            grpCloud.Controls.Add(txtGeminiKey);

            btnTestKey = new Button();
            btnTestKey.Text = "Test Key";
            btnTestKey.Location = new Point(410, 23);
            btnTestKey.Size = new Size(85, 26);
            btnTestKey.Click += (s, e) => TestApiKeyAsync();
            grpCloud.Controls.Add(btnTestKey);

            lblKeyHelp = new Label();
            lblKeyHelp.Text = "Free tier: 15 requests/min.";
            lblKeyHelp.Location = new Point(135, 60);
            lblKeyHelp.AutoSize = true;
            lblKeyHelp.ForeColor = Color.DimGray;
            grpCloud.Controls.Add(lblKeyHelp);

            lnkGetKey = new LinkLabel();
            lnkGetKey.Text = "Get Free API Key";
            lnkGetKey.Location = new Point(380, 60);
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
            grpSnip.Text = "Screen Snipper Shortcut (Capture & OCR)";
            grpSnip.Location = new Point(24, 270);
            grpSnip.Size = new Size(516, 120);

            // Modifiers
            chkSnipCtrl = new CheckBox() { Text = "Ctrl", Location = new Point(18, 26), AutoSize = true, Checked = snipConfig.Ctrl };
            chkSnipAlt = new CheckBox() { Text = "Alt", Location = new Point(78, 26), AutoSize = true, Checked = snipConfig.Alt };
            chkSnipShift = new CheckBox() { Text = "Shift", Location = new Point(135, 26), AutoSize = true, Checked = snipConfig.Shift };
            chkSnipWin = new CheckBox() { Text = "Win", Location = new Point(198, 26), AutoSize = true, Checked = snipConfig.Win };

            chkSnipCtrl.CheckedChanged += (s, e) => UpdateSnipPreview();
            chkSnipAlt.CheckedChanged += (s, e) => UpdateSnipPreview();
            chkSnipShift.CheckedChanged += (s, e) => UpdateSnipPreview();
            chkSnipWin.CheckedChanged += (s, e) => UpdateSnipPreview();

            grpSnip.Controls.Add(chkSnipCtrl);
            grpSnip.Controls.Add(chkSnipAlt);
            grpSnip.Controls.Add(chkSnipShift);
            grpSnip.Controls.Add(chkSnipWin);

            Label lblKeyPrompt = new Label() { Text = "+ Key:", Location = new Point(260, 28), AutoSize = true };
            grpSnip.Controls.Add(lblKeyPrompt);

            cmbSnipKey = new ComboBox();
            cmbSnipKey.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSnipKey.Location = new Point(310, 25);
            cmbSnipKey.Width = 85;
            PopulateKeysCombo(cmbSnipKey, snipConfig.Key);
            cmbSnipKey.SelectedIndexChanged += (s, e) => UpdateSnipPreview();
            grpSnip.Controls.Add(cmbSnipKey);

            btnRecordSnip = new Button();
            btnRecordSnip.Text = "Press to Record";
            btnRecordSnip.Location = new Point(405, 23);
            btnRecordSnip.Size = new Size(95, 28);
            btnRecordSnip.Click += (s, e) => StartRecording(true);
            grpSnip.Controls.Add(btnRecordSnip);

            lblSnipPreview = new Label();
            lblSnipPreview.Location = new Point(18, 68);
            lblSnipPreview.Size = new Size(480, 36);
            lblSnipPreview.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            lblSnipPreview.ForeColor = Color.DarkSlateBlue;
            grpSnip.Controls.Add(lblSnipPreview);

            this.Controls.Add(grpSnip);

            // Group 4: History Shortcut
            GroupBox grpHist = new GroupBox();
            grpHist.Text = "Text History Shortcut";
            grpHist.Location = new Point(24, 398);
            grpHist.Size = new Size(516, 120);

            chkHistCtrl = new CheckBox() { Text = "Ctrl", Location = new Point(18, 26), AutoSize = true, Checked = histConfig.Ctrl };
            chkHistAlt = new CheckBox() { Text = "Alt", Location = new Point(78, 26), AutoSize = true, Checked = histConfig.Alt };
            chkHistShift = new CheckBox() { Text = "Shift", Location = new Point(135, 26), AutoSize = true, Checked = histConfig.Shift };
            chkHistWin = new CheckBox() { Text = "Win", Location = new Point(198, 26), AutoSize = true, Checked = histConfig.Win };

            chkHistCtrl.CheckedChanged += (s, e) => UpdateHistPreview();
            chkHistAlt.CheckedChanged += (s, e) => UpdateHistPreview();
            chkHistShift.CheckedChanged += (s, e) => UpdateHistPreview();
            chkHistWin.CheckedChanged += (s, e) => UpdateHistPreview();

            grpHist.Controls.Add(chkHistCtrl);
            grpHist.Controls.Add(chkHistAlt);
            grpHist.Controls.Add(chkHistShift);
            grpHist.Controls.Add(chkHistWin);

            Label lblHistKeyPrompt = new Label() { Text = "+ Key:", Location = new Point(260, 28), AutoSize = true };
            grpHist.Controls.Add(lblHistKeyPrompt);

            cmbHistKey = new ComboBox();
            cmbHistKey.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbHistKey.Location = new Point(310, 25);
            cmbHistKey.Width = 85;
            PopulateKeysCombo(cmbHistKey, histConfig.Key);
            cmbHistKey.SelectedIndexChanged += (s, e) => UpdateHistPreview();
            grpHist.Controls.Add(cmbHistKey);

            btnRecordHist = new Button();
            btnRecordHist.Text = "Press to Record";
            btnRecordHist.Location = new Point(405, 23);
            btnRecordHist.Size = new Size(95, 28);
            btnRecordHist.Click += (s, e) => StartRecording(false);
            grpHist.Controls.Add(btnRecordHist);

            lblHistPreview = new Label();
            lblHistPreview.Location = new Point(18, 68);
            lblHistPreview.Size = new Size(480, 36);
            lblHistPreview.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            lblHistPreview.ForeColor = Color.DarkSlateBlue;
            grpHist.Controls.Add(lblHistPreview);

            this.Controls.Add(grpHist);

            // Bottom Buttons
            btnSave = new Button();
            btnSave.Text = "Save & Apply";
            btnSave.Size = new Size(130, 38);
            btnSave.Location = new Point(110, 560);
            btnSave.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnSave.BackColor = Color.FromArgb(16, 185, 129);
            btnSave.ForeColor = Color.White;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => SaveAndApply();
            this.Controls.Add(btnSave);

            btnReset = new Button();
            btnReset.Text = "Reset Defaults";
            btnReset.Size = new Size(130, 38);
            btnReset.Location = new Point(255, 560);
            btnReset.Click += (s, e) => ResetDefaults();
            this.Controls.Add(btnReset);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(100, 38);
            btnCancel.Location = new Point(400, 560);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            UpdateEngineUIState();
            UpdateSnipPreview();
            UpdateHistPreview();
        }

        private void PopulateKeysCombo(ComboBox cmb, Keys selectedKey)
        {
            cmb.Items.Clear();
            // A - Z
            for (char c = 'A'; c <= 'Z'; c++)
            {
                cmb.Items.Add(c.ToString());
            }
            // 0 - 9
            for (char c = '0'; c <= '9'; c++)
            {
                cmb.Items.Add(c.ToString());
            }
            // Function keys
            for (int f = 1; f <= 12; f++)
            {
                cmb.Items.Add("F" + f);
            }
            cmb.Items.Add("PrintScreen");
            cmb.Items.Add("Space");
            cmb.Items.Add("Insert");
            cmb.Items.Add("Delete");
            cmb.Items.Add("Home");
            cmb.Items.Add("End");
            cmb.Items.Add("PageUp");
            cmb.Items.Add("PageDown");

            string target = selectedKey.ToString();
            int idx = cmb.Items.IndexOf(target);
            if (idx >= 0) cmb.SelectedIndex = idx;
            else if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
        }

        private void StartRecording(bool isSnip)
        {
            if (isSnip)
            {
                isRecordingSnip = true;
                isRecordingHist = false;
                btnRecordSnip.Text = "Listening...";
                btnRecordSnip.BackColor = Color.Orange;
                btnRecordHist.Text = "Press to Record";
                btnRecordHist.BackColor = SystemColors.Control;
                lblSnipPreview.Text = "Press your desired shortcut combination now (e.g. Ctrl+Shift+A)...";
                lblSnipPreview.ForeColor = Color.OrangeRed;
            }
            else
            {
                isRecordingHist = true;
                isRecordingSnip = false;
                btnRecordHist.Text = "Listening...";
                btnRecordHist.BackColor = Color.Orange;
                btnRecordSnip.Text = "Press to Record";
                btnRecordSnip.BackColor = SystemColors.Control;
                lblHistPreview.Text = "Press your desired shortcut combination now (e.g. Ctrl+Shift+H)...";
                lblHistPreview.ForeColor = Color.OrangeRed;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (isRecordingSnip || isRecordingHist)
            {
                Keys key = keyData & Keys.KeyCode;
                bool ctrl = (keyData & Keys.Control) != 0;
                bool alt = (keyData & Keys.Alt) != 0;
                bool shift = (keyData & Keys.Shift) != 0;
                bool win = false;

                // Ignore bare modifier key presses until the actual trigger key is pressed
                if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu ||
                    key == Keys.LWin || key == Keys.RWin)
                {
                    return base.ProcessCmdKey(ref msg, keyData);
                }

                if (isRecordingSnip)
                {
                    chkSnipCtrl.Checked = ctrl;
                    chkSnipAlt.Checked = alt;
                    chkSnipShift.Checked = shift;
                    chkSnipWin.Checked = win;

                    string keyStr = key.ToString();
                    int idx = cmbSnipKey.Items.IndexOf(keyStr);
                    if (idx >= 0) cmbSnipKey.SelectedIndex = idx;

                    isRecordingSnip = false;
                    btnRecordSnip.Text = "Press to Record";
                    btnRecordSnip.BackColor = SystemColors.Control;
                    UpdateSnipPreview();
                    return true;
                }
                else if (isRecordingHist)
                {
                    chkHistCtrl.Checked = ctrl;
                    chkHistAlt.Checked = alt;
                    chkHistShift.Checked = shift;
                    chkHistWin.Checked = win;

                    string keyStr = key.ToString();
                    int idx = cmbHistKey.Items.IndexOf(keyStr);
                    if (idx >= 0) cmbHistKey.SelectedIndex = idx;

                    isRecordingHist = false;
                    btnRecordHist.Text = "Press to Record";
                    btnRecordHist.BackColor = SystemColors.Control;
                    UpdateHistPreview();
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private HotkeyConfig BuildSnipConfig()
        {
            string keyStr = cmbSnipKey.SelectedItem != null ? cmbSnipKey.SelectedItem.ToString() : "A";
            Keys k = Keys.A;
            try { k = (Keys)Enum.Parse(typeof(Keys), keyStr, true); } catch { }
            return new HotkeyConfig(chkSnipAlt.Checked, chkSnipCtrl.Checked, chkSnipShift.Checked, chkSnipWin.Checked, k);
        }

        private HotkeyConfig BuildHistConfig()
        {
            string keyStr = cmbHistKey.SelectedItem != null ? cmbHistKey.SelectedItem.ToString() : "H";
            Keys k = Keys.H;
            try { k = (Keys)Enum.Parse(typeof(Keys), keyStr, true); } catch { }
            return new HotkeyConfig(chkHistAlt.Checked, chkHistCtrl.Checked, chkHistShift.Checked, chkHistWin.Checked, k);
        }

        private void UpdateSnipPreview()
        {
            if (isRecordingSnip) return;
            HotkeyConfig cfg = BuildSnipConfig();
            lblSnipPreview.Text = "Active Shortcut: [" + cfg.ToString() + "]";
            lblSnipPreview.ForeColor = Color.DarkSlateBlue;
        }

        private void UpdateHistPreview()
        {
            if (isRecordingHist) return;
            HotkeyConfig cfg = BuildHistConfig();
            lblHistPreview.Text = "Active Shortcut: [" + cfg.ToString() + "]";
            lblHistPreview.ForeColor = Color.DarkSlateBlue;
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

        private void SaveAndApply()
        {
            HotkeyConfig newSnip = BuildSnipConfig();
            HotkeyConfig newHist = BuildHistConfig();

            // Validate modifiers
            if (!newSnip.Ctrl && !newSnip.Alt && !newSnip.Shift && !newSnip.Win)
            {
                MessageBox.Show(this, "Please select at least one modifier key (Ctrl, Alt, Shift, or Win) for the Screen Snipper shortcut.", "Invalid Shortcut", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!newHist.Ctrl && !newHist.Alt && !newHist.Shift && !newHist.Win)
            {
                MessageBox.Show(this, "Please select at least one modifier key (Ctrl, Alt, Shift, or Win) for the History shortcut.", "Invalid Shortcut", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Test if shortcuts can be registered with Windows
            string errSnip;
            if (!HotkeyWindow.TestRegister(newSnip, out errSnip))
            {
                MessageBox.Show(this, "Cannot use Screen Snipper shortcut:\n\n" + errSnip + "\n\nPlease select another key combination.", "Shortcut Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string errHist;
            if (!HotkeyWindow.TestRegister(newHist, out errHist))
            {
                MessageBox.Show(this, "Cannot use History shortcut:\n\n" + errHist + "\n\nPlease select another key combination.", "Shortcut Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AppConfig cfg = new AppConfig();
            cfg.Snip = newSnip;
            cfg.History = newHist;

            if (cmbEngine.SelectedIndex == 1) cfg.OcrEngine = "gemini";
            else if (cmbEngine.SelectedIndex == 2) cfg.OcrEngine = "vision";
            else if (cmbEngine.SelectedIndex == 3) cfg.OcrEngine = "tesseract";
            else cfg.OcrEngine = "paddle";

            if (cmbLanguage.SelectedIndex == 1) cfg.OcrLanguage = "ara";
            else if (cmbLanguage.SelectedIndex == 2) cfg.OcrLanguage = "eng";
            else if (cmbLanguage.SelectedIndex == 3) cfg.OcrLanguage = "fra";
            else if (cmbLanguage.SelectedIndex == 4) cfg.OcrLanguage = "deu";
            else if (cmbLanguage.SelectedIndex == 5) cfg.OcrLanguage = "ell";
            else cfg.OcrLanguage = "auto";

            cfg.GeminiApiKey = txtGeminiKey.Text.Trim();

            ConfigManager.Save(cfg);
            context.ApplyUpdatedShortcuts();
            this.Close();
        }

        private void ResetDefaults()
        {
            chkSnipCtrl.Checked = true;
            chkSnipAlt.Checked = false;
            chkSnipShift.Checked = true;
            chkSnipWin.Checked = false;
            cmbSnipKey.SelectedIndex = cmbSnipKey.Items.IndexOf("A");

            chkHistCtrl.Checked = true;
            chkHistAlt.Checked = false;
            chkHistShift.Checked = true;
            chkHistWin.Checked = false;
            cmbHistKey.SelectedIndex = cmbHistKey.Items.IndexOf("H");

            cmbEngine.SelectedIndex = 0; // PaddleOCR
            cmbLanguage.SelectedIndex = 0; // Auto Detect
            UpdateEngineUIState();
            UpdateSnipPreview();
            UpdateHistPreview();
        }
    }
}

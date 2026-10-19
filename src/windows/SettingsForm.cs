using System;
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
        private ComboBox cmbLanguage;
        private HotkeyConfig snipConfig;
        private HotkeyConfig histConfig;
        private string ocrLanguage;
        private Button btnSave;
        private Button btnReset;
        private Button btnCancel;

        public SettingsForm(SnipperContext ctx)
        {
            context = ctx;
            AppConfig current = ConfigManager.GetConfig();
            snipConfig = new HotkeyConfig(current.Snip.Alt, current.Snip.Ctrl, current.Snip.Shift, current.Snip.Win, current.Snip.Key);
            histConfig = new HotkeyConfig(current.History.Alt, current.History.Ctrl, current.History.Shift, current.History.Win, current.History.Key);
            ocrLanguage = current.OcrLanguage ?? "auto";

            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Settings & Shortcuts";
            this.Size = new Size(520, 440);
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
            lblHeader.Text = "Customize your global shortcuts and OCR recognition language mode.";
            lblHeader.Location = new Point(24, 16);
            lblHeader.AutoSize = true;
            lblHeader.ForeColor = Color.DarkSlateGray;
            this.Controls.Add(lblHeader);

            // Group 1: OCR Language Mode
            GroupBox grpLang = new GroupBox();
            grpLang.Text = "OCR Recognition Mode";
            grpLang.Location = new Point(24, 46);
            grpLang.Size = new Size(456, 75);

            Label lblLang = new Label();
            lblLang.Text = "Language Mode:";
            lblLang.Location = new Point(20, 28);
            lblLang.AutoSize = true;
            grpLang.Controls.Add(lblLang);

            cmbLanguage = new ComboBox();
            cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLanguage.Location = new Point(140, 25);
            cmbLanguage.Width = 290;
            cmbLanguage.Items.Add("Arabic & English (Auto)");
            cmbLanguage.Items.Add("Arabic Only (Highest Arabic Accuracy)");
            cmbLanguage.Items.Add("English Only");

            if (ocrLanguage == "ara") cmbLanguage.SelectedIndex = 1;
            else if (ocrLanguage == "eng") cmbLanguage.SelectedIndex = 2;
            else cmbLanguage.SelectedIndex = 0;

            grpLang.Controls.Add(cmbLanguage);
            this.Controls.Add(grpLang);

            // Group 2: Screen Snipper
            GroupBox grpSnip = new GroupBox();
            grpSnip.Text = "Screen Snipper Shortcut";
            grpSnip.Location = new Point(24, 132);
            grpSnip.Size = new Size(456, 80);

            Label lblSnipHelp = new Label();
            lblSnipHelp.Text = "Active Shortcut:";
            lblSnipHelp.Location = new Point(20, 30);
            lblSnipHelp.AutoSize = true;
            grpSnip.Controls.Add(lblSnipHelp);

            txtSnip = new TextBox();
            txtSnip.Location = new Point(140, 27);
            txtSnip.Width = 290;
            txtSnip.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            txtSnip.TextAlign = HorizontalAlignment.Center;
            txtSnip.Text = snipConfig.ToString();
            txtSnip.ReadOnly = true;
            txtSnip.BackColor = Color.White;
            txtSnip.KeyDown += (s, e) => HandleKeyRecord(e, ref snipConfig, txtSnip);
            grpSnip.Controls.Add(txtSnip);

            this.Controls.Add(grpSnip);

            // Group 3: History Window
            GroupBox grpHist = new GroupBox();
            grpHist.Text = "Text History Window Shortcut";
            grpHist.Location = new Point(24, 222);
            grpHist.Size = new Size(456, 80);

            Label lblHistHelp = new Label();
            lblHistHelp.Text = "Active Shortcut:";
            lblHistHelp.Location = new Point(20, 30);
            lblHistHelp.AutoSize = true;
            grpHist.Controls.Add(lblHistHelp);

            txtHistory = new TextBox();
            txtHistory.Location = new Point(140, 27);
            txtHistory.Width = 290;
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
            btnSave.Size = new Size(120, 34);
            btnSave.Location = new Point(110, 325);
            btnSave.Click += (s, e) => SaveAndApply();
            this.Controls.Add(btnSave);

            btnReset = new Button();
            btnReset.Text = "Reset Defaults";
            btnReset.Size = new Size(120, 34);
            btnReset.Location = new Point(245, 325);
            btnReset.Click += (s, e) => ResetDefaults();
            this.Controls.Add(btnReset);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(95, 34);
            btnCancel.Location = new Point(380, 325);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
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

            if (cmbLanguage.SelectedIndex == 1) cfg.OcrLanguage = "ara";
            else if (cmbLanguage.SelectedIndex == 2) cfg.OcrLanguage = "eng";
            else cfg.OcrLanguage = "auto";

            ConfigManager.Save(cfg);
            context.ApplyUpdatedShortcuts();
            this.Close();
        }

        private void ResetDefaults()
        {
            snipConfig = new HotkeyConfig(true, false, true, false, Keys.A);
            histConfig = new HotkeyConfig(true, false, true, false, Keys.H);
            cmbLanguage.SelectedIndex = 0;
            txtSnip.Text = snipConfig.ToString();
            txtHistory.Text = histConfig.ToString();
        }
    }
}

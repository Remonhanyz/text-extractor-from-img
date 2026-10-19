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
        private HotkeyConfig snipConfig;
        private HotkeyConfig histConfig;
        private Button btnSave;
        private Button btnReset;
        private Button btnCancel;

        public SettingsForm(SnipperContext ctx)
        {
            context = ctx;
            AppConfig current = ConfigManager.GetConfig();
            snipConfig = new HotkeyConfig(current.Snip.Alt, current.Snip.Ctrl, current.Snip.Shift, current.Snip.Win, current.Snip.Key);
            histConfig = new HotkeyConfig(current.History.Alt, current.History.Ctrl, current.History.Shift, current.History.Win, current.History.Key);

            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Shortcut Settings";
            this.Size = new Size(500, 360);
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
            lblHeader.Text = "Click inside any shortcut box and press your desired key combination.";
            lblHeader.Location = new Point(24, 18);
            lblHeader.AutoSize = true;
            lblHeader.ForeColor = Color.DarkSlateGray;
            this.Controls.Add(lblHeader);

            // Group 1: Screen Snipper
            GroupBox grpSnip = new GroupBox();
            grpSnip.Text = "Screen Snipper Shortcut";
            grpSnip.Location = new Point(24, 50);
            grpSnip.Size = new Size(436, 90);

            Label lblSnipHelp = new Label();
            lblSnipHelp.Text = "Active Shortcut:";
            lblSnipHelp.Location = new Point(20, 34);
            lblSnipHelp.AutoSize = true;
            grpSnip.Controls.Add(lblSnipHelp);

            txtSnip = new TextBox();
            txtSnip.Location = new Point(140, 30);
            txtSnip.Width = 260;
            txtSnip.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            txtSnip.TextAlign = HorizontalAlignment.Center;
            txtSnip.Text = snipConfig.ToString();
            txtSnip.ReadOnly = true;
            txtSnip.BackColor = Color.White;
            txtSnip.KeyDown += (s, e) => HandleKeyRecord(e, ref snipConfig, txtSnip);
            grpSnip.Controls.Add(txtSnip);

            this.Controls.Add(grpSnip);

            // Group 2: History Window
            GroupBox grpHist = new GroupBox();
            grpHist.Text = "Text History Window Shortcut";
            grpHist.Location = new Point(24, 155);
            grpHist.Size = new Size(436, 90);

            Label lblHistHelp = new Label();
            lblHistHelp.Text = "Active Shortcut:";
            lblHistHelp.Location = new Point(20, 34);
            lblHistHelp.AutoSize = true;
            grpHist.Controls.Add(lblHistHelp);

            txtHistory = new TextBox();
            txtHistory.Location = new Point(140, 30);
            txtHistory.Width = 260;
            txtHistory.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
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
            btnSave.Location = new Point(100, 265);
            btnSave.Click += (s, e) => SaveAndApply();
            this.Controls.Add(btnSave);

            btnReset = new Button();
            btnReset.Text = "Reset Defaults";
            btnReset.Size = new Size(120, 34);
            btnReset.Location = new Point(230, 265);
            btnReset.Click += (s, e) => ResetDefaults();
            this.Controls.Add(btnReset);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(90, 34);
            btnCancel.Location = new Point(360, 265);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private void HandleKeyRecord(KeyEventArgs e, ref HotkeyConfig target, TextBox box)
        {
            e.SuppressKeyPress = true;

            // Ignore pure modifier presses
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey ||
                e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
            {
                return;
            }

            bool alt = e.Alt;
            bool ctrl = e.Control;
            bool shift = e.Shift;
            Keys key = e.KeyCode;

            // Ensure at least one modifier is selected
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

            ConfigManager.Save(cfg);
            context.ApplyUpdatedShortcuts();
            this.Close();
        }

        private void ResetDefaults()
        {
            snipConfig = new HotkeyConfig(true, false, true, false, Keys.A); // Alt+Shift+A
            histConfig = new HotkeyConfig(true, false, true, false, Keys.H); // Alt+Shift+H
            txtSnip.Text = snipConfig.ToString();
            txtHistory.Text = histConfig.ToString();
        }
    }
}

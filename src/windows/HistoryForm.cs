using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace ArabicTextExtractor
{
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

            // Set custom icon from assets
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

            // Top Panel (Search)
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

            // Bottom Panel (Buttons & Status)
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

            // Split Container
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
}

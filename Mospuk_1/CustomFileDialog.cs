using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mospuk_1
{
    public partial class CustomFileDialog : Form
    {
        public string SelectedFilePath { get; private set; }
        public DialogResult Result { get; private set; }

        private ListView listView;
        private TextBox pathTextBox;
        private ComboBox filterComboBox;
        private Button okButton;
        private Button cancelButton;

        public CustomFileDialog(string initialDirectory, string filter, string title)
        {
            InitializeComponent(initialDirectory, filter, title);
        }

        private void InitializeComponent(string initialDirectory, string filter, string title)
        {
            this.Text = title;
            this.Size = new Size(800, 600);  // حجم قابل للتغيير
            this.MinimumSize = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;  // قابل للتكبير والتصغير
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            // إنشاء الـ Controls
            CreateControls(initialDirectory, filter);
            LoadDirectory(initialDirectory);
        }

        private void CreateControls(string initialDirectory, string filter)
        {
            // Path TextBox
            pathTextBox = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(this.Width - 30, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = initialDirectory
            };
            pathTextBox.KeyPress += PathTextBox_KeyPress;

            // ListView for files
            listView = new ListView
            {
                Location = new Point(10, 45),
                Size = new Size(this.Width - 30, this.Height - 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };
            listView.Columns.Add("Name", 300);
            listView.Columns.Add("Size", 100);
            listView.Columns.Add("Type", 100);
            listView.Columns.Add("Modified", 150);
            listView.DoubleClick += ListView_DoubleClick;

            // Filter ComboBox
            filterComboBox = new ComboBox
            {
                Location = new Point(10, this.Height - 65),
                Size = new Size(200, 25),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Parse filter string
            string[] filters = filter.Split('|');
            for (int i = 0; i < filters.Length; i += 2)
            {
                if (i + 1 < filters.Length)
                {
                    filterComboBox.Items.Add(filters[i]);
                }
            }
            if (filterComboBox.Items.Count > 0)
                filterComboBox.SelectedIndex = 0;

            filterComboBox.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;

            // Buttons
            okButton = new Button
            {
                Text = "OK",
                Location = new Point(this.Width - 180, this.Height - 65),
                Size = new Size(80, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(this.Width - 90, this.Height - 65),
                Size = new Size(80, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            cancelButton.Click += CancelButton_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] { pathTextBox, listView, filterComboBox, okButton, cancelButton });
        }

        private void LoadDirectory(string path)
        {
            try
            {
                listView.Items.Clear();
                DirectoryInfo dir = new DirectoryInfo(path);

                // Add parent directory
                if (dir.Parent != null)
                {
                    ListViewItem parentItem = new ListViewItem("..");
                    parentItem.SubItems.Add("");
                    parentItem.SubItems.Add("Folder");
                    parentItem.SubItems.Add("");
                    parentItem.Tag = dir.Parent.FullName;
                    parentItem.ImageIndex = 0;
                    listView.Items.Add(parentItem);
                }

                // Add directories
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    ListViewItem item = new ListViewItem(subDir.Name);
                    item.SubItems.Add("");
                    item.SubItems.Add("Folder");
                    item.SubItems.Add(subDir.LastWriteTime.ToString("yyyy/MM/dd HH:mm"));
                    item.Tag = subDir.FullName;
                    listView.Items.Add(item);
                }

                // Add files (filter based on selection)
                string[] extensions = GetCurrentFilterExtensions();
                foreach (FileInfo file in dir.GetFiles())
                {
                    if (extensions.Length == 0 || extensions.Contains("*.*") ||
                        extensions.Any(ext => file.Extension.ToLower() == ext.Replace("*", "").ToLower()))
                    {
                        ListViewItem item = new ListViewItem(file.Name);
                        item.SubItems.Add(FormatFileSize(file.Length));
                        item.SubItems.Add("File");
                        item.SubItems.Add(file.LastWriteTime.ToString("yyyy/MM/dd HH:mm"));
                        item.Tag = file.FullName;
                        listView.Items.Add(item);
                    }
                }

                pathTextBox.Text = path;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading directory: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string[] GetCurrentFilterExtensions()
        {
            if (filterComboBox.SelectedIndex >= 0)
            {
                // Get the filter pattern from the original filter string
                // This is a simplified version - you might want to enhance this
                return new string[] { "*.doc", "*.docx" };
            }
            return new string[] { "*.*" };
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return String.Format("{0:0.##} {1}", len, sizes[order]);
        }

        private void PathTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                LoadDirectory(pathTextBox.Text);
                e.Handled = true;
            }
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                string path = listView.SelectedItems[0].Tag.ToString();
                if (Directory.Exists(path))
                {
                    LoadDirectory(path);
                }
                else if (File.Exists(path))
                {
                    SelectedFilePath = path;
                    Result = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void FilterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadDirectory(pathTextBox.Text);
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                string path = listView.SelectedItems[0].Tag.ToString();
                if (File.Exists(path))
                {
                    SelectedFilePath = path;
                    Result = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Result = DialogResult.Cancel;
            this.Close();
        }
    }
}

// الحل الثالث: استخدام الـ Custom Dialog

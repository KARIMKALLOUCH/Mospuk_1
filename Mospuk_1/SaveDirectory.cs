using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace Mospuk_1
{
    public partial class SaveDirectory : Form
    {
        private readonly SQLiteDatabase db;

        private const string SAVE_PATH = "save";
        private const string ARCHIVE_PATH = "archive";
        private const string DOWNLOADS_PATH = "downloads";

        public SaveDirectory(SQLiteDatabase database)
        {
            InitializeComponent();
            this.db = database;
        }

        private void SaveDirectory_Load(object sender, EventArgs e)
        {
            LoadPathSetting(SAVE_PATH);
            LoadPathSetting(ARCHIVE_PATH);
            LoadPathSetting(DOWNLOADS_PATH);
        }

        private void btnsaveDirectory_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "اختر مجلد حفظ المشاريع الافتراضي";
                if (!string.IsNullOrEmpty(edittextsaveDirectory.Text))
                    folderDialog.SelectedPath = edittextsaveDirectory.Text;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;
                    edittextsaveDirectory.Text = selectedPath;
                    SavePathSetting(SAVE_PATH, selectedPath);
                }
            }
        }

        private void btnArchive_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "اختر مجلد الأرشيف الافتراضي";
                if (!string.IsNullOrEmpty(edittextarchive.Text))
                    folderDialog.SelectedPath = edittextarchive.Text;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;
                    edittextarchive.Text = selectedPath;
                    SavePathSetting(ARCHIVE_PATH, selectedPath);
                }
            }
        }

        private void btnDownloads_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "اختر مجلد التنزيلات الافتراضي";
                if (!string.IsNullOrEmpty(edittextDownloads.Text))
                    folderDialog.SelectedPath = edittextDownloads.Text;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;
                    edittextDownloads.Text = selectedPath;
                    SavePathSetting(DOWNLOADS_PATH, selectedPath);
                }
            }
        }

        private void LoadPathSetting(string pathType)
        {
            try
            {
                const string query = "SELECT path_value FROM user_paths WHERE path_type = @pathType";
                var parameters = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@pathType", pathType)
                };

                object result = db.ExecuteScalar(query, parameters);

                if (result != null)
                {
                    switch (pathType)
                    {
                        case SAVE_PATH:
                            edittextsaveDirectory.Text = result.ToString();
                            break;
                        case ARCHIVE_PATH:
                            edittextarchive.Text = result.ToString();
                            break;
                        case DOWNLOADS_PATH:
                            edittextDownloads.Text = result.ToString();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل المسار: {ex.Message}",
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SavePathSetting(string pathType, string path)
        {
            try
            {
                // ملاحظة: user_paths لديها UNIQUE(user_id, path_type)
                // لذلك نستخدم UPSERT بأسلوب SQLite:
                const string query = @"
                    INSERT INTO user_paths ( path_type, path_value)
                    VALUES ( @pathType, @path)
                    ON CONFLICT( path_type)
                    DO UPDATE SET
                        path_value = excluded.path_value,
                        updated_at = CURRENT_TIMESTAMP;";

                var parameters = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@pathType", pathType),
                    new SQLiteParameter("@path", path)
                };

                bool success = db.ExecuteNonQuery(query, parameters);

                if (!success)
                {
                    MessageBox.Show("فشل في حفظ المسار.", "خطأ",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ المسار: {ex.Message}",
                    "خطأ في قاعدة البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

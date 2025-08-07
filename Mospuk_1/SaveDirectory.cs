using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms;


namespace Mospuk_1
{
    public partial class SaveDirectory : Form
    {
        private MySqlDatabase db;
             private int userId; // متغير لحفظ ID المستخدم الحالي

        private const string SAVE_PATH = "save";
        private const string ARCHIVE_PATH = "archive";
        private const string DOWNLOADS_PATH = "downloads";
        public SaveDirectory(MySqlDatabase database, int userId)
        {
            InitializeComponent();
            this.db = database; // حفظ نسخة من اتصال قاعدة البيانات
            this.userId = userId; // حفظ ID المستخدم

        }

        private void btnsaveDirectory_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "اختر مجلد حفظ المشاريع الافتراضي";

                if (!string.IsNullOrEmpty(edittextsaveDirectory.Text))
                {
                    folderDialog.SelectedPath = edittextsaveDirectory.Text;
                }

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;
                    edittextsaveDirectory.Text = selectedPath;
                    SavePathSetting(SAVE_PATH, selectedPath);
                }
            }

        }
        private void LoadPathSetting(string pathType)
        {
            try
            {
                string query = "SELECT path_value FROM user_paths WHERE user_id = @userId AND path_type = @pathType";
                var parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@userId", userId),
                    new MySqlParameter("@pathType", pathType)
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
                string query = @"INSERT INTO user_paths (user_id, path_type, path_value) 
                                VALUES (@userId, @pathType, @path) 
                                ON DUPLICATE KEY UPDATE path_value = @path";

                var parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@userId", userId),
                    new MySqlParameter("@pathType", pathType),
                    new MySqlParameter("@path", path)
                };

                bool success = db.ExecuteNonQuery(query, parameters);

                if (!success)
                {
                    MessageBox.Show($"فشل في حفظ المسار.", "خطأ",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ المسار: {ex.Message}",
                              "خطأ في قاعدة البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void SaveDirectory_Load(object sender, EventArgs e)
        {
            LoadPathSetting(SAVE_PATH);
            LoadPathSetting(ARCHIVE_PATH);
            LoadPathSetting(DOWNLOADS_PATH);



        }

        private void btnArchive_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "اختر مجلد الأرشيف الافتراضي";

                if (!string.IsNullOrEmpty(edittextarchive.Text))
                {
                    folderDialog.SelectedPath = edittextarchive.Text;
                }

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
                {
                    folderDialog.SelectedPath = edittextDownloads.Text;
                }

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;
                    edittextDownloads.Text = selectedPath;
                    SavePathSetting(DOWNLOADS_PATH, selectedPath);
                }
            }
        }
    }
}


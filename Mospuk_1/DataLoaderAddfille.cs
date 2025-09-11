using System;
using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;
using SharpCompress.Archives; // إضافة هذا السطر
using SharpCompress.Common;   // إضافة هذا السطر
using System.Linq;            // إضافة هذا السطر للاستخدام مع LINQ (.Where)

namespace Mospuk_1
{
    public class DataLoaderAddfille
    {
        private readonly SQLiteDatabase db;

        // الكونستركتور يستقبل كائن قاعدة البيانات للتعامل معه
        public DataLoaderAddfille(SQLiteDatabase database)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database), "Database object cannot be null.");
            }
            db = database;
        }

        /// <summary>
        /// تحميل العملاء والشركات في ComboBox
        /// </summary>
        public void LoadClientsAndCompanies(ComboBox comboBox)
        {
            try
            {
                string query = @"SELECT client_id AS id, client_code AS code, 'Client' AS type FROM clients
                                 UNION ALL
                                 SELECT company_id AS id, company_code AS code, 'Company' AS type FROM companies
                                 ORDER BY type, code";
                DataTable combinedData = db.ExecuteQuery(query, null);

                comboBox.Items.Clear();
                comboBox.DisplayMember = "Value";
                comboBox.ValueMember = "Key";

                foreach (DataRow row in combinedData.Rows)
                {
                    int entityId = Convert.ToInt32(row["id"]);
                    string entityCode = row["code"].ToString();
                    comboBox.Items.Add(new KeyValuePair<int, string>(entityId, entityCode));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ في تحميل العملاء والشركات:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// تحميل أنواع المستندات في ComboBox
        /// </summary>
        public void LoadDocumentTypesToComboBox(ComboBox comboBox)
        {
            try
            {
                string query = "SELECT id, name AS code, 'Document Type' AS type FROM document_types ORDER BY type, code";
                DataTable documentTypesData = db.ExecuteQuery(query, null);

                comboBox.Items.Clear();
                comboBox.DisplayMember = "Value";
                comboBox.ValueMember = "Key";

                foreach (DataRow row in documentTypesData.Rows)
                {
                    int documentTypeId = Convert.ToInt32(row["id"]);
                    string documentTypeName = row["code"].ToString();
                    comboBox.Items.Add(new KeyValuePair<int, string>(documentTypeId, documentTypeName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading document types:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// تحميل أنواع الترجمة (أزواج اللغات) في ComboBox
        /// </summary>
        public void LoadLanguagePairsToComboBox(ComboBox comboBox)
        {
            try
            {
                string query = "SELECT id, name AS code, 'Language Pair' AS type FROM language_pairs ORDER BY type, code";
                DataTable languagePairsData = db.ExecuteQuery(query, null);

                comboBox.Items.Clear();
                comboBox.DisplayMember = "Value";
                comboBox.ValueMember = "Key";

                foreach (DataRow row in languagePairsData.Rows)
                {
                    int languagePairId = Convert.ToInt32(row["id"]);
                    string languagePairName = row["code"].ToString();
                    comboBox.Items.Add(new KeyValuePair<int, string>(languagePairId, languagePairName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading language pairs:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// تحميل المستخدمين في ComboBox
        /// </summary>
        public void LoadUsersToCombo(ComboBox comboBox)
        {
            try
            {
                string sql = @"SELECT user_id, user_code FROM users ORDER BY user_code";
                DataTable dt = db.ExecuteQuery(sql, null);

                comboBox.DataSource = dt;
                comboBox.DisplayMember = "user_code";
                comboBox.ValueMember = "user_id";
                comboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading users: " + ex.Message);
            }
        }

      
        public void ExtractRAR(string rarPath, string outputDirectory)
        {
            using (var archive = ArchiveFactory.Open(rarPath))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory(outputDirectory, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                }
            }
        }

        public void ExtractZIP(string zipPath, string outputDirectory)
        {
            using (var archive = ArchiveFactory.Open(zipPath))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory(outputDirectory, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                }
            }
        }
    }
}
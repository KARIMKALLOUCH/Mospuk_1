using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Data.SQLite; // تمت الإضافة
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace Mospuk_1
{
    public partial class Home : Form
    {
        SQLiteDatabase db; // تم التغيير
        private int selectedClientId = -1;
        private int selectedCompanyId = -1;
        private int selectedUserId = -1;
        private WebView2 webView;


        // تم تغيير نوع المتغير في الـ Constructor
        public Home(SQLiteDatabase database)
        {
            InitializeComponent();
            db = database;  // ← هنا تحفظ المتغير لتستخدمه لاحقاً
                            // تهيئة WebView
            InitializeWebView2(); // ← بدلاً من InitializeWebView
            LoadProjectsToDGV();
            navigationFrame1.TransitionAnimationProperties.FrameCount = 2;
          //  LoadClientsToDGV();
       //     LoadCompaniesToDGV();
          //  LoadDocumentTypesToDGV();
         //   LoadLanguagePairsToDGV();
        }
        private void LoadHtmlContent_WV2()
        {
            try
            {
                string htmlFilePath = Path.Combine(Application.StartupPath, "report.html");
                if (!File.Exists(htmlFilePath))
                    htmlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "report.html");

                if (File.Exists(htmlFilePath))
                {
                    // إمّا من ملف
                    webView.Source = new Uri(htmlFilePath);
                    // أو كسلسلة:
                    // webView.NavigateToString(File.ReadAllText(htmlFilePath));
                }
                else
                {
                    webView.NavigateToString("<h1>report.html غير موجود</h1>");
                }
            }
            catch (Exception ex)
            {
                webView?.NavigateToString($"<h1>خطأ</h1><p>{System.Net.WebUtility.HtmlEncode(ex.Message)}</p>");
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {


        }
        private async void InitializeWebView2()
        {
            webView = new WebView2();
            paneldoc.Controls.Add(webView);
            webView.Dock = DockStyle.Fill;

            // مجلد بيانات للمحرك
            string userData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Mospuk_WebView2");

            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userData);
            await webView.EnsureCoreWebView2Async(env);

            // إعدادات مفيدة
            webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

            // حمّل الصفحة
            LoadHtmlContent_WV2();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddFile addproject = new AddFile(db);
           addproject.FormClosed += (s, args) => LoadProjectsToDGV(); // تحديث عند إغلاق النموذج
           addproject.Show();
        }
        private void LoadProjectsToDGV()
        {
            string query = "SELECT folder_name FROM projects ORDER BY id DESC"; // تعرض آخر Project في الأعلى
            DataTable dt = db.ExecuteQuery(query, null);
            DGVChanges.DataSource = dt;
        }

        private void btnCompanyAdd_Click(object sender, EventArgs e)
        {

          //  OrcTest addproject = new OrcTest();
           // addproject.ShowDialog();
        }

        private void btnUserAdd_Click(object sender, EventArgs e)
        {
            navigationFrame1.SelectedPage = navigationPage1;
        }

        private void btnClientAdd_Click(object sender, EventArgs e)
        {
            LoadClientsToDGV();

            navigationFrame1.SelectedPage = navigationPage2;
        }

        private void labelControl11_Click(object sender, EventArgs e)
        {
        }

        private void btnAddClient_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtcode.Text))
            {
                MessageBox.Show("Client code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string firstName = txtfirstname.Text.Trim();
                string lastName = txtlastname.Text.Trim();
                string clientCode = txtcode.Text.Trim();
                string email = txtemail.Text.Trim();
                string phone = txtphone.Text.Trim();
                string address = txtaddress.Text.Trim();
                string notes = txtnotes.Text.Trim();

                // أولاً: تحقق هل client_code موجود مسبقاً
                string checkQuery = "SELECT COUNT(*) FROM clients WHERE client_code = @client_code";
                // تم التغيير
                var checkParams = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@client_code", clientCode)
                };

                object result = db.ExecuteScalar(checkQuery, checkParams);
                int count = Convert.ToInt32(result);

                if (count > 0)
                {
                    MessageBox.Show("Client code already exists. Please use a different client code.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // توقف العملية لأن الكود موجود مسبقاً
                }

                // إذا لم يكن موجود، قم بإضافة العميل
                string query = @"INSERT INTO clients 
            (first_name, last_name, client_code, email, phone, address, notes) 
            VALUES 
            (@first_name, @last_name, @client_code, @email, @phone, @address, @notes)";

                // تم التغيير
                var parameters = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@first_name", firstName),
                    new SQLiteParameter("@last_name", lastName),
                    new SQLiteParameter("@client_code", clientCode),
                    new SQLiteParameter("@email", email),
                    new SQLiteParameter("@phone", phone),
                    new SQLiteParameter("@address", address),
                    new SQLiteParameter("@notes", notes)
                };

                bool success = db.ExecuteNonQuery(query, parameters);

                if (success)
                {
                    MessageBox.Show("The client has been added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtfirstname.Clear();
                    txtlastname.Clear();
                    txtcode.Clear();
                    txtemail.Clear();
                    txtphone.Clear();
                    txtaddress.Clear();
                    txtnotes.Clear();
                    LoadClientsToDGV();
                }
                else
                {
                    MessageBox.Show("Failed to add the client.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadClientsToDGV()
        {
            string query = @"
        SELECT 
            client_id AS 'ID',
            first_name AS 'First Name',
            last_name AS 'Last Name',
            client_code AS 'Client Code',
            email AS 'Email',
            phone AS 'Phone',
            address AS 'Address',
            notes AS 'Notes'
        FROM clients 
        ORDER BY client_id DESC";

            DataTable dt = db.ExecuteQuery(query, null);
            DTGVClient.DataSource = dt;
            // Remove old column if it exists to avoid duplication
            if (DTGVClient.Columns.Contains("Delete"))
                DTGVClient.Columns.Remove("Delete");

            DataGridViewImageColumn imgCol = new DataGridViewImageColumn();
            imgCol.Name = "Delete";
            imgCol.HeaderText = "";
            imgCol.Image = Properties.Resources.delete_red; // 🛑 ضع هنا صورة delete في Resources
            imgCol.ImageLayout = DataGridViewImageCellLayout.Zoom;

            DTGVClient.Columns.Add(imgCol);
        }

        private void DTGVClient_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = DTGVClient.Rows[e.RowIndex];

                selectedClientId = Convert.ToInt32(row.Cells["ID"].Value);
                txtfirstname.Text = row.Cells["First Name"].Value.ToString();
                txtlastname.Text = row.Cells["Last Name"].Value.ToString();
                txtcode.Text = row.Cells["Client Code"].Value.ToString();
                txtemail.Text = row.Cells["Email"].Value.ToString();
                txtphone.Text = row.Cells["Phone"].Value.ToString();
                txtaddress.Text = row.Cells["Address"].Value.ToString();
                txtnotes.Text = row.Cells["Notes"].Value.ToString();
            }
        }

        private void btnEditClient_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtcode.Text))
            {
                MessageBox.Show("Client code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedClientId == -1)
            {
                MessageBox.Show("Please select a client to edit by double-clicking on it.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string firstName = txtfirstname.Text.Trim();
                string lastName = txtlastname.Text.Trim();
                string clientCode = txtcode.Text.Trim();
                string email = txtemail.Text.Trim();
                string phone = txtphone.Text.Trim();
                string address = txtaddress.Text.Trim();
                string notes = txtnotes.Text.Trim();

                // تحقق هل client_code موجود في عميل آخر غير الذي نعدله
                string checkQuery = "SELECT COUNT(*) FROM clients WHERE client_code = @client_code AND client_id != @client_id";
                // تم التغيير
                var checkParams = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@client_code", clientCode),
                    new SQLiteParameter("@client_id", selectedClientId)
                };

                object result = db.ExecuteScalar(checkQuery, checkParams);
                int count = Convert.ToInt32(result);

                if (count > 0)
                {
                    MessageBox.Show("Client code already exists for another client. Please use a different client code.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string query = @"UPDATE clients SET
                         first_name = @first_name,
                         last_name = @last_name,
                         client_code = @client_code,
                         email = @email,
                         phone = @phone,
                         address = @address,
                         notes = @notes
                         WHERE client_id = @client_id";

                // تم التغيير
                var parameters = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@first_name", firstName),
                    new SQLiteParameter("@last_name", lastName),
                    new SQLiteParameter("@client_code", clientCode),
                    new SQLiteParameter("@email", email),
                    new SQLiteParameter("@phone", phone),
                    new SQLiteParameter("@address", address),
                    new SQLiteParameter("@notes", notes),
                    new SQLiteParameter("@client_id", selectedClientId)
                };

                bool success = db.ExecuteNonQuery(query, parameters);

                if (success)
                {
                    MessageBox.Show("The client has been updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadClientsToDGV();

                    // إفراغ الحقول وإعادة تعيين selectedClientId
                    txtfirstname.Clear();
                    txtlastname.Clear();
                    txtcode.Clear();
                    txtemail.Clear();
                    txtphone.Clear();
                    txtaddress.Clear();
                    txtnotes.Clear();
                    selectedClientId = -1;
                }
                else
                {
                    MessageBox.Show("Failed to update the client.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DTGVClient_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && DTGVClient.Columns[e.ColumnIndex].Name == "Delete")
            {
                DialogResult result = MessageBox.Show("Are you sure you want to delete this client?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    int clientId = Convert.ToInt32(DTGVClient.Rows[e.RowIndex].Cells["ID"].Value);

                    string deleteQuery = "DELETE FROM clients WHERE client_id = @client_id";
                    // تم التغيير
                    var parameters = new List<SQLiteParameter>
                    {
                        new SQLiteParameter("@client_id", clientId)
                    };

                    bool success = db.ExecuteNonQuery(deleteQuery, parameters);

                    if (success)
                    {
                        MessageBox.Show("Client deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadClientsToDGV();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete the client.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void txtsearch_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtsearch.Text.Trim();

            string query = @"
        SELECT 
            client_id AS 'ID',
            first_name AS 'First Name',
            last_name AS 'Last Name',
            client_code AS 'Client Code',
            email AS 'Email',
            phone AS 'Phone',
            address AS 'Address',
            notes AS 'Notes'
        FROM clients
        WHERE 
            first_name LIKE @search OR
            last_name LIKE @search OR
            email LIKE @search OR
            client_code LIKE @search
        ORDER BY client_id DESC";

            // تم التغيير
            var parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@search", "%" + searchText + "%")
            };

            DataTable dt = db.ExecuteQuery(query, parameters);
            DTGVClient.DataSource = dt;

            // إعادة إضافة زر الحذف بعد كل بحث
            if (!DTGVClient.Columns.Contains("Delete"))
            {
                DataGridViewImageColumn imgCol = new DataGridViewImageColumn();
                imgCol.Name = "Delete";
                imgCol.HeaderText = "";
                imgCol.Image = Properties.Resources.delete_red;
                imgCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
                DTGVClient.Columns.Add(imgCol);
            }
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            LoadCompaniesToDGV();
            navigationFrame1.SelectedPage = navigationPage3;
        }

        private void LoadCompaniesToDGV()
        {
            string query = @"
        SELECT 
            company_id AS 'ID',
            company_name AS 'Company Name',
            company_code AS 'Company Code',
            tax_number AS 'Tax Number',
            address AS 'Address',
            phone AS 'Phone',
            email AS 'Email',
            notes AS 'Notes'
        FROM companies
        ORDER BY company_id DESC";

            DataTable dt = db.ExecuteQuery(query, null);
            DTGVCompany.DataSource = dt;

            if (DTGVCompany.Columns.Contains("Delete"))
                DTGVCompany.Columns.Remove("Delete");

            DataGridViewImageColumn imgCol = new DataGridViewImageColumn();
            imgCol.Name = "Delete";
            imgCol.HeaderText = "";
            imgCol.Image = Properties.Resources.delete_red;
            imgCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
            DTGVCompany.Columns.Add(imgCol);
        }

        private void btnEditCompany_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtcodeCompany.Text))
            {
                MessageBox.Show("Company code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedCompanyId == -1)
            {
                MessageBox.Show("Please select a company to edit.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string name = txtnamecompany.Text.Trim(); // تم التعديل
                string code = txtcodeCompany.Text.Trim();
                string tax = txtTax.Text.Trim();
                string address = txtaddresCompany.Text.Trim();
                string phone = txtNumberCompany.Text.Trim();
                string email = txtemaiCompany.Text.Trim();
                string notes = txtnoteCompany.Text.Trim();

                // تحقق هل الكود أو رقم الضريبة مستخدم في شركة أخرى
                string checkQuery = "SELECT COUNT(*) FROM companies WHERE (company_code = @code OR tax_number = @tax) AND company_id != @id";
                // تم التغيير
                var checkParams = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@code", code),
                    new SQLiteParameter("@tax", tax),
                    new SQLiteParameter("@id", selectedCompanyId)
                };

                int count = Convert.ToInt32(db.ExecuteScalar(checkQuery, checkParams));
                if (count > 0)
                {
                    MessageBox.Show("Company code or tax number already used by another company.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string query = @"UPDATE companies SET
                        company_name = @name,
                        company_code = @code,
                        tax_number = @tax,
                        address = @address,
                        phone = @phone,
                        email = @email,
                        notes = @notes
                        WHERE company_id = @id";

                // تم التغيير
                var parameters = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@name", name),
                    new SQLiteParameter("@code", code),
                    new SQLiteParameter("@tax", tax),
                    new SQLiteParameter("@address", address),
                    new SQLiteParameter("@phone", phone),
                    new SQLiteParameter("@email", email),
                    new SQLiteParameter("@notes", notes),
                    new SQLiteParameter("@id", selectedCompanyId)
                };

                bool success = db.ExecuteNonQuery(query, parameters);
                if (success)
                {
                    MessageBox.Show("Company updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadCompaniesToDGV();
                    ClearCompanyInputs();
                }
                else
                {
                    MessageBox.Show("Failed to update company.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddCompany_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtcodeCompany.Text))
            {
                MessageBox.Show("Company code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string name = txtnamecompany.Text.Trim();
                string code = txtcodeCompany.Text.Trim();
                string tax = txtTax.Text.Trim();
                string address = txtaddresCompany.Text.Trim();
                string phone = txtNumberCompany.Text.Trim();
                string email = txtemaiCompany.Text.Trim();
                string notes = txtnoteCompany.Text.Trim();

                // تحقق من وجود الكود فقط
                string checkQuery = "SELECT COUNT(*) FROM companies WHERE company_code = @code";
                // تم التغيير
                List<SQLiteParameter> checkParams = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@code", code)
                };

                // إذا كان tax غير فارغ، أضف شرط التحقق منه أيضًا
                if (!string.IsNullOrWhiteSpace(tax))
                {
                    checkQuery += " OR tax_number = @tax";
                    checkParams.Add(new SQLiteParameter("@tax", tax));
                }

                int count = Convert.ToInt32(db.ExecuteScalar(checkQuery, checkParams));
                if (count > 0)
                {
                    MessageBox.Show("Company code or tax number already exists.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string query = @"INSERT INTO companies 
        (company_name, company_code, tax_number, address, phone, email, notes) 
        VALUES 
        (@name, @code, @tax, @address, @phone, @email, @notes)";

                // تم التغيير
                var parameters = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@name", name),
                    new SQLiteParameter("@code", code),
                    new SQLiteParameter("@tax", string.IsNullOrWhiteSpace(tax) ? (object)DBNull.Value : tax),
                    new SQLiteParameter("@address", address),
                    new SQLiteParameter("@phone", phone),
                    new SQLiteParameter("@email", email),
                    new SQLiteParameter("@notes", notes)
                };

                bool success = db.ExecuteNonQuery(query, parameters);
                if (success)
                {
                    MessageBox.Show("Company added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadCompaniesToDGV();
                    ClearCompanyInputs();
                }
                else
                {
                    MessageBox.Show("Failed to add company.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ClearCompanyInputs()
        {
            txtnamecompany.Clear();
            txtcodeCompany.Clear();
            txtTax.Clear();
            txtaddresCompany.Clear();
            txtNumberCompany.Clear();
            txtemaiCompany.Clear();
            txtnoteCompany.Clear();
            selectedCompanyId = -1;
        }

        private void DTGVCompany_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = DTGVCompany.Rows[e.RowIndex];

                selectedCompanyId = Convert.ToInt32(row.Cells["ID"].Value);
                txtnamecompany.Text = row.Cells["Company Name"].Value.ToString();
                txtcodeCompany.Text = row.Cells["Company Code"].Value.ToString();
                txtTax.Text = row.Cells["Tax Number"].Value.ToString();
                txtemaiCompany.Text = row.Cells["Email"].Value.ToString();
                txtNumberCompany.Text = row.Cells["Phone"].Value.ToString();
                txtaddresCompany.Text = row.Cells["Address"].Value.ToString();
                txtnoteCompany.Text = row.Cells["Notes"].Value.ToString();
            }
        }

        private void DTGVCompany_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && DTGVCompany.Columns[e.ColumnIndex].Name == "Delete")
            {
                DialogResult result = MessageBox.Show("Are you sure you want to delete this company?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    int companyId = Convert.ToInt32(DTGVCompany.Rows[e.RowIndex].Cells["ID"].Value);

                    string deleteQuery = "DELETE FROM companies WHERE company_id = @company_id";
                    // تم التغيير
                    var parameters = new List<SQLiteParameter>
                    {
                        new SQLiteParameter("@company_id", companyId)
                    };

                    bool success = db.ExecuteNonQuery(deleteQuery, parameters);

                    if (success)
                    {
                        MessageBox.Show("Company deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadCompaniesToDGV();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete the company.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void txtsearchCompany_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtsearchCompany.Text.Trim();

            string query = @"
        SELECT 
            company_id AS 'ID',
            company_name AS 'Company Name',
            company_code AS 'Company Code',
            tax_number AS 'Tax Number',
            email AS 'Email',
            phone AS 'Phone',
            address AS 'Address',
            notes AS 'Notes'
        FROM companies
        WHERE 
            company_name LIKE @search OR
            company_code LIKE @search OR
            tax_number LIKE @search OR
            email LIKE @search
        ORDER BY company_id DESC";

            // تم التغيير
            var parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@search", "%" + searchText + "%")
            };

            DataTable dt = db.ExecuteQuery(query, parameters);
            DTGVCompany.DataSource = dt;

            // إعادة إضافة زر الحذف بعد كل بحث
            if (!DTGVCompany.Columns.Contains("Delete"))
            {
                DataGridViewImageColumn imgCol = new DataGridViewImageColumn();
                imgCol.Name = "Delete";
                imgCol.HeaderText = "";
                imgCol.Image = Properties.Resources.delete_red;
                imgCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
                DTGVCompany.Columns.Add(imgCol);
            }
        }

        private void DGVChanges_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
              LoadDocumentTypesToDGV();
               LoadLanguagePairsToDGV();
            navigationFrame1.SelectedPage = navigationPage4;
        }

        private void add_document_types_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txt_document_types.Text))
                {
                    MessageBox.Show("Please enter a document type", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string documentTypeName = txt_document_types.Text.Trim();

                string checkQuery = "SELECT COUNT(*) FROM document_types WHERE name = @name";
                // تم التغيير
                var checkParams = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@name", documentTypeName)
                };

                int count = Convert.ToInt32(db.ExecuteScalar(checkQuery, checkParams));

                if (count > 0)
                {
                    MessageBox.Show("This document type already exists!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string insertQuery = "INSERT INTO document_types (name) VALUES (@name)";
                // تم التغيير
                var insertParams = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@name", documentTypeName)
                };

                bool success = db.ExecuteNonQuery(insertQuery, insertParams);

                if (success)
                {
                    MessageBox.Show("Document type added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txt_document_types.Clear();
                    LoadDocumentTypesToDGV();
                }
                else
                {
                    MessageBox.Show("Failed to add document type", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadDocumentTypesToDGV()
        {
            string query = @"
    SELECT 
        id AS 'ID',
        name AS 'Document Type'
        FROM document_types
    ORDER BY id DESC";

            DataTable dt = db.ExecuteQuery(query, null);
            DTGVdocument.DataSource = dt;

            if (DTGVdocument.Columns.Contains("Delete"))
                DTGVdocument.Columns.Remove("Delete");

            DataGridViewImageColumn imgCol = new DataGridViewImageColumn();
            imgCol.Name = "Delete";
            imgCol.HeaderText = "";
            imgCol.Image = Properties.Resources.delete_red;
            imgCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
            DTGVdocument.Columns.Add(imgCol);
        }

        private void DTGVdocument_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && DTGVdocument.Columns[e.ColumnIndex].Name == "Delete")
            {
                DialogResult result = MessageBox.Show("Are you sure you want to delete this document type?",
                                                   "Confirm Delete",
                                                   MessageBoxButtons.YesNo,
                                                   MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    int documentTypeId = Convert.ToInt32(DTGVdocument.Rows[e.RowIndex].Cells["ID"].Value);

                    string deleteQuery = "DELETE FROM document_types WHERE id = @id";
                    // تم التغيير
                    var parameters = new List<SQLiteParameter>
                    {
                        new SQLiteParameter("@id", documentTypeId)
                    };

                    bool success = db.ExecuteNonQuery(deleteQuery, parameters);

                    if (success)
                    {
                        MessageBox.Show("Document type deleted successfully.", "Success",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadDocumentTypesToDGV();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete document type.", "Error",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void txtsearchdocument_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtsearchdocument.Text.Trim();

            string query = @"
    SELECT 
        id AS 'ID',
        name AS 'Document Type'
    FROM document_types
    WHERE 
        name LIKE @search
    ORDER BY id DESC";

            // تم التغيير
            var parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@search", "%" + searchText + "%")
            };

            DataTable dt = db.ExecuteQuery(query, parameters);
            DTGVdocument.DataSource = dt;

            if (!DTGVdocument.Columns.Contains("Delete"))
            {
                DataGridViewImageColumn imgCol = new DataGridViewImageColumn();
                imgCol.Name = "Delete";
                imgCol.HeaderText = "";
                imgCol.Image = Properties.Resources.delete_red;
                imgCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
                DTGVdocument.Columns.Add(imgCol);
            }
        }

        private void add_Language_Pair_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txt_Language_Pair.Text))
                {
                    MessageBox.Show("Please enter a language pair", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string languagePairName = txt_Language_Pair.Text.Trim();

                string checkQuery = "SELECT COUNT(*) FROM language_pairs WHERE name = @name";
                // تم التغيير
                var checkParams = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@name", languagePairName)
                };

                int count = Convert.ToInt32(db.ExecuteScalar(checkQuery, checkParams));

                if (count > 0)
                {
                    MessageBox.Show("This language pair already exists!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string insertQuery = "INSERT INTO language_pairs (name) VALUES (@name)";
                // تم التغيير
                var insertParams = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@name", languagePairName)
                };

                bool success = db.ExecuteNonQuery(insertQuery, insertParams);

                if (success)
                {
                    MessageBox.Show("Language pair added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txt_Language_Pair.Clear();
                    LoadLanguagePairsToDGV();
                }
                else
                {
                    MessageBox.Show("Failed to add language pair", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLanguagePairsToDGV()
        {
            string query = @"
    SELECT 
        id AS 'ID',
        name AS 'Language Pair'
        FROM language_pairs
    ORDER BY id DESC";

            DataTable dt = db.ExecuteQuery(query, null);
            DTGVLanguage.DataSource = dt;

            if (DTGVLanguage.Columns.Contains("Delete"))
                DTGVLanguage.Columns.Remove("Delete");

            DataGridViewImageColumn imgCol = new DataGridViewImageColumn();
            imgCol.Name = "Delete";
            imgCol.HeaderText = "";
            imgCol.Image = Properties.Resources.delete_red;
            imgCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
            DTGVLanguage.Columns.Add(imgCol);
        }

        private void txtsearchLanguage_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtsearchLanguage.Text.Trim();

            string query = @"
    SELECT 
        id AS 'ID',
        name AS 'Language Pair'        
    FROM language_pairs
    WHERE 
        name LIKE @search
    ORDER BY id DESC";

            // تم التغيير
            var parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@search", "%" + searchText + "%")
            };

            DataTable dt = db.ExecuteQuery(query, parameters);
            DTGVLanguage.DataSource = dt;

            if (!DTGVLanguage.Columns.Contains("Delete"))
            {
                DataGridViewImageColumn imgCol = new DataGridViewImageColumn();
                imgCol.Name = "Delete";
                imgCol.HeaderText = "";
                imgCol.Image = Properties.Resources.delete_red;
                imgCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
                DTGVLanguage.Columns.Add(imgCol);
            }
        }

        private void DTGVLanguage_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && DTGVLanguage.Columns[e.ColumnIndex].Name == "Delete")
            {
                DialogResult result = MessageBox.Show("Are you sure you want to delete this language pair?",
                                                   "Confirm Delete",
                                                   MessageBoxButtons.YesNo,
                                                   MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    int languagePairId = Convert.ToInt32(DTGVLanguage.Rows[e.RowIndex].Cells["ID"].Value);

                    string deleteQuery = "DELETE FROM language_pairs WHERE id = @id";
                    // تم التغيير
                    var parameters = new List<SQLiteParameter>
                    {
                        new SQLiteParameter("@id", languagePairId)
                    };

                    bool success = db.ExecuteNonQuery(deleteQuery, parameters);

                    if (success)
                    {
                        MessageBox.Show("Language pair deleted successfully.", "Success",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadLanguagePairsToDGV();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete language pair.", "Error",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnDirectory_Click(object sender, EventArgs e)
        {
            SaveDirectory saveDirectory = new SaveDirectory(db);
            saveDirectory.ShowDialog();
        }

        private void Home_Load(object sender, EventArgs e)
        {
         //   LoadProjectsToDGV();
        }
       

        private void tabPane1_Click(object sender, EventArgs e)
        {

        }

        private void btnUserAdd_Click_1(object sender, EventArgs e)
        {
            LoadUsersToDGV();
            navigationFrame1.SelectedPage = navigationPage6;

        }

        private void panel7_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tabNavParametres_Paint(object sender, PaintEventArgs e)
        {

        }

        private void navigationPage4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnAddUser_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtcodeuser.Text))
            {
                MessageBox.Show("User code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string firstName = txtfirstnameuser.Text.Trim();
                string lastName = txtlastnameuser.Text.Trim();
                string userCode = txtcodeuser.Text.Trim();
                string email = txtemailuser.Text.Trim();
                string phone = txtphoneuser.Text.Trim();
                string address = txtaddressuser.Text.Trim();
                string notes = txtnotesuser.Text.Trim();

                // أولاً: تحقق هل client_code موجود مسبقاً
                string checkQuery = "SELECT COUNT(*) FROM users  WHERE user_code  = @user_code";
                // تم التغيير
                var checkParams = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@user_code", userCode)
                };

                object result = db.ExecuteScalar(checkQuery, checkParams);
                int count = Convert.ToInt32(result);

                if (count > 0)
                {
                    MessageBox.Show("User code already exists. Please use a different user code.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // توقف العملية لأن الكود موجود مسبقاً
                }

                // إذا لم يكن موجود، قم بإضافة العميل
                string query = @"INSERT INTO users  
            (first_name, last_name, user_code, email, phone, address, notes) 
            VALUES 
            (@first_name, @last_name, @user_code, @email, @phone, @address, @notes)";

                // تم التغيير
                var parameters = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@first_name", firstName),
                    new SQLiteParameter("@last_name", lastName),
                    new SQLiteParameter("@user_code", userCode),
                    new SQLiteParameter("@email", email),
                    new SQLiteParameter("@phone", phone),
                    new SQLiteParameter("@address", address),
                    new SQLiteParameter("@notes", notes)
                };

                bool success = db.ExecuteNonQuery(query, parameters);

                if (success)
                {
                    MessageBox.Show("The user has been added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtfirstnameuser.Clear();
                    txtlastnameuser.Clear();
                    txtcodeuser.Clear();
                    txtemailuser.Clear();
                    txtphoneuser.Clear();
                    txtaddressuser.Clear();
                    txtnotesuser.Clear();
                    LoadUsersToDGV();
                }
                else
                {
                    MessageBox.Show("Failed to add the user.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private void LoadUsersToDGV()
        {
            string query = @"
        SELECT 
            user_id  AS 'ID',
            first_name AS 'First Name',
            last_name AS 'Last Name',
            user_code AS 'User Code',
            email AS 'Email',
            phone AS 'Phone',
            address AS 'Address',
            notes AS 'Notes'
        FROM users 
        ORDER BY user_id DESC";

            DataTable dt = db.ExecuteQuery(query, null);
            DTGVUser.DataSource = dt;
            // Remove old column if it exists to avoid duplication
            if (DTGVUser.Columns.Contains("Delete"))
                DTGVUser.Columns.Remove("Delete");

            DataGridViewImageColumn imgCol = new DataGridViewImageColumn();
            imgCol.Name = "Delete";
            imgCol.HeaderText = "";
            imgCol.Image = Properties.Resources.delete_red; // 🛑 ضع هنا صورة delete في Resources
            imgCol.ImageLayout = DataGridViewImageCellLayout.Zoom;

            DTGVUser.Columns.Add(imgCol);
        }

        private void DTGVUser_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = DTGVUser.Rows[e.RowIndex];

                selectedClientId = Convert.ToInt32(row.Cells["ID"].Value);
                txtfirstname.Text = row.Cells["First Name"].Value.ToString();
                txtlastname.Text = row.Cells["Last Name"].Value.ToString();
                txtcode.Text = row.Cells["User Code"].Value.ToString();
                txtemail.Text = row.Cells["Email"].Value.ToString();
                txtphone.Text = row.Cells["Phone"].Value.ToString();
                txtaddress.Text = row.Cells["Address"].Value.ToString();
                txtnotes.Text = row.Cells["Notes"].Value.ToString();
            }
        }

        private void txtsearchuser_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtsearchuser.Text.Trim();

            string query = @"
        SELECT 
            user_id  AS 'ID',
            first_name AS 'First Name',
            last_name AS 'Last Name',
            user_code AS 'User Code',
            email AS 'Email',
            phone AS 'Phone',
            address AS 'Address',
            notes AS 'Notes'
        FROM users
        WHERE 
            first_name LIKE @search OR
            last_name LIKE @search OR
            email LIKE @search OR
            user_code LIKE @search
        ORDER BY user_id  DESC";

            // تم التغيير
            var parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@search", "%" + searchText + "%")
            };

            DataTable dt = db.ExecuteQuery(query, parameters);
            DTGVUser.DataSource = dt;

            // إعادة إضافة زر الحذف بعد كل بحث
            if (!DTGVUser.Columns.Contains("Delete"))
            {
                DataGridViewImageColumn imgCol = new DataGridViewImageColumn();
                imgCol.Name = "Delete";
                imgCol.HeaderText = "";
                imgCol.Image = Properties.Resources.delete_red;
                imgCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
                DTGVUser.Columns.Add(imgCol);
            }
        }

        private void btnEditUser_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtcodeuser.Text))
            {
                MessageBox.Show("user code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedUserId == -1)
            {
                MessageBox.Show("Please select a user to edit by double-clicking on it.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string firstName = txtfirstnameuser.Text.Trim();
                string lastName = txtlastnameuser.Text.Trim();
                string userCode = txtcodeuser.Text.Trim();
                string email = txtemailuser.Text.Trim();
                string phone = txtphoneuser.Text.Trim();
                string address = txtaddressuser.Text.Trim();
                string notes = txtnotesuser.Text.Trim();      

                string checkQuery = "SELECT COUNT(*) FROM users WHERE user_code = @user_code AND user_id  != @user_id ";
                // تم التغيير
                var checkParams = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@user_code", userCode),
                    new SQLiteParameter("@user_id", selectedUserId)
                };

                object result = db.ExecuteScalar(checkQuery, checkParams);
                int count = Convert.ToInt32(result);

                if (count > 0)
                {
                    MessageBox.Show("user code already exists for another user. Please use a different user code.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string query = @"UPDATE users SET
                         first_name = @first_name,
                         last_name = @last_name,
                         user_code = @user_code,
                         email = @email,
                         phone = @phone,
                         address = @address,
                         notes = @notes
                         WHERE user_id = @user_id";

                // تم التغيير
                var parameters = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@first_name", firstName),
                    new SQLiteParameter("@last_name", lastName),
                    new SQLiteParameter("@user_code", userCode),
                    new SQLiteParameter("@email", email),
                    new SQLiteParameter("@phone", phone),
                    new SQLiteParameter("@address", address),
                    new SQLiteParameter("@notes", notes),
                    new SQLiteParameter("@user_id", selectedUserId)
                };

                bool success = db.ExecuteNonQuery(query, parameters);

                if (success)
                {
                    MessageBox.Show("The user has been updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadUsersToDGV();

                    // إفراغ الحقول وإعادة تعيين selectedClientId
                    txtfirstnameuser.Clear();
                    txtlastnameuser.Clear();
                    txtcodeuser.Clear();
                    txtemailuser.Clear();
                    txtphoneuser.Clear();
                    txtaddressuser.Clear();
                    txtnotesuser.Clear();
                    selectedUserId = -1;
                }
                else
                {
                    MessageBox.Show("Failed to update the client.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DTGVUser_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && DTGVUser.Columns[e.ColumnIndex].Name == "Delete")
            {
                DialogResult result = MessageBox.Show(
                    "Are you sure you want to delete this user?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    int userId = Convert.ToInt32(DTGVUser.Rows[e.RowIndex].Cells["ID"].Value);

                    string deleteQuery = "DELETE FROM users WHERE user_id = @user_id";
                    var parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@user_id", userId)
            };

                    bool success = db.ExecuteNonQuery(deleteQuery, parameters);

                    if (success)
                    {
                        MessageBox.Show("User deleted successfully.", "Deleted",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadUsersToDGV();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete the user.", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void paneldoc_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
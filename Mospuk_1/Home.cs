using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
    
namespace Mospuk_1
{
    public partial class Home : Form
    {
        MySqlDatabase db;
        private int selectedClientId = -1;


        public Home(MySqlDatabase database)
        {
            InitializeComponent();
            db = database;  // ← هنا تحفظ المتغير لتستخدمه لاحقاً
            LoadProjectsToDGV();
            navigationFrame1.TransitionAnimationProperties.FrameCount = 1;
            LoadClientsToDGV();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddProject addproject = new AddProject(db);

            addproject.ShowDialog();
            LoadProjectsToDGV();
        }
        private void LoadProjectsToDGV()
        {
            string query = "SELECT folder_name FROM projects ORDER BY id DESC"; // تعرض آخر Project في الأعلى
            DataTable dt = db.ExecuteQuery(query, null);
            DGVChanges.DataSource = dt;
        }

        private void btnCompanyAdd_Click(object sender, EventArgs e)
        {
            OrcTest addproject = new OrcTest();

            addproject.ShowDialog();
        }

        private void btnUserAdd_Click(object sender, EventArgs e)
        {
            navigationFrame1.SelectedPage = navigationPage1;

        }

        private void btnClientAdd_Click(object sender, EventArgs e)
        {
            navigationFrame1.SelectedPage = navigationPage2;

        }

        private void labelControl11_Click(object sender, EventArgs e)
        {

        }

        private void btnAddClient_Click(object sender, EventArgs e)
        {
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
                var checkParams = new List<MySqlParameter>
        {
            new MySqlParameter("@client_code", clientCode)
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

                var parameters = new List<MySqlParameter>
        {
            new MySqlParameter("@first_name", firstName),
            new MySqlParameter("@last_name", lastName),
            new MySqlParameter("@client_code", clientCode),
            new MySqlParameter("@email", email),
            new MySqlParameter("@phone", phone),
            new MySqlParameter("@address", address),
            new MySqlParameter("@notes", notes)
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
            imgCol.HeaderText = "Delete";
            imgCol.Image = Properties.Resources.delete_red; // 🛑 ضع هنا صورة delete في Resources
            imgCol.Width = 40;
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
                var checkParams = new List<MySqlParameter>
        {
            new MySqlParameter("@client_code", clientCode),
            new MySqlParameter("@client_id", selectedClientId)
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

                var parameters = new List<MySqlParameter>
        {
            new MySqlParameter("@first_name", firstName),
            new MySqlParameter("@last_name", lastName),
            new MySqlParameter("@client_code", clientCode),
            new MySqlParameter("@email", email),
            new MySqlParameter("@phone", phone),
            new MySqlParameter("@address", address),
            new MySqlParameter("@notes", notes),
            new MySqlParameter("@client_id", selectedClientId)
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
    }
}

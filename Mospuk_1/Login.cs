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

    public partial class Login : Form
    {  
        MySqlDatabase db;

        public Login(MySqlDatabase database)
        {
            InitializeComponent();
            db = database;
        }

        private void Login_Load(object sender, EventArgs e)
        {
            Switch.Checked = true;
        }

        private void Switch_CheckedChanged(object sender, EventArgs e)
        {
            if (Switch.Checked == false)
            {
                PasT.UseSystemPasswordChar = false;
            }
            else
            {
                PasT.UseSystemPasswordChar = true;
            }
        }

        private void LoginB_Click(object sender, EventArgs e)
        {
            string username = UsertT.Text.Trim();
            string password = PasT.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please fill in all fields", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // SQL query
            string query = "SELECT * FROM tb_user WHERE username = @username AND password = @password";

            // Set query parameters
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@username", username),
                new MySqlParameter("@password", password) // 🔐 Ideally, use hashed passwords in production
            };

            DataTable result = db.ExecuteQuery(query, parameters);

            if (result.Rows.Count > 0)
            {
                // MessageBox.Show("Login successful", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Open the main form or hide the login form
                this.Hide();
                Home home = new Home(db);
                home.Show();
            }
            else
            {
                MessageBox.Show("Invalid username or password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    

    }
}

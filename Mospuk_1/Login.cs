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
using System.IO;

namespace Mospuk_1
{

    public partial class Login : Form
    {
        SQLiteDatabase db; // تم التغيير

        public Login(SQLiteDatabase database)
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

      /*  private void LoginB_Click(object sender, EventArgs e)
        {
            string username = UsertT.Text.Trim();
            string password = PasT.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // استعلام لجلب بيانات المستخدم بما فيها الـ ID
            string query = "SELECT id FROM users WHERE username = @username AND password = @password";

            var parameters = new List<MySqlParameter>
    {
        new MySqlParameter("@username", username),
        new MySqlParameter("@password", password)
    };

            object result = db.ExecuteScalar(query, parameters);

            if (result != null && int.TryParse(result.ToString(), out int userId))
            {
                // ✅ حفظ userId في session.txt
                File.WriteAllText("session.txt", userId.ToString());

                this.Hide();
                Home home = new Home(db); // نمرر ID المستخدم للنموذج الرئيسي
                home.Show();
            }
            else
            {
                MessageBox.Show("Incorrect username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }*/
    }
}

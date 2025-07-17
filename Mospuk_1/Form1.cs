using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Mospuk_1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

       
       

        private void SaveConnectionSettings(string server, string database, string username, string password, string port = "3306")
        {
            // Encrypt the password
            string encryptedPassword = EncryptionHelper.EncryptPassword(password);

            XElement xmlSettings = new XElement("Settings",
                new XElement("Server", server),
                new XElement("Port", port),
                new XElement("Database", database),
                new XElement("Username", username),
                new XElement("Password", encryptedPassword)
            );

            xmlSettings.Save("connection_settings.xml");
        }

        private void txtboxPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnConnect.PerformClick();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Optional: code to run on load
        }

        private void txtboxPassword_TextChanged(object sender, EventArgs e)
        {
            // Optional: code on password change
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtboxDatabase.Text) || string.IsNullOrEmpty(txtboxServer.Text) || string.IsNullOrEmpty(txtboxUsername.Text))
            {
                MessageBox.Show("Veuillez remplir tous les champs", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SaveConnectionSettings(txtboxServer.Text, txtboxDatabase.Text, txtboxUsername.Text, txtboxPassword.Text, txtboxPort.Text);

            MySqlDatabase.Initialize(txtboxServer.Text, txtboxPort.Text, txtboxDatabase.Text, txtboxUsername.Text, txtboxPassword.Text);
            this.DialogResult = DialogResult.OK;
            this.Hide();
        }

        private void swtchPassword_CheckedChanged(object sender, EventArgs e)
        {
            if (swtchPassword.Checked)
            {
                txtboxPassword.UseSystemPasswordChar = false;
            }
            else
            {
                txtboxPassword.UseSystemPasswordChar = true;
            }
        }
    }
}

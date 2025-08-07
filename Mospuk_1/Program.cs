using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Mospuk_1
{
    public class Program
    {
        private MySqlDatabase db;

        [STAThread]
        public static void Main()
        {
            string server = "";
            string port = "";
            string database = "";
            string username = "";
            string password = "";

            try
            {
                if (File.Exists("connection_settings.xml"))
                {
                    // Load settings from XML
                    XElement xmlSettings = XElement.Load("connection_settings.xml");
                    server = xmlSettings.Element("Server")?.Value;
                    port = xmlSettings.Element("Port")?.Value;
                    database = xmlSettings.Element("Database")?.Value;
                    username = xmlSettings.Element("Username")?.Value;
                    string encryptedPassword = xmlSettings.Element("Password")?.Value;

                    password = EncryptionHelper.DecryptPassword(encryptedPassword);
                }
                else
                {
                    // Show form to select server and database
                    var selectServerForm = new Form1(); // <-- تأكد هذا الفورم موجود عندك
                    if (selectServerForm.ShowDialog() == DialogResult.OK)
                    {
                        server = MySqlDatabase.Server;
                        port = MySqlDatabase.Port;
                        database = MySqlDatabase.Database;
                        username = MySqlDatabase.Username;
                        password = MySqlDatabase.Password;

                        string encryptedPassword = EncryptionHelper.EncryptPassword(password);
                        SaveConnectionSettings(server, port, database, username, encryptedPassword);
                    }
                    else
                    {
                        return; // Exit if canceled
                    }
                } 

                // Initialize the DB connection
                MySqlDatabase.Initialize(server, port, database, username, password);

                Program app = new Program();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                if (File.Exists("session.txt"))
                {
                    // قرينا userId من الملف
                    string userIdStr = File.ReadAllText("session.txt");
                    if (int.TryParse(userIdStr, out int userId))
                    {
                        Application.Run(new Home(app.db, userId));
                        return;
                    }
                }

                // إذا ما كيناش session نشوفو Login
                Application.Run(new Login(app.db));
                        }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'initialisation: " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public Program()
        {
            db = new MySqlDatabase();
        }

        private static void SaveConnectionSettings(string server, string port, string database, string username, string encryptedPassword)
        {
            XElement xmlSettings = new XElement("Settings",
                new XElement("Server", server),
                new XElement("Port", port),
                new XElement("Database", database),
                new XElement("Username", username),
                new XElement("Password", encryptedPassword)
            );
            xmlSettings.Save("connection_settings.xml");
        }
    }
}

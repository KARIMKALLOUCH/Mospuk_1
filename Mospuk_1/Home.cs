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

        public Home(MySqlDatabase database)
        {
            InitializeComponent();
            db = database;  // ← هنا تحفظ المتغير لتستخدمه لاحقاً
            LoadProjectsToDGV();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddProject addproject = new AddProject(db);

            addproject.ShowDialog();

        }
        private void LoadProjectsToDGV()
        {
            string query = "SELECT folder_name FROM projects ORDER BY id DESC"; // تعرض آخر Project في الأعلى
            DataTable dt = db.ExecuteQuery(query, null);
            DGVChanges.DataSource = dt;
        }

    }
}

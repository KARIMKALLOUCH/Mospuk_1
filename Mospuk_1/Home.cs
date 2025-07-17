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
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddProject addproject = new AddProject();

            addproject.ShowDialog();

        }
    }
}

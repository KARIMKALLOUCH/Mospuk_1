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
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void btnGeneral_Click(object sender, EventArgs e)
        {
            navigationFrame2.SelectedPage = navigationPageGenral;

        }

        private void btnAddclientS_Click(object sender, EventArgs e)
        {
            navigationFrame2.SelectedPage = navigationPageclient;

        }

        private void btnaddcompanyS_Click(object sender, EventArgs e)
        {
            navigationFrame2.SelectedPage = navigationPagecompany;

        }

        private void btnadddocument_Lang_Click(object sender, EventArgs e)
        {
            navigationFrame2.SelectedPage = navigationPageDocument;

        }
    }
}

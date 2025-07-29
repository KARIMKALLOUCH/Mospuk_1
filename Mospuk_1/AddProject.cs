using MySql.Data.MySqlClient;
using Mysqlx.Crud;
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

    public partial class AddProject : Form
    {
        private int _projectId;

        MySqlDatabase db;
        public AddProject()
        {
            InitializeComponent();
         //   db = database;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.Width = 200;
            this.Height = 100;

            Label lbl = new Label();
            lbl.Text = "⏳ جاري تحويل PDF...";
            lbl.Font = new Font("Arial", 10, FontStyle.Bold);
            lbl.AutoSize = false;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.Dock = DockStyle.Fill;
            this.Controls.Add(lbl);

            this.TopMost = true;
            this.ShowInTaskbar = false;


        }

        private void guna2DateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void AddProject_Load(object sender, EventArgs e)
        {
          
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
        }

        
    }
    }

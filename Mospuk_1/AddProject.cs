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

    public partial class AddProject : Form
    {
        MySqlDatabase db;
        public AddProject(MySqlDatabase database)
        {
            InitializeComponent();
            db = database;

        }

        private void guna2DateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void AddProject_Load(object sender, EventArgs e)
        {
            Delivery_Date.Items.Add(new KeyValuePair<string, int>("Default (3 days)", 3));
            Delivery_Date.Items.Add(new KeyValuePair<string, int>("Urgent (2 days)", 2));
            Delivery_Date.Items.Add(new KeyValuePair<string, int>("Very Urgent (1 day)", 1));

            Delivery_Date.DisplayMember = "Key";
            Delivery_Date.ValueMember = "Value";
            Delivery_Date.SelectedIndex = 0;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string companyClient = Company_Client.Text.Trim();
            DateTime receptionDate = Reception_Date.Value.Date;
            string receptionTime = Time.Text.Trim();
            int deliveryDays = ((KeyValuePair<string, int>)Delivery_Date.SelectedItem).Value;
            DateTime deliveryDate = receptionDate.AddDays(deliveryDays);

            // جلب آخر رقم المشروع في ذلك اليوم من قاعدة البيانات
            string orderQuery = "SELECT IFNULL(MAX(project_order), 0) FROM projects WHERE reception_date = @date";
            object result = db.ExecuteScalar(orderQuery, new List<MySqlParameter>
{
    new MySqlParameter("@date", receptionDate.ToString("yyyy-MM-dd"))
});

            int lastOrder = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
            int newOrder = lastOrder + 1;

            // تحضير باقي المعطيات (ثابت)
            string hoursSpent = "24";

            // توليد اسم المجلد حسب التنسيق المطلوب
            string deliveryDateStr = deliveryDate.ToString("yyyyMMdd");
            string receptionDateStr = receptionDate.ToString("yyMMdd");
            string projectOrderStr = newOrder.ToString("D2");
            string receptionTimeStr = receptionTime.Replace(":", ""); // إزالة النقط إذا وجدت
            string folderName = $"{deliveryDateStr}{hoursSpent}_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}";

            // إدخال المشروع الجديد إلى MySQL
            string insertQuery = @"INSERT INTO projects 
    (company_client, reception_date, reception_time, delivery_days, delivery_date, hours_spent, project_order, folder_name) 
    VALUES (@company_client, @reception_date, @reception_time, @delivery_days, @delivery_date, @hours_spent, @project_order, @folder_name)";

            List<MySqlParameter> parameters = new List<MySqlParameter> 
{
    new MySqlParameter("@company_client", companyClient),
    new MySqlParameter("@reception_date", receptionDate.ToString("yyyy-MM-dd")),
    new MySqlParameter("@reception_time", receptionTime),
    new MySqlParameter("@delivery_days", deliveryDays),
    new MySqlParameter("@delivery_date", deliveryDate.ToString("yyyy-MM-dd")),
    new MySqlParameter("@hours_spent", 24),
    new MySqlParameter("@project_order", newOrder),
    new MySqlParameter("@folder_name", folderName)
};

            bool success = db.ExecuteNonQuery(insertQuery, parameters);
            if (success)
            {
                MessageBox.Show("✅ Project inserted successfully");
                AddFile addProject = new AddFile();
                addProject.ShowDialog();

            }
            else
            {
                MessageBox.Show("❌ Error inserting project.");
            }
        }

        
    }
    }

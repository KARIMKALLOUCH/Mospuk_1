using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Mospuk_1
{
    public class Program
    {
        private SQLiteDatabase db;

        [STAThread]
        public static void Main()
        {
            try
            {
                // إنشاء كائن التطبيق
                Program app = new Program();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // افتح نافذة Home مباشرة (يمكنك تمرير قيمة افتراضية لـ userId أو 0 إذا لم يكن مطلوباً)
                Application.Run(new AddFile(app.db));
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تشغيل التطبيق: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public Program()
        {
            try
            {
                // إنشاء قاعدة البيانات - ستُنشأ تلقائياً في مجلد Data
                db = new SQLiteDatabase("mospuk_database.db");
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في إنشاء قاعدة البيانات: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
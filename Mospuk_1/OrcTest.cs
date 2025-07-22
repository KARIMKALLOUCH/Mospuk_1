using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using Xceed.Words.NET;

namespace Mospuk_1
{

    public partial class OrcTest : Form
    {
        string imagePath = "";
        string tessDataPath = @"C:\abdelkarim\Mospuk_1\tessdata"; // بدلها بمسارك الحقيقي

        public OrcTest()
        {
            InitializeComponent();
        }

        private void btnLoadImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                imagePath = ofd.FileName;
                pictureBox1.Image = Image.FromFile(imagePath);
            }
        }

        private void btnExtractText_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                MessageBox.Show("المرجو اختيار صورة أولا");
                return;
            }

            try
            {
                using (var engine = new TesseractEngine(tessDataPath, "eng+ara", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(imagePath))
                    {
                        using (var page = engine.Process(img))
                        {
                            string text = page.GetText();
                            textBox1.Text = text;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void btnSaveWord_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Word Document|*.docx";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var doc = DocX.Create(sfd.FileName);
                doc.InsertParagraph(textBox1.Text);
                doc.Save();

                MessageBox.Show("✅ ");
            }
        }
    }
}

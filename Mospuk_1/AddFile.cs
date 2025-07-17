using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;

using System.Threading.Tasks;
using System.Windows.Forms;
using PdfiumViewer;

namespace Mospuk_1
{
    public partial class AddFile : Form

    {
        private Point dragStartPoint;
        private bool isDragging = false;
        public AddFile()
        {
            InitializeComponent();
            panel1.AutoScroll = true; // AutoScroll ON

            panel1.Padding = new Padding(10);
            image1.AllowDrop = true;
            image1.DragEnter += image1_DragEnter;
            image1.DragDrop += image1_DragDrop;

            panel1.MouseDown += Pb_MouseDown;


        }

        private void btnUplaod_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All Supported Files|*.rar;*.zip;*.jpg;*.jpeg;*.png;*.pdf;*.docx;*.xlsx|RAR Files|*.rar|ZIP Files|*.zip|Images|*.jpg;*.jpeg;*.png|PDF Files|*.pdf|All Files|*.*";
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string outputFolder = Path.Combine(Application.StartupPath, "ExtractedFiles");

                if (Directory.Exists(outputFolder))
                    Directory.Delete(outputFolder, true);

                Directory.CreateDirectory(outputFolder);

                // نظف لوحة panel1 قبل الإضافة (اختياري)

                // تعالج كل ملف على حدة
                foreach (string path in ofd.FileNames)
                {
                    string extension = Path.GetExtension(path).ToLower();
                    if (extension == ".rar")
                    {
                        ExtractRAR(path, outputFolder);
                    }
                    else if (extension == ".zip")
                    {
                        ExtractZIP(path, outputFolder);
                    }
                    else
                    {
                        // انسخ الملف داخل مجلد الاستخراج
                        File.Copy(path, Path.Combine(outputFolder, Path.GetFileName(path)), true);
                    }
                }

                // بعد انتهاء معالجة كل الملفات، اعرضها
                DisplayFiles(outputFolder);
            }
        }
        public void ExtractRAR(string rarPath, string outputDirectory)
        {
            using (var archive = ArchiveFactory.Open(rarPath))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory(outputDirectory, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
        }

        private void ExtractZIP(string zipPath, string outputDirectory)
        {
       //     ZipFile.ExtractToDirectory(zipPath, outputDirectory, true);
        }

        private void DisplayFiles(string directory)
        {

            int padding = 10;
            int maxWidth = 120;
            int maxHeight = 120;
            int itemsPerRow = Math.Max(1, (panel1.ClientSize.Width - padding) / (maxWidth + padding));
            // نحدد كم عدد الصور القديمة بالفعل
            int count = panel1.Controls.Count;
            int x = padding + (count % itemsPerRow) * (maxWidth + padding);
            int y = padding + (count / itemsPerRow) * (maxHeight + padding);


            string[] files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string ext = Path.GetExtension(file).ToLower();

                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                {
                    PictureBox pb = new PictureBox();
                    pb.Width = maxWidth;
                    pb.Height = maxHeight;
                    pb.SizeMode = PictureBoxSizeMode.Zoom;
                    pb.BorderStyle = BorderStyle.None;
                    pb.BackColor = Color.Transparent;
                    pb.Tag = file;

                    try
                    {
                        using (var imgTemp = Image.FromFile(file))
                        {
                            pb.Image = new Bitmap(imgTemp);
                        }
                    }
                    catch
                    {
                        pb.BackColor = Color.White;
                    }

                    pb.Location = new Point(x, y);
                    pb.DoubleClick += Pb_DoubleClick;
                    pb.MouseDown += Pb_MouseDown;  // ← أضف هذا السطر
                    pb.MouseMove += Pb_MouseMove;
                    pb.MouseUp += Pb_MouseUp;

                    panel1.Controls.Add(pb);

                    x += maxWidth + padding;
                    count++;
                    if (count % itemsPerRow == 0)
                    {
                        x = padding;
                        y += maxHeight + padding;
                    }
                }
                else if (ext == ".pdf")
                {
                    try
                    {
                        string pdfImagesDir = Path.Combine(directory, Path.GetFileNameWithoutExtension(file) + "_Images");
                        if (!Directory.Exists(pdfImagesDir))
                            Directory.CreateDirectory(pdfImagesDir);

                        using (var document = PdfDocument.Load(file))
                        {
                            int dpi = 100;
                            for (int i = 0; i < document.PageCount; i++)
                            {
                                string imagePath = Path.Combine(pdfImagesDir, $"Page_{i + 1}.png");

                                if (!File.Exists(imagePath))
                                {
                                    using (var image = document.Render(i, dpi, dpi, true))
                                    {
                                        image.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                                    }
                                }
                            }
                        }

                        string[] pdfImages = Directory.GetFiles(pdfImagesDir, "*.png");
                        foreach (var imgFile in pdfImages)
                        {
                            PictureBox pb = new PictureBox();
                            pb.Width = maxWidth;
                            pb.Height = maxHeight;
                            pb.SizeMode = PictureBoxSizeMode.Zoom;
                            pb.BorderStyle = BorderStyle.None;
                            pb.BackColor = Color.White;
                            pb.Tag = imgFile;

                            try
                            {
                                using (var imgTemp = Image.FromFile(imgFile))
                                {
                                    pb.Image = new Bitmap(imgTemp);
                                }
                            }
                            catch
                            {
                                pb.BackColor = Color.LightGray;
                            }

                            pb.Location = new Point(x, y);
                            pb.DoubleClick += Pb_DoubleClick;
                            pb.MouseDown += Pb_MouseDown;
                            pb.MouseMove += Pb_MouseMove;
                            pb.MouseUp += Pb_MouseUp;
                            panel1.Controls.Add(pb);

                            x += maxWidth + padding;
                            count++;
                            if (count % itemsPerRow == 0)
                            {
                                x = padding;
                                y += maxHeight + padding;
                            }
                        }
                    }
                    catch
                    {
                        PictureBox pbPdf = new PictureBox();
                        pbPdf.Width = maxWidth;
                        pbPdf.Height = maxHeight;
                        pbPdf.BackColor = Color.White;
                        pbPdf.Paint += (s, e) =>
                        {
                            e.Graphics.DrawString("PDF", new Font("Arial", 10, FontStyle.Bold),
                                Brushes.Black, new PointF(40, 50));
                        };
                        pbPdf.Location = new Point(x, y);
                        panel1.Controls.Add(pbPdf);

                        x += maxWidth + padding;
                        count++;
                        if (count % itemsPerRow == 0)
                        {
                            x = padding;
                            y += maxHeight + padding;
                        }
                    }
                }
                else
                {
                    PictureBox pbOther = new PictureBox();
                    pbOther.Width = maxWidth;
                    pbOther.Height = maxHeight;
                    pbOther.BackColor = Color.LightGray;
                    pbOther.BorderStyle = BorderStyle.None;
                    pbOther.Paint += (s, e) =>
                    {
                        e.Graphics.DrawString(Path.GetFileName(file), new Font("Arial", 8),
                            Brushes.Black, new PointF(5, 40));
                    };
                    pbOther.Location = new Point(x, y);
                    pbOther.Tag = file;
                    pbOther.DoubleClick += Pb_DoubleClick;
                    panel1.Controls.Add(pbOther);

                    x += maxWidth + padding;
                    count++;
                    if (count % itemsPerRow == 0)
                    {
                        x = padding;
                        y += maxHeight + padding;
                    }
                }
            }
        }

        private void Pb_DoubleClick(object sender, EventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb?.Tag != null)
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = pb.Tag.ToString(),
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ في فتح الملف: " + ex.Message);
                }
            }

        }

        private void image1_Click(object sender, EventArgs e)
        {

        }

        private void image1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void image1_DragDrop(object sender, DragEventArgs e)
        {
            string filePath = (string)e.Data.GetData(DataFormats.StringFormat);
            if (File.Exists(filePath))
            {
                try
                {
                    using (var imgTemp = Image.FromFile(filePath))
                    {
                        image1.Image = new Bitmap(imgTemp);
                    }
                }
                catch
                {
                    MessageBox.Show("Cannot load this file as image.");
                }
            }
        }
        private void Pb_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStartPoint = e.Location;
                isDragging = false;
            }
        }
        private void Pb_MouseMove(object sender, MouseEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (e.Button == MouseButtons.Left && pb != null)
            {
                // إذا تحرك الماوس مسافة أكبر من DragSize نبدأ السحب
                if (!isDragging)
                {
                    Size dragSize = SystemInformation.DragSize;
                    Rectangle dragRect = new Rectangle(
                        dragStartPoint.X - dragSize.Width / 2,
                        dragStartPoint.Y - dragSize.Height / 2,
                        dragSize.Width,
                        dragSize.Height);

                    if (!dragRect.Contains(e.Location))
                    {
                        isDragging = true;
                        pb.DoDragDrop(pb.Tag.ToString(), DragDropEffects.Copy);
                    }
                }
            }
        }

        private void Pb_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

       

    }

}

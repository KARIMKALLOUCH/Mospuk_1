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
using MySql.Data.MySqlClient;

namespace Mospuk_1
{

    public partial class AddFile : Form

    {
        MySqlDatabase db;

        private Point dragStartPoint;
        private bool isDragging = false;
        private List<PictureBox> selectedPictureBoxes = new List<PictureBox>(); // للتحديد المتعدد
        private bool isMultiSelectMode = false; // وضع التحديد المتعدد
        public AddFile(MySqlDatabase database)
        {
            InitializeComponent();
            db = database;

            panel1.AutoScroll = true; // AutoScroll ON

            panel1.Padding = new Padding(10);


            panel1.MouseDown += Pb_MouseDown;
            SetupImageApostille();
            SetupFlowLayoutPanel1();
            SetupFlowLayoutPanel2();     // إضافة تهيئة flowLayoutPanel2 مع دعم إعادة الترتيب



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
                    pb.MouseDown += Pb_MouseDown_Panel1;  // تحديث اسم الدالة
                    pb.MouseMove += Pb_MouseMove_Panel1;  // تحديث اسم الدالة
                    pb.MouseUp += Pb_MouseUp_Panel1;      // تحديث اسم الدالة
                    pb.Click += Pb_Click_Panel1;          // إضافة حدث النقر للتحديد المتعدد

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
                            pb.MouseDown += Pb_MouseDown_Panel1;
                            pb.MouseMove += Pb_MouseMove_Panel1;
                            pb.MouseUp += Pb_MouseUp_Panel1;
                            pb.Click += Pb_Click_Panel1;
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
        // أحداث panel1 - تم تحديث الأسماء والوظائف للتحديد المتعدد
        private void Pb_Click_Panel1(object sender, EventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null) return;

            // التحقق من الضغط على مفتاح Ctrl للتحديد المتعدد
            if (Control.ModifierKeys == Keys.Control)
            {
                isMultiSelectMode = true;
                if (selectedPictureBoxes.Contains(pb))
                {
                    // إلغاء التحديد
                    selectedPictureBoxes.Remove(pb);
                    pb.BorderStyle = BorderStyle.None;
                }
                else
                {
                    // إضافة للتحديد
                    selectedPictureBoxes.Add(pb);
                    pb.BorderStyle = BorderStyle.FixedSingle;
                }
            }
            else
            {
                // إلغاء التحديد السابق
                ClearSelection();
                // تحديد الصورة الحالية
                selectedPictureBoxes.Add(pb);
                pb.BorderStyle = BorderStyle.FixedSingle;
                isMultiSelectMode = false;
            }
        }

        private void ClearSelection()
        {
            foreach (var pb in selectedPictureBoxes)
            {
                pb.BorderStyle = BorderStyle.None;
            }
            selectedPictureBoxes.Clear();
        }

        private void Pb_MouseDown_Panel1(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStartPoint = e.Location;
                isDragging = false;
            }
        }

        private void Pb_MouseMove_Panel1(object sender, MouseEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (e.Button == MouseButtons.Left && pb != null)
            {
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

                        // تحضير البيانات للسحب
                        List<string> filePaths = new List<string>();

                        if (selectedPictureBoxes.Count > 1 && selectedPictureBoxes.Contains(pb))
                        {
                            // سحب الصور المحددة
                            foreach (var selectedPb in selectedPictureBoxes)
                            {
                                if (selectedPb.Tag != null)
                                    filePaths.Add(selectedPb.Tag.ToString());
                            }
                        }
                        else
                        {
                            // سحب الصورة الحالية فقط
                            if (pb.Tag != null)
                                filePaths.Add(pb.Tag.ToString());
                        }

                        if (filePaths.Count > 0)
                        {
                            string dataToTransfer = string.Join("|", filePaths);
                            pb.DoDragDrop(dataToTransfer, DragDropEffects.Move);
                        }
                    }
                }
            }
        }

        private void Pb_MouseUp_Panel1(object sender, MouseEventArgs e)
        {
            isDragging = false;
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
        private void ReArrangeImages()
        {
            int padding = 10;
            int maxWidth = 120;
            int maxHeight = 120;
            int itemsPerRow = Math.Max(1, (panel1.ClientSize.Width - padding) / (maxWidth + padding));

            int count = 0;
            foreach (Control control in panel1.Controls)
            {
                if (control is PictureBox)
                {
                    int x = padding + (count % itemsPerRow) * (maxWidth + padding);
                    int y = padding + (count / itemsPerRow) * (maxHeight + padding);

                    control.Location = new Point(x, y);
                    count++;
                }
            }
        }
        private void SetupFlowLayoutPanel1()
        {
            flowLayoutPanel1.AllowDrop = true;
            flowLayoutPanel1.DragEnter += FlowLayoutPanel1_DragEnter;
            flowLayoutPanel1.DragDrop += FlowLayoutPanel1_DragDrop;
        }

        private void FlowLayoutPanel1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat) || e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void FlowLayoutPanel1_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                List<string> filePaths = new List<string>();

                // التعامل مع الملفات الخارجية
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    filePaths.AddRange(files);
                }
                // التعامل مع السحب الداخلي من panel1
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    filePaths.AddRange(data.Split('|'));
                }

                // معالجة كل ملف
                foreach (string filePath in filePaths)
                {
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) continue;

                    string ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        // البحث عن PictureBox فارغ في flowLayoutPanel1
                        PictureBox targetPic = FindEmptyPictureBox();

                        if (targetPic == null)
                        {
                            // إنشاء PictureBox جديد إذا لم يوجد فارغ
                            targetPic = CreateNewPictureBox();
                            flowLayoutPanel1.Controls.Add(targetPic);
                        }

                        // تحميل الصورة في PictureBox
                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            if (targetPic.Image != null)
                                targetPic.Image.Dispose();

                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

                        // حذف الصورة من panel1 إذا كانت من السحب الداخلي
                        if (e.Data.GetDataPresent(DataFormats.StringFormat))
                        {
                            RemoveImageFromPanel1(filePath);
                        }
                    }
                }

                // مسح التحديد بعد السحب
                ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while processing drag and drop:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // البحث عن PictureBox فارغ في flowLayoutPanel1
        private PictureBox FindEmptyPictureBox()
        {
            foreach (Control control in flowLayoutPanel1.Controls)
            {
                if (control is PictureBox pb && pb.Image == null)
                {
                    return pb;
                }
            }
            return null;
        }

        // إنشاء PictureBox جديد مع إعدادات السحب والإفلات وإعادة الترتيب
        private PictureBox CreateNewPictureBox()
        {
            var pic = new Guna.UI2.WinForms.Guna2PictureBox();
            int index = flowLayoutPanel1.Controls.Count + 1;
            pic.Name = "_" + index.ToString();
            pic.Width = 130;
            pic.Height = 157;
            pic.BorderStyle = BorderStyle.FixedSingle;
            pic.SizeMode = PictureBoxSizeMode.StretchImage;
            pic.Margin = new Padding(5);
            pic.AllowDrop = true;

            // إضافة أحداث السحب والإفلات
            pic.DragEnter += Pic_DragEnter_FlowPanel;
            pic.DragDrop += Pic_DragDrop_FlowPanel;

            // إضافة أحداث إعادة الترتيب
            pic.MouseDown += Pic_MouseDown_FlowPanel;
            pic.MouseMove += Pic_MouseMove_FlowPanel;
            pic.MouseUp += Pic_MouseUp_FlowPanel;

            // إضافة قائمة سياق
            AddContextMenuToPictureBox(pic);

            return pic;
        }

        // أحداث السحب والإفلات في flowLayoutPanel1
        private void Pic_DragEnter_FlowPanel(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.StringFormat) || e.Data.GetDataPresent("FlowPanelReorder"))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Pic_DragDrop_FlowPanel(object sender, DragEventArgs e)
        {
            PictureBox targetPic = sender as PictureBox;
            if (targetPic == null) return;

            try
            {
                // التعامل مع إعادة الترتيب داخل flowLayoutPanel1
                if (e.Data.GetDataPresent("FlowPanelReorder"))
                {
                    PictureBox sourcePic = (PictureBox)e.Data.GetData("FlowPanelReorder");
                    if (sourcePic != targetPic)
                    {
                        SwapPictureBoxes(sourcePic, targetPic);
                    }
                    return;
                }

                // التعامل مع السحب العادي (من panel1 أو ملفات خارجية)
                string filePath = null;

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                        filePath = files[0];
                }
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    filePath = data.Split('|')[0]; // أخذ الملف الأول إذا كان هناك عدة ملفات
                }

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    string ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            if (targetPic.Image != null)
                                targetPic.Image.Dispose();

                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

                        // حذف من panel1 إذا كان السحب داخليًا
                        if (e.Data.GetDataPresent(DataFormats.StringFormat))
                        {
                            RemoveImageFromPanel1(filePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading the image:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // أحداث إعادة الترتيب في flowLayoutPanel1
        private PictureBox draggedPictureBox = null;

        private void Pic_MouseDown_FlowPanel(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStartPoint = e.Location;
                isDragging = false;
                draggedPictureBox = sender as PictureBox;
            }
        }

        // الحل الأول: استخدام DataObject لحمل البيانات المخصصة
        private void Pic_MouseMove_FlowPanel(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && draggedPictureBox != null && draggedPictureBox.Image != null)
            {
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

                        // إنشاء DataObject لحمل البيانات المخصصة
                        DataObject dataObject = new DataObject();
                        dataObject.SetData("FlowPanelReorder", draggedPictureBox);

                        // بدء عملية السحب لإعادة الترتيب
                        draggedPictureBox.DoDragDrop(dataObject, DragDropEffects.Move);
                    }
                }
            }
        }

        private void Pic_MouseUp_FlowPanel(object sender, MouseEventArgs e)
        {
            isDragging = false;
            draggedPictureBox = null;
        }

        // دالة تبديل مواضع الصور في flowLayoutPanel1
        private void SwapPictureBoxes(PictureBox source, PictureBox target)
        {
            // حفظ بيانات الصورة المصدر
            Image sourceImage = source.Image;
            object sourceTag = source.Tag;

            // حفظ بيانات الصورة الهدف
            Image targetImage = target.Image;
            object targetTag = target.Tag;

            // تبديل الصور
            source.Image = targetImage;
            source.Tag = targetTag;

            target.Image = sourceImage;
            target.Tag = sourceTag;
        }

        // تحديث الدالة لتعمل مع التحديد المتعدد
     

        private void savebtn_Click(object sender, EventArgs e)
        {
            string companyClient = Company_Client.Text.Trim();
            DateTime receptionDate = Reception_Date.Value.Date;
            string receptionTime = Time.Text.Trim();

            // قيمة أيام التسليم من الـ ComboBox (مخزن كـ KeyValuePair<string,int>)
            int deliveryDays = ((KeyValuePair<string, int>)Delivery_Date.SelectedItem).Value;

            DateTime deliveryDate = receptionDate.AddDays(deliveryDays);

            // جلب آخر رقم طلب في نفس يوم الاستقبال
            string orderQuery = "SELECT IFNULL(MAX(project_order), 0) FROM projects WHERE reception_date = @date";
            object result = db.ExecuteScalar(orderQuery, new List<MySqlParameter>
    {
        new MySqlParameter("@date", receptionDate.ToString("yyyy-MM-dd"))
    });

            int lastOrder = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
            int newOrder = lastOrder + 1;

            // قيم أنواع الوثيقة والترجمة (مخزنة كـ KeyValuePair<string,string>)
            string documentType = ((KeyValuePair<string, string>)comboDocumentType.SelectedItem).Value;
            string translationType = ((KeyValuePair<string, string>)comboTranslation.SelectedItem).Value;

            string hoursSpent = "24";

            // تكوين اسم المجلد مع إضافة نوع الترجمة ونوع الوثيقة
            string deliveryDateStr = deliveryDate.ToString("yyyyMMdd");
            string receptionDateStr = receptionDate.ToString("yyMMdd");
            string projectOrderStr = newOrder.ToString("D2");
            string receptionTimeStr = receptionTime.Replace(":", "");

            string folderName = $"{deliveryDateStr}{hoursSpent}_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}";

            if (!string.IsNullOrWhiteSpace(txtnotes.Text))
            {
                // تنقية الملاحظة: إزالة الرموز غير المسموحة
                string rawNote = txtnotes.Text.Trim();
                string sanitizedNote = new string(rawNote.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray()).Replace(" ", "_");

                // تقصير الملاحظة إن كانت طويلة جدًا
                if (sanitizedNote.Length > 30)
                    sanitizedNote = sanitizedNote.Substring(0, 30);

                // إضافة الملاحظة إلى اسم المجلد بشكل واضح
                folderName += $"-------------{sanitizedNote}-----------";
            }

            string insertQuery = @"INSERT INTO projects 
        (company_client, reception_date, reception_time, delivery_days, delivery_date, hours_spent, project_order, folder_name, note, document_type, translation_type, registration_date, last_update_date) 
        VALUES 
        (@company_client, @reception_date, @reception_time, @delivery_days, @delivery_date, @hours_spent, @project_order, @folder_name, @note, @document_type, @translation_type, CURRENT_DATE, CURRENT_TIMESTAMP)";

            List<MySqlParameter> parameters = new List<MySqlParameter>
    {
        new MySqlParameter("@company_client", companyClient),
        new MySqlParameter("@reception_date", receptionDate.ToString("yyyy-MM-dd")),
        new MySqlParameter("@reception_time", receptionTime),
        new MySqlParameter("@delivery_days", deliveryDays),
        new MySqlParameter("@delivery_date", deliveryDate.ToString("yyyy-MM-dd")),
        new MySqlParameter("@hours_spent", 24),
        new MySqlParameter("@project_order", newOrder),
        new MySqlParameter("@folder_name", folderName),
        new MySqlParameter("@note", string.IsNullOrWhiteSpace(txtnotes.Text) ? DBNull.Value : (object)txtnotes.Text),
        new MySqlParameter("@document_type", documentType),
        new MySqlParameter("@translation_type", translationType)
    };

            bool success = db.ExecuteNonQuery(insertQuery, parameters);
            if (success)
            {
                // الحصول على ID المشروع المحفوظ حديثاً
                string getLastIdQuery = "SELECT LAST_INSERT_ID()";
                object lastIdResult = db.ExecuteScalar(getLastIdQuery, new List<MySqlParameter>());
                int projectId = Convert.ToInt32(lastIdResult);

                // حفظ الصور من flowLayoutPanel1
                bool allImagesSaved = SaveProjectImages(projectId, folderName, deliveryDateStr, receptionDateStr, projectOrderStr, receptionTimeStr, companyClient, translationType, documentType);

                if (allImagesSaved)
                {
                    MessageBox.Show("✅ Project and images saved successfully");
                }
                else
                {
                    MessageBox.Show("⚠️ Project saved but some images failed to save.");
                }
            }
            else
            {
                MessageBox.Show("❌ Error inserting project.");
            }
        }

        private bool SaveProjectImages(int projectId, string folderName, string deliveryDateStr, string receptionDateStr, string projectOrderStr, string receptionTimeStr, string companyClient, string translationType, string documentType)
        {
            bool allSaved = true;
            int imageCounter = 1;

            // إنشاء مجلد للمشروع إذا لم يكن موجوداً
            string projectFolder = Path.Combine(Application.StartupPath, "ProjectImages", folderName);
            if (!Directory.Exists(projectFolder))
            {
                Directory.CreateDirectory(projectFolder);
            }

            // 1. حفظ صور flowLayoutPanel1 (الصور الأساسية) أولاً
            foreach (Control control in flowLayoutPanel1.Controls)
            {
                if (control is PictureBox pb && pb.Image != null && pb.Tag != null)
                {
                    try
                    {
                        // تكوين اسم الصورة الأساسية
                        string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}";

                        // الحصول على امتداد الملف الأصلي
                        string originalPath = pb.Tag.ToString();
                        string extension = Path.GetExtension(originalPath);

                        // اسم الملف الكامل مع الامتداد
                        string fullImageName = imageName + extension;

                        // مسار حفظ الصورة
                        string imagePath = Path.Combine(projectFolder, fullImageName);

                        // نسخ الصورة إلى مجلد المشروع
                        if (File.Exists(originalPath))
                        {
                            File.Copy(originalPath, imagePath, true);
                        }
                        else
                        {
                            // إذا لم يكن الملف الأصلي موجود، احفظ الصورة من PictureBox
                            pb.Image.Save(imagePath);
                        }

                        // حفظ معلومات الصورة في قاعدة البيانات
                        string insertImageQuery = @"INSERT INTO items (project_id, image_name, image_path, registration_date, last_update_date) 
                                  VALUES (@project_id, @image_name, @image_path, CURRENT_DATE, CURRENT_TIMESTAMP)";

                        List<MySqlParameter> imageParameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@project_id", projectId),
                    new MySqlParameter("@image_name", fullImageName),
                    new MySqlParameter("@image_path", imagePath)
                };

                        bool imageSaved = db.ExecuteNonQuery(insertImageQuery, imageParameters);
                        if (!imageSaved)
                        {
                            allSaved = false;
                            MessageBox.Show($"Failed to save image: {fullImageName}");
                        }

                        imageCounter++;
                    }
                    catch (Exception ex)
                    {
                        allSaved = false;
                        MessageBox.Show($"Error saving image {imageCounter}: {ex.Message}");
                        imageCounter++;
                    }
                }
            }

            // 2. حفظ صورة imageApostille (إذا كانت موجودة وتحتوي على صورة)
            PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            if (imageApostille != null && imageApostille.Image != null && imageApostille.Tag != null)
            {
                try
                {
                    // تكوين اسم صورة Apostille
                    string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Apostille";

                    // الحصول على امتداد الملف الأصلي
                    string originalPath = imageApostille.Tag.ToString();
                    string extension = Path.GetExtension(originalPath);

                    // اسم الملف الكامل مع الامتداد
                    string fullImageName = imageName + extension;

                    // مسار حفظ الصورة
                    string imagePath = Path.Combine(projectFolder, fullImageName);

                    // نسخ الصورة إلى مجلد المشروع
                    if (File.Exists(originalPath))
                    {
                        File.Copy(originalPath, imagePath, true);
                    }
                    else
                    {
                        // إذا لم يكن الملف الأصلي موجود، احفظ الصورة من PictureBox
                        imageApostille.Image.Save(imagePath);
                    }

                    // حفظ معلومات الصورة في قاعدة البيانات
                    string insertImageQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) 
                              VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";

                    List<MySqlParameter> imageParameters = new List<MySqlParameter>
            {
                new MySqlParameter("@project_id", projectId),
                new MySqlParameter("@image_name", fullImageName),
                new MySqlParameter("@image_path", imagePath),
                new MySqlParameter("@attachment_type", "Apostille")
            };

                    bool imageSaved = db.ExecuteNonQuery(insertImageQuery, imageParameters);
                    if (!imageSaved)
                    {
                        allSaved = false;
                        MessageBox.Show($"Failed to save Apostille image: {fullImageName}");
                    }

                    imageCounter++;
                }
                catch (Exception ex)
                {
                    allSaved = false;
                    MessageBox.Show($"Error saving Apostille image: {ex.Message}");
                    imageCounter++;
                }
            }

            // 3. حفظ صور flowLayoutPanel2 (المرفقات)
            string attachmentType = "A";

            foreach (Control control in flowLayoutPanel2.Controls)
            {
                if (control is PictureBox pb && pb.Image != null && pb.Tag != null)
                {
                    try
                    {
                        // تكوين اسم صورة المرفق
                        string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_{attachmentType}";

                        // الحصول على امتداد الملف الأصلي
                        string originalPath = pb.Tag.ToString();
                        string extension = Path.GetExtension(originalPath);

                        // اسم الملف الكامل مع الامتداد
                        string fullImageName = imageName + extension;

                        // مسار حفظ الصورة
                        string imagePath = Path.Combine(projectFolder, fullImageName);

                        // نسخ الصورة إلى مجلد المشروع
                        if (File.Exists(originalPath))
                        {
                            File.Copy(originalPath, imagePath, true);
                        }
                        else
                        {
                            // إذا لم يكن الملف الأصلي موجود، احفظ الصورة من PictureBox
                            pb.Image.Save(imagePath);
                        }

                        // حفظ معلومات الصورة في قاعدة البيانات
                        string insertImageQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) 
                      VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";

                        List<MySqlParameter> imageParameters = new List<MySqlParameter>
            {
                new MySqlParameter("@project_id", projectId),
                new MySqlParameter("@image_name", fullImageName),
                new MySqlParameter("@image_path", imagePath),
                new MySqlParameter("@attachment_type", attachmentType)
            };

                        bool imageSaved = db.ExecuteNonQuery(insertImageQuery, imageParameters);
                        if (!imageSaved)
                        {
                            allSaved = false;
                            MessageBox.Show($"Failed to save attachment image: {fullImageName}");
                        }

                        imageCounter++;
                    }
                    catch (Exception ex)
                    {
                        allSaved = false;
                        MessageBox.Show($"Error saving attachment image {imageCounter}: {ex.Message}");
                        imageCounter++;
                    }
                }
            }

            // 4. حفظ ملفات Word من panelDocx أخيراً
            foreach (Control control in panelDocx.Controls)
            {
                if (control is Label lbl && lbl.Tag != null)
                {
                    try
                    {
                        // تكوين اسم ملف Word
                        string wordFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_OCR";

                        // الحصول على مسار الملف الأصلي
                        string originalPath = lbl.Tag.ToString();
                        string extension = Path.GetExtension(originalPath);

                        // اسم الملف الكامل مع الامتداد
                        string fullWordFileName = wordFileName + extension;

                        // مسار حفظ ملف Word
                        string wordPath = Path.Combine(projectFolder, fullWordFileName);

                        // نسخ ملف Word إلى مجلد المشروع
                        if (File.Exists(originalPath))
                        {
                            File.Copy(originalPath, wordPath, true);

                            // حفظ معلومات ملف Word في قاعدة البيانات
                            string insertWordQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) 
                                      VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";

                            List<MySqlParameter> wordParameters = new List<MySqlParameter>
                    {
                        new MySqlParameter("@project_id", projectId),
                        new MySqlParameter("@image_name", fullWordFileName),
                        new MySqlParameter("@image_path", wordPath),
                        new MySqlParameter("@attachment_type", "WORD") // تحديد نوع المرفق كـ WORD
                    };

                            bool wordSaved = db.ExecuteNonQuery(insertWordQuery, wordParameters);
                            if (!wordSaved)
                            {
                                allSaved = false;
                                MessageBox.Show($"Failed to save Word file: {fullWordFileName}");
                            }
                            else
                            {
                            }

                            imageCounter++;
                        }
                        else
                        {
                            allSaved = false;
                            MessageBox.Show($"Word file not found: {originalPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        allSaved = false;
                        MessageBox.Show($"Error saving Word file {imageCounter}: {ex.Message}");
                        imageCounter++;
                    }
                    // 5. إنشاء ملف Word جديد بإسم Google Driver
                    try
                    {
                        string googleDriverFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Google Driver.docx";
                        string googleDriverPath = Path.Combine(projectFolder, googleDriverFileName);

                        // إنشاء ملف Word فارغ (يمكنك تعديل هذا لإنشاء قالب مخصص مثلاً)
                        using (var fs = File.Create(googleDriverPath))
                        {
                            // يمكنك كتابة محتوى افتراضي هنا إن أردت
                        }

                        // حفظ في قاعدة البيانات
                        string insertGoogleDriverQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) 
              VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";

                        List<MySqlParameter> googleDriverParams = new List<MySqlParameter>
    {
        new MySqlParameter("@project_id", projectId),
        new MySqlParameter("@image_name", googleDriverFileName),
        new MySqlParameter("@image_path", googleDriverPath),
        new MySqlParameter("@attachment_type", "Google Driver")
    };

                        bool googleDriverSaved = db.ExecuteNonQuery(insertGoogleDriverQuery, googleDriverParams);
                        if (!googleDriverSaved)
                        {
                            allSaved = false;
                            MessageBox.Show($"❌ Failed to save Google Driver file: {googleDriverFileName}");
                        }

                        imageCounter++;
                    }
                    catch (Exception ex)
                    {
                        allSaved = false;
                        MessageBox.Show($"❌ Error creating Google Driver file {imageCounter}: {ex.Message}");
                        imageCounter++;
                    }

                }
            }

            return allSaved;
        }
     

        // تصحيح Pic_DragEnter لقبول كلا النوعين من السحب
        private void Pic_DragEnter(object sender, DragEventArgs e)
        {
            // قبول الملفات الخارجية والسحب الداخلي من panel1
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        // تصحيح Pic_DragDrop للتعامل مع النوعين
        private void Pic_DragDrop(object sender, DragEventArgs e)
        {
            PictureBox targetPic = sender as PictureBox;
            if (targetPic == null) return;

            string filePath = null;

            try
            {
                // التعامل مع الملفات الخارجية (من خارج التطبيق)
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        filePath = files[0];
                    }
                }
                // التعامل مع السحب الداخلي من panel1
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    filePath = (string)e.Data.GetData(DataFormats.StringFormat);
                }

                // إذا تم الحصول على مسار الملف
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    // التأكد من أن الملف صورة
                    string ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        // تحميل الصورة
                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            // تنظيف الصورة السابقة إن وجدت
                            if (targetPic.Image != null)
                            {
                                targetPic.Image.Dispose();
                            }

                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

                        // حذف الصورة من panel1 إذا كانت موجودة هناك (فقط في حالة السحب الداخلي)
                        if (e.Data.GetDataPresent(DataFormats.StringFormat))
                        {
                            RemoveImageFromPanel1(filePath);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please select a valid image file (jpg, png, bmp, gif).", "Unsupported File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading the image:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // دالة مساعدة لحذف الصورة من panel1
        private void RemoveImageFromPanel1(string filePath)
        {
            PictureBox pbToRemove = null;
            foreach (Control control in panel1.Controls)
            {
                if (control is PictureBox pb && pb.Tag != null && pb.Tag.ToString() == filePath)
                {
                    pbToRemove = pb;
                    break;
                }
            }

            if (pbToRemove != null)
            {
                // تنظيف موارد الصورة قبل الإزالة
                if (pbToRemove.Image != null)
                {
                    pbToRemove.Image.Dispose();
                    pbToRemove.Image = null;
                }

                // إزالة PictureBox من panel1
                panel1.Controls.Remove(pbToRemove);
                pbToRemove.Dispose();

                // إعادة ترتيب الصور المتبقية في panel1
                ReArrangeImages();
            }
        }

        // إضافة دالة لتنظيف PictureBox (اختيارية)
        private void ClearPictureBox(PictureBox pb)
        {
            if (pb.Image != null && pb.Tag != null && File.Exists(pb.Tag.ToString()))
            {
                string filePath = pb.Tag.ToString();

                // تنظيف الصورة في الـ PictureBox في flowLayoutPanel1 (لكن لا تزيله)
                pb.Image.Dispose();
                pb.Image = null;
                pb.Tag = null;

                // إرجاع الصورة إلى panel1 كصورة جديدة
                AddImageBackToPanel1(filePath);
            }
        }
        private void AddImageBackToPanel1(string filePath)
        {
            int padding = 10;
            int maxWidth = 120;
            int maxHeight = 120;
            int itemsPerRow = Math.Max(1, (panel1.ClientSize.Width - padding) / (maxWidth + padding));

            int count = panel1.Controls.Count;
            int x = padding + (count % itemsPerRow) * (maxWidth + padding);
            int y = padding + (count / itemsPerRow) * (maxHeight + padding);

            PictureBox pb = new PictureBox();
            pb.Width = maxWidth;
            pb.Height = maxHeight;
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.BorderStyle = BorderStyle.None;
            pb.BackColor = Color.Transparent;
            pb.Tag = filePath;

            try
            {
                using (var imgTemp = Image.FromFile(filePath))
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
            panel1.Controls.SetChildIndex(pb, 0); // ضعه في بداية القائمة

            ReArrangeImages();
        }


        // إضافة قائمة سياق للتحكم في PictureBox الجديد (اختيارية)
        private void AddContextMenuToPictureBox(PictureBox pb)
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            ToolStripMenuItem clearItem = new ToolStripMenuItem("Return Image");
            clearItem.Click += (s, e) => ClearPictureBox(pb);

            ToolStripMenuItem openItem = new ToolStripMenuItem("Open Image");
            openItem.Click += (s, e) => {
                if (pb.Tag != null && File.Exists(pb.Tag.ToString()))
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
                        MessageBox.Show("An error occurred while opening the file:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            contextMenu.Items.Add(clearItem);
            contextMenu.Items.Add(openItem);

            pb.ContextMenuStrip = contextMenu;
        }

        private void AddFile_Load(object sender, EventArgs e)
        {
            // تهيئة Delivery_Date
            Delivery_Date.Items.Add(new KeyValuePair<string, int>("Default", 3));
            Delivery_Date.Items.Add(new KeyValuePair<string, int>("Urgent (2 days)", 2));
            Delivery_Date.Items.Add(new KeyValuePair<string, int>("Very Urgent (1 day)", 1));
            Delivery_Date.DisplayMember = "Key";
            Delivery_Date.ValueMember = "Value";
            Delivery_Date.SelectedIndex = 0;

            // تهيئة comboDocumentType
            comboDocumentType.Items.Add(new KeyValuePair<string, string>("Nacimiento", "Nacimiento"));
            comboDocumentType.Items.Add(new KeyValuePair<string, string>("Bac", "Bac"));
            comboDocumentType.DisplayMember = "Key";
            comboDocumentType.ValueMember = "Value";
            comboDocumentType.SelectedIndex = 0;

            // تهيئة comboTranslation
            comboTranslation.Items.Add(new KeyValuePair<string, string>("ErAr", "ErAr"));
            comboTranslation.Items.Add(new KeyValuePair<string, string>("EsAr", "EsAr"));
            comboTranslation.DisplayMember = "Key";
            comboTranslation.ValueMember = "Value";
            comboTranslation.SelectedIndex = 0;



        
        }
     
  

      
        

        private void btnAddWord_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = @"C:\Users\karim_01\Downloads\karim archive\karim archive";
            ofd.Filter = "Word Files|*.doc;*.docx|All Files|*.*";
            ofd.Title = "Select Word File";
            ofd.Multiselect = true; // السماح بتحديد عدة ملفات

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (string filePath in ofd.FileNames)
                {
                    string fileName = Path.GetFileName(filePath);
                    string fullPath = filePath;

                    // التحقق من عدم وجود الملف مسبقاً
                    bool fileExists = false;
                    foreach (Control ctrl in panelDocx.Controls)
                    {
                        if (ctrl is Label existingLbl && existingLbl.Tag?.ToString() == fullPath)
                        {
                            fileExists = true;
                            break;
                        }
                    }

                    if (!fileExists)
                    {
                        // إنشاء Label جديد لكل ملف
                        Label lbl = new Label();
                        lbl.Text = fileName;
                        lbl.Tag = fullPath; // تخزين المسار الكامل
                        lbl.AutoSize = true;
                        lbl.Padding = new Padding(5);
                        lbl.Margin = new Padding(5);
                        lbl.BackColor = Color.SeaGreen;
                        lbl.ForeColor = Color.White;
                        lbl.Cursor = Cursors.Hand;
                        lbl.BorderStyle = BorderStyle.FixedSingle;

                        // إضافة ToolTip لإظهار المسار الكامل
                        ToolTip toolTip = new ToolTip();
                        toolTip.SetToolTip(lbl, fullPath);

                        // إضافة حدث النقر لفتح الملف
                        lbl.DoubleClick += (s, ev) =>
                        {
                            try
                            {
                                var psi = new System.Diagnostics.ProcessStartInfo()
                                {
                                    FileName = fullPath,
                                    UseShellExecute = true
                                };
                                System.Diagnostics.Process.Start(psi);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        };

                        // إضافة قائمة سياق لحذف الملف
                        ContextMenuStrip contextMenu = new ContextMenuStrip();
                        ToolStripMenuItem removeItem = new ToolStripMenuItem("Remove File");
                        removeItem.Click += (s, ev) =>
                        {
                            if (MessageBox.Show($"Are you sure you want to remove '{fileName}' from the list?",
                                "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                panelDocx.Controls.Remove(lbl);
                                lbl.Dispose();
                            }
                        };
                        contextMenu.Items.Add(removeItem);
                        lbl.ContextMenuStrip = contextMenu;

                        panelDocx.Controls.Add(lbl);
                    }
                    else
                    {
                        MessageBox.Show($"File '{fileName}' is already added.", "Duplicate File", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
         }

        private void flowLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void labelControl11_Click(object sender, EventArgs e)
        {

        }
      

        // ثانياً: إضافة دالة تهيئة مخصصة لـ flowLayoutPanel2
        private void SetupFlowLayoutPanel2()
        {
            flowLayoutPanel2.AllowDrop = true;
            flowLayoutPanel2.DragEnter += FlowLayoutPanel2_DragEnter;
            flowLayoutPanel2.DragDrop += FlowLayoutPanel2_DragDrop;
        }
        // ثالثاً: أحداث flowLayoutPanel2 (نسخة من أحداث flowLayoutPanel1)
        private void FlowLayoutPanel2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat) || e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void FlowLayoutPanel2_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                List<string> filePaths = new List<string>();

                // التعامل مع الملفات الخارجية
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    filePaths.AddRange(files);
                }
                // التعامل مع السحب الداخلي من panel1
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    filePaths.AddRange(data.Split('|'));
                }

                // معالجة كل ملف
                foreach (string filePath in filePaths)
                {
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) continue;

                    string ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        // البحث عن PictureBox فارغ في flowLayoutPanel2
                        PictureBox targetPic = FindEmptyPictureBoxInPanel2();

                        if (targetPic == null)
                        {
                            // إنشاء PictureBox جديد إذا لم يوجد فارغ
                            targetPic = CreateNewPictureBoxForPanel2();
                            flowLayoutPanel2.Controls.Add(targetPic);
                        }

                        // تحميل الصورة في PictureBox
                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            if (targetPic.Image != null)
                                targetPic.Image.Dispose();

                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

                        // حذف الصورة من panel1 إذا كانت من السحب الداخلي
                        if (e.Data.GetDataPresent(DataFormats.StringFormat))
                        {
                            RemoveImageFromPanel1(filePath);
                        }
                    }
                }

                // مسح التحديد بعد السحب
                ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while processing drag and drop:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // رابعاً: البحث عن PictureBox فارغ في flowLayoutPanel2
        private PictureBox FindEmptyPictureBoxInPanel2()
        {
            foreach (Control control in flowLayoutPanel2.Controls)
            {
                if (control is PictureBox pb && pb.Image == null)
                {
                    return pb;
                }
            }
            return null;
        }

        // خامساً: إنشاء PictureBox جديد مع دعم إعادة الترتيب لـ flowLayoutPanel2
        private PictureBox CreateNewPictureBoxForPanel2()
        {
            var pic = new Guna.UI2.WinForms.Guna2PictureBox();
            int index = flowLayoutPanel2.Controls.Count + 1;
            pic.Name = "_panel2_" + index.ToString();
            pic.Width = 130;
            pic.Height = 157;
            pic.BorderStyle = BorderStyle.FixedSingle;
            pic.SizeMode = PictureBoxSizeMode.StretchImage;
            pic.Margin = new Padding(5);
            pic.AllowDrop = true;

            // إضافة أحداث السحب والإفلات للملفات الخارجية
            pic.DragEnter += Pic_DragEnter_Panel2;
            pic.DragDrop += Pic_DragDrop_Panel2;

            // إضافة أحداث إعادة الترتيب داخل flowLayoutPanel2
            pic.MouseDown += Pic_MouseDown_Panel2;
            pic.MouseMove += Pic_MouseMove_Panel2;
            pic.MouseUp += Pic_MouseUp_Panel2;

            // إضافة قائمة سياق
            AddContextMenuToPictureBoxPanel2(pic);

            return pic;
        }

        // سادساً: أحداث السحب والإفلات في PictureBox داخل flowLayoutPanel2
        private void Pic_DragEnter_Panel2(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetDataPresent(DataFormats.StringFormat) ||
                e.Data.GetDataPresent("FlowPanel2Reorder"))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Pic_DragDrop_Panel2(object sender, DragEventArgs e)
        {
            PictureBox targetPic = sender as PictureBox;
            if (targetPic == null) return;

            try
            {
                // التعامل مع إعادة الترتيب داخل flowLayoutPanel2
                if (e.Data.GetDataPresent("FlowPanel2Reorder"))
                {
                    PictureBox sourcePic = (PictureBox)e.Data.GetData("FlowPanel2Reorder");
                    if (sourcePic != targetPic)
                    {
                        SwapPictureBoxesPanel2(sourcePic, targetPic);
                    }
                    return;
                }

                // التعامل مع السحب العادي (من panel1 أو ملفات خارجية)
                string filePath = null;

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                        filePath = files[0];
                }
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    filePath = data.Split('|')[0]; // أخذ الملف الأول إذا كان هناك عدة ملفات
                }

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    string ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            if (targetPic.Image != null)
                                targetPic.Image.Dispose();

                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

                        // حذف من panel1 إذا كان السحب داخليًا
                        if (e.Data.GetDataPresent(DataFormats.StringFormat))
                        {
                            RemoveImageFromPanel1(filePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading the image:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // سابعاً: أحداث إعادة الترتيب داخل flowLayoutPanel2
        private PictureBox draggedPictureBoxPanel2 = null;

        private void Pic_MouseDown_Panel2(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStartPoint = e.Location;
                isDragging = false;
                draggedPictureBoxPanel2 = sender as PictureBox;
            }
        }

        private void Pic_MouseMove_Panel2(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && draggedPictureBoxPanel2 != null && draggedPictureBoxPanel2.Image != null)
            {
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

                        // إنشاء DataObject لحمل البيانات المخصصة
                        DataObject dataObject = new DataObject();
                        dataObject.SetData("FlowPanel2Reorder", draggedPictureBoxPanel2);

                        // بدء عملية السحب لإعادة الترتيب
                        draggedPictureBoxPanel2.DoDragDrop(dataObject, DragDropEffects.Move);
                    }
                }
            }
        }

        private void Pic_MouseUp_Panel2(object sender, MouseEventArgs e)
        {
            isDragging = false;
            draggedPictureBoxPanel2 = null;
        }

        // ثامناً: دالة تبديل مواضع الصور في flowLayoutPanel2
        private void SwapPictureBoxesPanel2(PictureBox source, PictureBox target)
        {
            // حفظ بيانات الصورة المصدر
            Image sourceImage = source.Image;
            object sourceTag = source.Tag;

            // حفظ بيانات الصورة الهدف
            Image targetImage = target.Image;
            object targetTag = target.Tag;

            // تبديل الصور
            source.Image = targetImage;
            source.Tag = targetTag;

            target.Image = sourceImage;
            target.Tag = sourceTag;
        }

        // تاسعاً: دالة تنظيف PictureBox في flowLayoutPanel2
        private void ClearPictureBoxPanel2(PictureBox pb)
        {
            if (pb.Image != null && pb.Tag != null && File.Exists(pb.Tag.ToString()))
            {
                string filePath = pb.Tag.ToString();

                // تنظيف الصورة في الـ PictureBox في flowLayoutPanel2
                pb.Image.Dispose();
                pb.Image = null;
                pb.Tag = null;

                // إرجاع الصورة إلى panel1 كصورة جديدة
                AddImageBackToPanel1(filePath);
            }
        }

        // عاشراً: إضافة قائمة سياق مخصصة لـ flowLayoutPanel2
        private void AddContextMenuToPictureBoxPanel2(PictureBox pb)
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            ToolStripMenuItem clearItem = new ToolStripMenuItem("Return Image");
            clearItem.Click += (s, e) => ClearPictureBoxPanel2(pb);

            ToolStripMenuItem openItem = new ToolStripMenuItem("Open Image");
            openItem.Click += (s, e) => {
                if (pb.Tag != null && File.Exists(pb.Tag.ToString()))
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
                        MessageBox.Show("An error occurred while opening the file:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            contextMenu.Items.Add(clearItem);
            contextMenu.Items.Add(openItem);

            pb.ContextMenuStrip = contextMenu;
        }
        // إضافة دالة لتهيئة imageApostille مع إمكانية السحب والإفلات
        // إضافة دالة لتهيئة imageApostille مع إمكانية السحب والإفلات
        private void SetupImageApostille()
        {
            PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            if (imageApostille != null)
            {
                imageApostille.AllowDrop = true;
                imageApostille.SizeMode = PictureBoxSizeMode.StretchImage;
                imageApostille.BorderStyle = BorderStyle.FixedSingle;

                // إضافة الأحداث المطلوبة للسحب والإفلات
                imageApostille.DragEnter += ImageApostille_DragEnter;
                imageApostille.DragDrop += ImageApostille_DragDrop;

                // إضافة DragOver للتأكد من استمرارية السحب
                imageApostille.DragOver += ImageApostille_DragOver;

                // إضافة قائمة سياق
                AddContextMenuToImageApostille(imageApostille);
            }
        }

        // حدث DragEnter لـ imageApostille - محدث
        private void ImageApostille_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                // التحقق من نوع البيانات المسحوبة
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    // فحص أن الملفات المسحوبة هي صور
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        string ext = Path.GetExtension(files[0]).ToLower();
                        if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                        {
                            e.Effect = DragDropEffects.Copy;
                            return;
                        }
                    }
                }
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    // السحب الداخلي من panel1
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    if (!string.IsNullOrEmpty(data))
                    {
                        string firstFile = data.Split('|')[0];
                        if (File.Exists(firstFile))
                        {
                            string ext = Path.GetExtension(firstFile).ToLower();
                            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                            {
                                e.Effect = DragDropEffects.Move;
                                return;
                            }
                        }
                    }
                }

                // في حالة عدم تطابق أي شرط
                e.Effect = DragDropEffects.None;
            }
            catch (Exception ex)
            {
                e.Effect = DragDropEffects.None;
                MessageBox.Show($"Error in DragEnter: {ex.Message}");
            }
        }

        // إضافة حدث DragOver للتأكد من استمرارية السحب
        private void ImageApostille_DragOver(object sender, DragEventArgs e)
        {
            // نفس منطق DragEnter
            ImageApostille_DragEnter(sender, e);
        }

        // حدث DragDrop لـ imageApostille - محدث
        private void ImageApostille_DragDrop(object sender, DragEventArgs e)
        {
            PictureBox targetPic = sender as PictureBox;
            if (targetPic == null) return;

            string filePath = null;

            try
            {
                // التعامل مع الملفات الخارجية (من خارج التطبيق)
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        filePath = files[0];
                    }
                }
                // التعامل مع السحب الداخلي من panel1
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    if (!string.IsNullOrEmpty(data))
                    {
                        filePath = data.Split('|')[0]; // أخذ الملف الأول
                    }
                }

                // إذا تم الحصول على مسار الملف
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    // التأكد من أن الملف صورة
                    string ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        // تحميل الصورة
                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            // تنظيف الصورة السابقة إن وجدت
                            if (targetPic.Image != null)
                            {
                                targetPic.Image.Dispose();
                            }

                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

                        // حذف الصورة من panel1 إذا كانت موجودة هناك (فقط في حالة السحب الداخلي)
                        if (e.Data.GetDataPresent(DataFormats.StringFormat))
                        {
                            RemoveImageFromPanel1(filePath);
                        }

                        // رسالة نجح العملية
                    }
                    else
                    {
                        MessageBox.Show("Please select a valid image file (jpg, png, bmp, gif).",
                            "Unsupported File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("File not found or invalid path.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading the image:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // إضافة قائمة سياق لـ imageApostille - محدثة
        private void AddContextMenuToImageApostille(PictureBox pb)
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            ToolStripMenuItem clearItem = new ToolStripMenuItem("Return Image");
            clearItem.Click += (s, e) => ClearImageApostille(pb);

            ToolStripMenuItem openItem = new ToolStripMenuItem("Open Image");
            openItem.Click += (s, e) => {
                if (pb.Tag != null && File.Exists(pb.Tag.ToString()))
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
                        MessageBox.Show("An error occurred while opening the file:\n" + ex.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

         

            contextMenu.Items.Add(clearItem);
            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            pb.ContextMenuStrip = contextMenu;
        }

        // دالة تنظيف imageApostille وإرجاع الصورة لـ panel1 - محدثة
        private void ClearImageApostille(PictureBox pb)
        {
            if (pb.Image != null)
            {
                string filePath = pb.Tag?.ToString();

                // تنظيف الصورة في imageApostille
                pb.Image.Dispose();
                pb.Image = null;
                pb.Tag = null;

                // إرجاع الصورة إلى panel1 إذا كان المسار صحيح
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    AddImageBackToPanel1(filePath);
                }

                MessageBox.Show("Image cleared from Apostille section!", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }


}

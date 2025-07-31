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
        private Rectangle selectionRectangle;      // مستطيل التحديد
        private Point selectionStartPoint;         // نقطة بداية السحب للتحديد
        private bool isSelecting = false;          // حالة تدل على أننا نقوم برسم مستطيل التحديد
        private FlowLayoutPanel dragSourcePanel = null;  // لتتبع المصدر
        private List<PictureBox> selectedPictureBoxesFlow1 = new List<PictureBox>(); // للتحديد المتعدد في flowLayoutPanel1
        private bool isMultiSelectModeFlow1 = false; // وضع التحديد المتعدد                                              // ****************************************
        private DateTime lastClickTime = DateTime.MinValue;

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
            // *** إضافة 2: تفعيل استقبال السحب والإفلات في panel1 ***
            panel1.AllowDrop = true;
            panel1.DragEnter += panel1_DragEnter;
            panel1.DragDrop += panel1_DragDrop;

            this.KeyPreview = true; // *** أضف هذا السطر الهام ***
            this.KeyDown += AddFile_KeyDown; // *** أضف هذا السطر لربط الحدث ***


            panel1.MouseDown += Pb_MouseDown;
            SetupImageApostille();
            SetupFlowLayoutPanel1();
            SetupFlowLayoutPanel2();     // إضافة تهيئة flowLayoutPanel2 مع دعم إعادة الترتيب

            // *** إضافة 3: ربط أحداث جديدة لـ panel1 لرسم مستطيل التحديد ***
            panel1.MouseDown += panel1_MouseDown_ForSelection;
            panel1.MouseMove += panel1_MouseMove_ForSelection;
            panel1.MouseUp += panel1_MouseUp_ForSelection;
            panel1.Paint += panel1_Paint_SelectionRectangle;

        }
        private void AddFile_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (selectedPictureBoxesFlow1.Count > 0)
                {
                    var confirmResult = MessageBox.Show(
                        "Are you sure you want to delete the selected images?",
                        "Confirm Deletion",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirmResult == DialogResult.Yes)
                    {
                      
                        foreach (var pb in selectedPictureBoxesFlow1.ToList())
                        {
                            flowLayoutPanel1.Controls.Remove(pb);

                            pb.Dispose();
                        }

                        // بعد حذف كل الصور، قم بتفريغ قائمة العناصر المحددة
                        selectedPictureBoxesFlow1.Clear();
                    }
                }
            }
        }
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

                        // *** تعديل جديد: دعم السحب المتعدد ***
                        DataObject dataObject = new DataObject();
                        List<string> filePathsToTransfer = new List<string>();

                        // إذا كانت الصورة المسحوبة ضمن التحديد المتعدد وهناك أكثر من صورة محددة
                        if (selectedPictureBoxesFlow1.Count > 1 && selectedPictureBoxesFlow1.Contains(draggedPictureBox))
                        {
                            // سحب جميع الصور المحددة
                            foreach (var selectedPb in selectedPictureBoxesFlow1)
                            {
                                if (selectedPb.Tag != null && selectedPb.Image != null)
                                {
                                    filePathsToTransfer.Add(selectedPb.Tag.ToString());
                                }
                            }
                            // إضافة معرف خاص للسحب المتعدد
                            dataObject.SetData("MultiDragFlow1", selectedPictureBoxesFlow1.ToArray());
                        }
                        else
                        {
                            // سحب الصورة الحالية فقط
                            if (draggedPictureBox.Tag != null)
                            {
                                filePathsToTransfer.Add(draggedPictureBox.Tag.ToString());
                            }
                        }

                        // إعداد البيانات للسحب
                        dataObject.SetData("FlowPanelReorder", draggedPictureBox); // لإعادة الترتيب الداخلي
                        dataObject.SetData("ReturnToPanel1", draggedPictureBox);   // للإرجاع إلى panel1
                        dataObject.SetData("DragSource", dragSourcePanel?.Name ?? "unknown");

                        // إرسال مسارات الملفات
                        if (filePathsToTransfer.Count > 0)
                        {
                            string dataToTransfer = string.Join("|", filePathsToTransfer);
                            dataObject.SetData(DataFormats.StringFormat, dataToTransfer);
                        }

                        draggedPictureBox.DoDragDrop(dataObject, DragDropEffects.Move);
                    }
                }
            }
        }
        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
            // تحقق مما إذا كانت البيانات المسحوبة من النوع الذي يمكن إرجاعه
            if (e.Data.GetDataPresent("ReturnToPanel1"))
            {
                e.Effect = DragDropEffects.Move; // إظهار أيقونة النقل
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ReturnToPanel1"))
            {
                // التعامل مع السحب المتعدد من flowLayoutPanel1
                if (e.Data.GetDataPresent("MultiDragFlow1"))
                {
                    PictureBox[] selectedPictureBoxes = (PictureBox[])e.Data.GetData("MultiDragFlow1");

                    foreach (PictureBox sourcePb in selectedPictureBoxes)
                    {
                        if (sourcePb?.Tag != null && sourcePb.Image != null)
                        {
                            string filePath = sourcePb.Tag.ToString();
                            AddImageBackToPanel1(filePath);
                        }
                    }

                    // إزالة جميع الصور المحددة من flowLayoutPanel1
                    foreach (PictureBox sourcePb in selectedPictureBoxes)
                    {
                        if (sourcePb.Parent != null)
                        {
                            sourcePb.Parent.Controls.Remove(sourcePb);
                        }
                        sourcePb.Dispose();
                    }

                    // مسح التحديد
                    ClearSelectionFlow1();
                }
                else
                {
                    // السحب المفرد (الكود الأصلي)
                    PictureBox sourcePb = (PictureBox)e.Data.GetData("ReturnToPanel1");

                    if (sourcePb?.Tag != null)
                    {
                        string filePath = sourcePb.Tag.ToString();
                        AddImageBackToPanel1(filePath);

                        if (sourcePb.Name == "imageApostille")
                        {
                            sourcePb.Image?.Dispose();
                            sourcePb.Image = null;
                            sourcePb.Tag = null;
                        }
                        else
                        {
                            if (sourcePb.Parent != null)
                            {
                                sourcePb.Parent.Controls.Remove(sourcePb);
                            }
                            sourcePb.Dispose();
                        }
                    }
                }
            }
        }
        private void OpenImage_DoubleClick(object sender, EventArgs e)
        {
            // تم إزالة سطر التأخير من هنا
            PictureBox pb = sender as PictureBox;
            if (pb?.Tag != null)
            {
                string filePath = pb.Tag.ToString();
                try
                {
                    // التأكد من وجود الملف قبل محاولة فتحه
                    if (File.Exists(filePath))
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = filePath,
                            UseShellExecute = true // مهم لفتح الملف بالبرنامج الافتراضي
                        };
                        System.Diagnostics.Process.Start(psi);
                    }
                    else
                    {
                        MessageBox.Show("الملف المحدد لم يعد موجوداً في المسار:\n" + filePath, "ملف غير موجود", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ في فتح الملف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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
                    pb.DoubleClick += OpenImage_DoubleClick;
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
                            pb.DoubleClick += OpenImage_DoubleClick;
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
                    pbOther.DoubleClick += OpenImage_DoubleClick;
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
            // *** إضافة جديدة: منع السحب إلى نفس المصدر ***
            if (e.Data.GetDataPresent("DragSource"))
            {
                string dragSource = e.Data.GetData("DragSource").ToString();
                FlowLayoutPanel targetPanel = sender as FlowLayoutPanel;

                // إذا كان المصدر والهدف هما نفس الـ FlowLayoutPanel، امنع السحب
                if (dragSource == targetPanel?.Name)
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }
            }

            // السماح بالسحب في الحالات الأخرى
            if (e.Data.GetDataPresent(DataFormats.StringFormat) ||
                e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetDataPresent("ReturnToPanel1"))
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
                bool isInternalDrag = e.Data.GetDataPresent(DataFormats.StringFormat);

                // التعامل مع السحب من imageApostille
                if (e.Data.GetDataPresent("ReturnToPanel1") && e.Data.GetDataPresent("FromImageApostille"))
                {
                    PictureBox sourceApostille = (PictureBox)e.Data.GetData("ReturnToPanel1");
                    if (sourceApostille?.Tag != null)
                    {
                        string apostilleFilePath = sourceApostille.Tag.ToString();

                        // البحث عن PictureBox فارغ في flowLayoutPanel1
                        PictureBox targetPic = FindEmptyPictureBox();

                        if (targetPic == null)
                        {
                            // إنشاء PictureBox جديد إذا لم يوجد فارغ
                            targetPic = CreateNewPictureBox();
                            flowLayoutPanel1.Controls.Add(targetPic);
                        }

                        // نقل الصورة من imageApostille إلى flowLayoutPanel1
                        using (var imgTemp = Image.FromFile(apostilleFilePath))
                        {
                            if (targetPic.Image != null)
                                targetPic.Image.Dispose();

                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = apostilleFilePath;
                        }

                        // مسح الصورة من imageApostille
                        sourceApostille.Image?.Dispose();
                        sourceApostille.Image = null;
                        sourceApostille.Tag = null;

                        return;
                    }
                }

                // باقي الكود الأصلي للتعامل مع الحالات الأخرى...
                if (e.Data.GetDataPresent("DragSource"))
                {
                    string dragSource = e.Data.GetData("DragSource").ToString();
                    FlowLayoutPanel targetPanel = sender as FlowLayoutPanel;

                    if (dragSource == targetPanel?.Name)
                    {
                        return;
                    }
                }

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
                        PictureBox targetPic = FindEmptyPictureBox();

                        if (targetPic == null)
                        {
                            targetPic = CreateNewPictureBox();
                            flowLayoutPanel1.Controls.Add(targetPic);
                        }

                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            if (targetPic.Image != null)
                                targetPic.Image.Dispose();

                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

                        if (e.Data.GetDataPresent(DataFormats.StringFormat))
                        {
                            RemoveImageFromPanel1(filePath);
                            PictureBox apostilleBox = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
                            if (apostilleBox != null && apostilleBox.Tag?.ToString() == filePath)
                            {
                                apostilleBox.Image?.Dispose();
                                apostilleBox.Image = null;
                                apostilleBox.Tag = null;
                            }
                            RemoveImageFromFlowLayoutPanel(flowLayoutPanel2, filePath);
                        }
                    }
                }

                ClearSelection();
                dragSourcePanel = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while processing drag and drop:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }        // البحث عن PictureBox فارغ في flowLayoutPanel1
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
            pic.BorderStyle = BorderStyle.None;
            pic.SizeMode = PictureBoxSizeMode.StretchImage;
            pic.BackColor = Color.Transparent; // لون خلفية شفاف

            pic.Margin = new Padding(5);
            pic.AllowDrop = true;

            // إضافة أحداث السحب والإفلات
            pic.DragEnter += Pic_DragEnter_FlowPanel;
            pic.DragDrop += Pic_DragDrop_FlowPanel;

            // إضافة أحداث إعادة الترتيب
            pic.MouseDown += Pic_MouseDown_FlowPanel;
            pic.MouseMove += Pic_MouseMove_FlowPanel;
            pic.MouseUp += Pic_MouseUp_FlowPanel;

            // *** إضافة جديدة: أحداث التحديد المتعدد ***
            pic.Click += Pic_Click_FlowPanel1;
            pic.DoubleClick += OpenImage_DoubleClick;

            return pic;
        }
        private void Pic_Click_FlowPanel1(object sender, EventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null || pb.Image == null) return;

            if ((DateTime.Now - lastClickTime).TotalMilliseconds < SystemInformation.DoubleClickTime)
            {
                return;
            }
            lastClickTime = DateTime.Now; // سجل وقت هذه النقرة.

            // التحقق من الضغط على مفتاح Ctrl للتحديد المتعدد
            if (Control.ModifierKeys == Keys.Control)
            {
                isMultiSelectModeFlow1 = true;
                if (selectedPictureBoxesFlow1.Contains(pb))
                {
                    // إلغاء التحديد
                    selectedPictureBoxesFlow1.Remove(pb);
                    pb.BorderStyle = BorderStyle.None;
                    pb.BackColor = Color.Transparent;
                }
                else
                {
                    // إضافة للتحديد
                    selectedPictureBoxesFlow1.Add(pb);
                    pb.BorderStyle = BorderStyle.FixedSingle;
                    pb.BackColor = Color.LightBlue;
                }
            }
            else
            {
                // إلغاء التحديد السابق
                ClearSelectionFlow1();
                // تحديد الصورة الحالية
                if (pb.Image != null)
                {
                    selectedPictureBoxesFlow1.Add(pb);
                    pb.BorderStyle = BorderStyle.FixedSingle;
                    pb.BackColor = Color.LightBlue;
                }
                isMultiSelectModeFlow1 = false;
            }
        }
        private void ClearSelectionFlow1()
        {
            foreach (var pb in selectedPictureBoxesFlow1)
            {
                pb.BorderStyle = BorderStyle.None; // إزالة الحدود
                pb.BackColor = Color.Transparent; // إزالة لون التحديد
            }
            selectedPictureBoxesFlow1.Clear();
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

                        // تعديل: إرسال بيانات مع تحديد المصدر
                        DataObject dataObject = new DataObject();
                        dataObject.SetData("FlowPanel2Reorder", draggedPictureBoxPanel2); // لإعادة الترتيب الداخلي
                        dataObject.SetData("ReturnToPanel1", draggedPictureBoxPanel2);      // للإرجاع إلى panel1

                        // *** إضافة جديدة: تحديد مصدر السحب ***
                        dataObject.SetData("DragSource", dragSourcePanel?.Name ?? "unknown");

                        // إرسال مسار الملف للسماح بالنقل إلى حاوية أخرى
                        if (draggedPictureBoxPanel2.Tag != null)
                        {
                            dataObject.SetData(DataFormats.StringFormat, draggedPictureBoxPanel2.Tag.ToString());
                        }

                        draggedPictureBoxPanel2.DoDragDrop(dataObject, DragDropEffects.Move);
                    }
                }
            }
        }

        // أحداث السحب والإفلات في flowLayoutPanel1
        private void Pic_DragEnter_FlowPanel(object sender, DragEventArgs e)
        {
            // *** إضافة جديدة: منع السحب داخل نفس الـ FlowLayoutPanel ***
            if (e.Data.GetDataPresent("DragSource"))
            {
                string dragSource = e.Data.GetData("DragSource").ToString();
                PictureBox targetPic = sender as PictureBox;
                FlowLayoutPanel targetPanel = targetPic?.Parent as FlowLayoutPanel;

                // إذا كان المصدر والهدف في نفس الـ FlowLayoutPanel
                if (dragSource == targetPanel?.Name)
                {
                    // السماح فقط بإعادة الترتيب (FlowPanelReorder)
                    if (e.Data.GetDataPresent("FlowPanelReorder"))
                    {
                        e.Effect = DragDropEffects.Move;
                    }
                    else
                    {
                        e.Effect = DragDropEffects.None; // منع إنشاء نسخ مكررة
                    }
                    return;
                }
            }

            // السماح بالسحب في الحالات الأخرى
            if (e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetDataPresent(DataFormats.StringFormat) ||
                e.Data.GetDataPresent("FlowPanelReorder") ||
                e.Data.GetDataPresent("ReturnToPanel1"))
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
                string filePath = null;

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

                // التعامل مع التبديل من imageApostille
                if (e.Data.GetDataPresent("ReturnToPanel1"))
                {
                    PictureBox sourceApostille = (PictureBox)e.Data.GetData("ReturnToPanel1");
                    if (sourceApostille != null && sourceApostille.Name == "imageApostille")
                    {
                        // إذا كان targetPic يحتوي على صورة، قم بالتبديل
                        if (targetPic.Image != null)
                        {
                            SwapImagesBetweenControls(targetPic, sourceApostille);
                        }
                        else
                        {
                            // نقل عادي إلى PictureBox فارغ
                            if (sourceApostille.Image != null && sourceApostille.Tag != null)
                            {
                                filePath = sourceApostille.Tag.ToString();
                                using (var imgTemp = Image.FromFile(filePath))
                                {
                                    targetPic.Image = new Bitmap(imgTemp);
                                    targetPic.Tag = filePath;
                                }

                                // مسح الصورة من imageApostille
                                sourceApostille.Image?.Dispose();
                                sourceApostille.Image = null;
                                sourceApostille.Tag = null;
                            }
                        }
                        return;
                    }
                }

                // التعامل مع السحب العادي (من panel1، flowLayoutPanel2، ملفات خارجية)
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                        filePath = files[0];
                }
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    filePath = data.Split('|')[0];
                }

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    string ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        // *** التعديل الرئيسي: التحقق من مصدر السحب للتبديل ***
                        bool isFromFlowLayoutPanel2 = false;
                        PictureBox sourcePictureBox = null;

                        // البحث عن مصدر الصورة في flowLayoutPanel2
                        if (e.Data.GetDataPresent(DataFormats.StringFormat))
                        {
                            sourcePictureBox = FindPictureBoxInFlowPanel(flowLayoutPanel2, filePath);
                            if (sourcePictureBox != null)
                            {
                                isFromFlowLayoutPanel2 = true;
                            }
                        }

                        // إذا كان المصدر من flowLayoutPanel2 والهدف يحتوي على صورة، قم بالتبديل
                        if (isFromFlowLayoutPanel2 && sourcePictureBox != null && targetPic.Image != null)
                        {
                            SwapImagesBetweenControls(sourcePictureBox, targetPic);
                        }
                        else
                        {
                            // نقل عادي (للملفات الخارجية أو الهدف فارغ)
                            using (var imgTemp = Image.FromFile(filePath))
                            {
                                if (targetPic.Image != null)
                                    targetPic.Image.Dispose();

                                targetPic.Image = new Bitmap(imgTemp);
                                targetPic.Tag = filePath;
                            }

                            // حذف من المصدر فقط في حالات معينة (ليس من flowLayoutPanel2 إذا كان الهدف يحتوي على صورة)
                            if (e.Data.GetDataPresent(DataFormats.StringFormat) && !isFromFlowLayoutPanel2)
                            {
                                RemoveImageFromPanel1(filePath);
                                PictureBox apostilleBox = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
                                if (apostilleBox != null && apostilleBox.Tag?.ToString() == filePath)
                                {
                                    apostilleBox.Image?.Dispose();
                                    apostilleBox.Image = null;
                                    apostilleBox.Tag = null;
                                }
                            }
                            // إذا كان من flowLayoutPanel2 لكن الهدف فارغ، احذف من المصدر
                            else if (isFromFlowLayoutPanel2 && targetPic.Image == null)
                            {
                                RemoveImageFromFlowLayoutPanel(flowLayoutPanel2, filePath);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading the image:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private PictureBox FindPictureBoxInFlowPanel(FlowLayoutPanel panel, string filePath)
        {
            foreach (Control control in panel.Controls)
            {
                if (control is PictureBox pb && pb.Tag != null && pb.Tag.ToString() == filePath)
                {
                    return pb;
                }
            }
            return null;
        }


        // دالة جديدة لتبديل الصور بين أي عنصرين
        private void SwapImagesBetweenControls(PictureBox control1, PictureBox control2)
        {
            try
            {
                // حفظ بيانات الصورة الأولى
                Image image1 = control1.Image;
                object tag1 = control1.Tag;

                // حفظ بيانات الصورة الثانية
                Image image2 = control2.Image;
                object tag2 = control2.Tag;

                // تبديل الصور
                control1.Image = image2;
                control1.Tag = tag2;

                control2.Image = image1;
                control2.Tag = tag1;

                // لا نحتاج لـ Dispose هنا لأننا فقط ننقل المراجع
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error swapping images: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                // *** إضافة جديدة: تحديد مصدر السحب ***
                if (draggedPictureBox != null)
                {
                    dragSourcePanel = draggedPictureBox.Parent as FlowLayoutPanel;
                }
            }
        }


        // الحل الأول: استخدام DataObject لحمل البيانات المخصصة

        private void Pic_MouseUp_FlowPanel(object sender, MouseEventArgs e)
        {
            isDragging = false;
            draggedPictureBox = null;
            dragSourcePanel = null; // *** إضافة جديدة: مسح مصدر السحب ***
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

            // --- الجزء الذي تم تعديله ---
            // ربط الأحداث الصحيحة التي تسمح بالسحب المتعدد والسحب للخارج
            pb.DoubleClick += OpenImage_DoubleClick;
            pb.MouseDown += Pb_MouseDown_Panel1; //  مهم للسحب
            pb.MouseMove += Pb_MouseMove_Panel1; //  مهم للسحب
            pb.MouseUp += Pb_MouseUp_Panel1;   //  مهم للسحب
            pb.Click += Pb_Click_Panel1;       //  مهم للتحديد الفردي والمتعدد
                                               // ----------------------------

            panel1.Controls.Add(pb);
            panel1.Controls.SetChildIndex(pb, 0); // ضعه في بداية القائمة

            ReArrangeImages();
        }

        // إضافة قائمة سياق للتحكم في PictureBox الجديد (اختيارية)


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

            UpdateExistingPictureBoxEvents();



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
            // *** إضافة جديدة: منع السحب إلى نفس المصدر ***
            if (e.Data.GetDataPresent("DragSource"))
            {
                string dragSource = e.Data.GetData("DragSource").ToString();
                FlowLayoutPanel targetPanel = sender as FlowLayoutPanel;

                // إذا كان المصدر والهدف هما نفس الـ FlowLayoutPanel، امنع السحب
                if (dragSource == targetPanel?.Name)
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }
            }

            // السماح بالسحب في الحالات الأخرى
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
                bool isInternalDrag = e.Data.GetDataPresent(DataFormats.StringFormat);
                bool isMultiDrag = e.Data.GetDataPresent("MultiDragFlow1");

                // التعامل مع السحب من imageApostille (كما هو)
                if (e.Data.GetDataPresent("ReturnToPanel1") && e.Data.GetDataPresent("FromImageApostille"))
                {
                    PictureBox sourceApostille = (PictureBox)e.Data.GetData("ReturnToPanel1");
                    if (sourceApostille?.Tag != null)
                    {
                        string apostilleFilePath = sourceApostille.Tag.ToString();
                        PictureBox targetPic = FindEmptyPictureBoxInPanel2();

                        if (targetPic == null)
                        {
                            targetPic = CreateNewPictureBoxForPanel2();
                            flowLayoutPanel2.Controls.Add(targetPic);
                        }

                        using (var imgTemp = Image.FromFile(apostilleFilePath))
                        {
                            if (targetPic.Image != null)
                                targetPic.Image.Dispose();

                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = apostilleFilePath;
                        }

                        sourceApostille.Image?.Dispose();
                        sourceApostille.Image = null;
                        sourceApostille.Tag = null;
                        return;
                    }
                }

                // التحقق من مصدر السحب
                if (e.Data.GetDataPresent("DragSource"))
                {
                    string dragSource = e.Data.GetData("DragSource").ToString();
                    FlowLayoutPanel targetPanel = sender as FlowLayoutPanel;
                    if (dragSource == targetPanel?.Name)
                        return;
                }

                // التعامل مع الملفات الخارجية
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    filePaths.AddRange(files);
                }
                // التعامل مع السحب الداخلي
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    filePaths.AddRange(data.Split('|'));
                }

                // *** معالجة كل ملف مع وضعه في مكان فارغ متتالي ***
                foreach (string filePath in filePaths)
                {
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) continue;

                    string ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        // البحث عن أول PictureBox فارغ
                        PictureBox targetPic = FindEmptyPictureBoxInPanel2();

                        if (targetPic == null)
                        {
                            targetPic = CreateNewPictureBoxForPanel2();
                            flowLayoutPanel2.Controls.Add(targetPic);
                        }

                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            targetPic.Image?.Dispose();
                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

                        // حذف من المصدر في حالة السحب الداخلي
                        if (isInternalDrag)
                        {
                            RemoveImageFromPanel1(filePath);
                            PictureBox apostilleBox = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
                            if (apostilleBox != null && apostilleBox.Tag?.ToString() == filePath)
                            {
                                apostilleBox.Image?.Dispose();
                                apostilleBox.Image = null;
                                apostilleBox.Tag = null;
                            }
                            RemoveImageFromFlowLayoutPanel(flowLayoutPanel1, filePath);
                        }
                    }
                }

                // *** مسح التحديد بعد السحب المتعدد ***
                if (isMultiDrag)
                {
                    ClearSelectionFlow1();
                }

                ClearSelection();
                dragSourcePanel = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while processing drag and drop:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
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

            pic.DoubleClick += OpenImage_DoubleClick;


            return pic;
        }

        // سادساً: أحداث السحب والإفلات في PictureBox داخل flowLayoutPanel2
        private void Pic_DragEnter_Panel2(object sender, DragEventArgs e)
        {
            // *** إضافة جديدة: منع السحب داخل نفس الـ FlowLayoutPanel ***
            if (e.Data.GetDataPresent("DragSource"))
            {
                string dragSource = e.Data.GetData("DragSource").ToString();
                PictureBox targetPic = sender as PictureBox;
                FlowLayoutPanel targetPanel = targetPic?.Parent as FlowLayoutPanel;

                // إذا كان المصدر والهدف في نفس الـ FlowLayoutPanel
                if (dragSource == targetPanel?.Name)
                {
                    // السماح فقط بإعادة الترتيب (FlowPanel2Reorder)
                    if (e.Data.GetDataPresent("FlowPanel2Reorder"))
                    {
                        e.Effect = DragDropEffects.Move;
                    }
                    else
                    {
                        e.Effect = DragDropEffects.None; // منع إنشاء نسخ مكررة
                    }
                    return;
                }
            }

            // السماح بالسحب في الحالات الأخرى
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
                string filePath = null;

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

                // التعامل مع التبديل من imageApostille
                if (e.Data.GetDataPresent("ReturnToPanel1"))
                {
                    PictureBox sourceApostille = (PictureBox)e.Data.GetData("ReturnToPanel1");
                    if (sourceApostille != null && sourceApostille.Name == "imageApostille")
                    {
                        // إذا كان targetPic يحتوي على صورة، قم بالتبديل
                        if (targetPic.Image != null)
                        {
                            SwapImagesBetweenControls(targetPic, sourceApostille);
                        }
                        else
                        {
                            // نقل عادي إلى PictureBox فارغ
                            if (sourceApostille.Image != null && sourceApostille.Tag != null)
                            {
                                filePath = sourceApostille.Tag.ToString();
                                using (var imgTemp = Image.FromFile(filePath))
                                {
                                    targetPic.Image = new Bitmap(imgTemp);
                                    targetPic.Tag = filePath;
                                }

                                // مسح الصورة من imageApostille
                                sourceApostille.Image?.Dispose();
                                sourceApostille.Image = null;
                                sourceApostille.Tag = null;
                            }
                        }
                        return;
                    }
                }

                // التعامل مع السحب العادي (من panel1، flowLayoutPanel1، ملفات خارجية)
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                        filePath = files[0];
                }
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    filePath = data.Split('|')[0];
                }

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    string ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        // *** التعديل الرئيسي: التحقق من مصدر السحب للتبديل ***
                        bool isFromFlowLayoutPanel1 = false;
                        PictureBox sourcePictureBox = null;

                        // البحث عن مصدر الصورة في flowLayoutPanel1
                        if (e.Data.GetDataPresent(DataFormats.StringFormat))
                        {
                            sourcePictureBox = FindPictureBoxInFlowPanel(flowLayoutPanel1, filePath);
                            if (sourcePictureBox != null)
                            {
                                isFromFlowLayoutPanel1 = true;
                            }
                        }

                        // إذا كان المصدر من flowLayoutPanel1 والهدف يحتوي على صورة، قم بالتبديل
                        if (isFromFlowLayoutPanel1 && sourcePictureBox != null && targetPic.Image != null)
                        {
                            SwapImagesBetweenControls(sourcePictureBox, targetPic);
                        }
                        else
                        {
                            // نقل عادي (للملفات الخارجية أو الهدف فارغ)
                            using (var imgTemp = Image.FromFile(filePath))
                            {
                                if (targetPic.Image != null)
                                    targetPic.Image.Dispose();

                                targetPic.Image = new Bitmap(imgTemp);
                                targetPic.Tag = filePath;
                            }

                            // حذف من المصدر فقط في حالات معينة (ليس من flowLayoutPanel1 إذا كان الهدف يحتوي على صورة)
                            if (e.Data.GetDataPresent(DataFormats.StringFormat) && !isFromFlowLayoutPanel1)
                            {
                                RemoveImageFromPanel1(filePath);
                                // محاولة الحذف من imageApostille
                                PictureBox apostilleBox = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
                                if (apostilleBox != null && apostilleBox.Tag?.ToString() == filePath)
                                {
                                    apostilleBox.Image?.Dispose();
                                    apostilleBox.Image = null;
                                    apostilleBox.Tag = null;
                                }
                            }
                            // إذا كان من flowLayoutPanel1 لكن الهدف فارغ، احذف من المصدر
                            else if (isFromFlowLayoutPanel1 && targetPic.Image == null)
                            {
                                RemoveImageFromFlowLayoutPanel(flowLayoutPanel1, filePath);
                            }
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

                // *** إضافة جديدة: تحديد مصدر السحب ***
                if (draggedPictureBoxPanel2 != null)
                {
                    dragSourcePanel = draggedPictureBoxPanel2.Parent as FlowLayoutPanel;
                }
            }
        }


        private void Pic_MouseUp_Panel2(object sender, MouseEventArgs e)
        {
            isDragging = false;
            draggedPictureBoxPanel2 = null;
            dragSourcePanel = null; // *** إضافة جديدة: مسح مصدر السحب ***
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
                // *** تعديل 8: إضافة حدث النقر المزدوج لفتح الصورة ***
                imageApostille.DoubleClick += OpenImage_DoubleClick;

                // *** إضافة 9: إضافة أحداث السحب (لإرجاع الصورة إلى panel1) ***
                imageApostille.MouseDown += imageApostille_MouseDown;
                imageApostille.MouseMove += imageApostille_MouseMove;
                imageApostille.MouseUp += imageApostille_MouseUp;
            }
            // إضافة قائمة سياق
        
        }
        private void imageApostille_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStartPoint = e.Location;
                isDragging = false;
            }
        }

        private void imageApostille_MouseMove(object sender, MouseEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (e.Button == MouseButtons.Left && pb != null && pb.Image != null)
            {
                if (!isDragging)
                {
                    Size dragSize = SystemInformation.DragSize;
                    Rectangle dragRect = new Rectangle(dragStartPoint, dragSize);
                    if (!dragRect.Contains(e.Location))
                    {
                        isDragging = true;

                        DataObject data = new DataObject();

                        // بيانات لإمكانية الإرجاع إلى panel1 والتبديل مع flowLayoutPanel1
                        data.SetData("ReturnToPanel1", pb);

                        // بيانات للسحب إلى flowLayoutPanel1 (للتبديل)
                        if (pb.Tag != null)
                        {
                            data.SetData(DataFormats.StringFormat, pb.Tag.ToString());
                        }

                        // إضافة معرف خاص لـ imageApostille
                        data.SetData("FromImageApostille", true);

                        pb.DoDragDrop(data, DragDropEffects.Move);
                    }
                }
            }
        }

        private void imageApostille_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
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
        // تحديث حدث DragDrop لـ imageApostille لدعم التبديل مع flowLayoutPanel2 أيضاً
        private void ImageApostille_DragDrop(object sender, DragEventArgs e)
        {
            PictureBox targetPic = sender as PictureBox;
            if (targetPic == null) return;

            string filePath = null;
            PictureBox sourcePictureBox = null;

            try
            {
                bool isInternalDrag = e.Data.GetDataPresent(DataFormats.StringFormat);
                bool isFromFlowPanel1 = false;
                bool isFromFlowPanel2 = false;

                // التعامل مع الملفات الخارجية
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        filePath = files[0];
                    }
                }
                // التعامل مع السحب الداخلي
                else if (isInternalDrag)
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    if (!string.IsNullOrEmpty(data))
                    {
                        filePath = data.Split('|')[0];

                        // البحث عن مصدر الصورة في flowLayoutPanel1
                        sourcePictureBox = FindPictureBoxInFlowPanel(flowLayoutPanel1, filePath);
                        if (sourcePictureBox != null)
                        {
                            isFromFlowPanel1 = true;
                        }
                        else
                        {
                            // البحث عن مصدر الصورة في flowLayoutPanel2
                            sourcePictureBox = FindPictureBoxInFlowPanel(flowLayoutPanel2, filePath);
                            if (sourcePictureBox != null)
                            {
                                isFromFlowPanel2 = true;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    string ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        // إذا كان السحب من flowLayoutPanel1 أو flowLayoutPanel2 وكان imageApostille يحتوي على صورة
                        if ((isFromFlowPanel1 || isFromFlowPanel2) && sourcePictureBox != null && targetPic.Image != null)
                        {
                            // تبديل الصور
                            SwapImagesBetweenControls(sourcePictureBox, targetPic);
                        }
                        else
                        {
                            // نقل عادي (بدون تبديل)
                            using (var imgTemp = Image.FromFile(filePath))
                            {
                                targetPic.Image?.Dispose();
                                targetPic.Image = new Bitmap(imgTemp);
                                targetPic.Tag = filePath;
                            }

                            // حذف من المصدر في حالة النقل العادي
                            if (isInternalDrag)
                            {
                                RemoveImageFromPanel1(filePath);
                                RemoveImageFromFlowLayoutPanel(flowLayoutPanel1, filePath);
                                RemoveImageFromFlowLayoutPanel(flowLayoutPanel2, filePath);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please select a valid image file (jpg, png, bmp, gif).",
                            "Unsupported File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading the image:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void panel1_MouseDown_ForSelection(object sender, MouseEventArgs e)
        {
            // ابدأ التحديد فقط إذا تم الضغط على زر الفأرة الأيسر
            // وعلى مساحة فارغة في panel1 (وليس على PictureBox)
            if (e.Button == MouseButtons.Left)
            {
                // التحقق مما إذا كان النقر فوق عنصر تحكم فرعي (صورة)
                Control clickedControl = panel1.GetChildAtPoint(e.Location);
                if (clickedControl != null && clickedControl is PictureBox)
                {
                    // إذا نقرت على صورة، دع منطق النقر على الصورة يعمل
                    // ولا تبدأ رسم المستطيل
                    isSelecting = false;
                    return;
                }

                isSelecting = true;
                selectionStartPoint = e.Location;

                // إذا لم يكن مفتاح Ctrl مضغوطًا، قم بإلغاء التحديد الحالي
                if (Control.ModifierKeys != Keys.Control)
                {
                    ClearSelection();
                }

                // إعادة رسم اللوحة لإظهار التغييرات
                panel1.Invalidate();
            }
        }

        private void panel1_MouseMove_ForSelection(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                // حساب أبعاد المستطيل أثناء تحريك الفأرة
                int x = Math.Min(selectionStartPoint.X, e.X);
                int y = Math.Min(selectionStartPoint.Y, e.Y);
                int width = Math.Abs(selectionStartPoint.X - e.X);
                int height = Math.Abs(selectionStartPoint.Y - e.Y);
                selectionRectangle = new Rectangle(x, y, width, height);

                // طلب إعادة رسم اللوحة لإظهار المستطيل المحدث
                panel1.Invalidate();
            }
        }

        private void panel1_MouseUp_ForSelection(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                isSelecting = false;

                // المرور على كل الصور في panel1
                foreach (Control control in panel1.Controls)
                {
                    if (control is PictureBox pb)
                    {
                        // التحقق مما إذا كانت حدود الصورة تتقاطع مع مستطيل التحديد
                        if (selectionRectangle.IntersectsWith(pb.Bounds))
                        {
                            // إذا تقاطعت، قم بتحديد الصورة
                            if (!selectedPictureBoxes.Contains(pb))
                            {
                                selectedPictureBoxes.Add(pb);
                                pb.BorderStyle = BorderStyle.FixedSingle;
                            }
                        }
                    }
                }

                // طلب إعادة رسم اللوحة لإخفاء المستطيل
                panel1.Invalidate();
            }
        }

        private void panel1_Paint_SelectionRectangle(object sender, PaintEventArgs e)
        {
            // إذا كنا في وضع التحديد، قم برسم المستطيل
            if (isSelecting)
            {
                // استخدام لون شفاف جزئيًا لتعبئة المستطيل
                using (Brush brush = new SolidBrush(Color.FromArgb(70, 0, 120, 215))) // لون أزرق شفاف
                {
                    e.Graphics.FillRectangle(brush, selectionRectangle);
                }
                // استخدام قلم لرسم حدود المستطيل
                using (Pen pen = new Pen(Color.DodgerBlue, 1))
                {
                    e.Graphics.DrawRectangle(pen, selectionRectangle);
                }
            }
        }

        // ***************************************************************
        // دالة مساعدة جديدة لحذف الصورة من أي FlowLayoutPanel بناءً على مسار الملف
        private void RemoveImageFromFlowLayoutPanel(FlowLayoutPanel panel, string filePath)
        {
            PictureBox pbToRemove = null;
            foreach (Control control in panel.Controls)
            {
                if (control is PictureBox pb && pb.Tag != null && pb.Tag.ToString() == filePath)
                {
                    pbToRemove = pb;
                    break;
                }
            }

            if (pbToRemove != null)
            {
                // إزالة من قائمة التحديد إذا كانت محددة
                if (selectedPictureBoxesFlow1.Contains(pbToRemove))
                {
                    selectedPictureBoxesFlow1.Remove(pbToRemove);
                }

                panel.Controls.Remove(pbToRemove);
                pbToRemove.Image?.Dispose();
                pbToRemove.Dispose();
            }
        }
        // ***************************************************************
        private void UpdateExistingPictureBoxEvents()
        {
            // تطبيق الأحداث على جميع PictureBox الموجودة في flowLayoutPanel1
            foreach (Control control in flowLayoutPanel1.Controls)
            {
                if (control is PictureBox pb)
                {
                    // إزالة الأحداث القديمة إذا كانت موجودة لتجنب التكرار
                    pb.Click -= Pic_Click_FlowPanel1;

                    // إضافة الأحداث الجديدة
                    pb.Click += Pic_Click_FlowPanel1;
                }
            }
        }
             // ***************************************************************

    }


}

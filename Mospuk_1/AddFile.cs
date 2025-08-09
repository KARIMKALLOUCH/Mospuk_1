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
using Org.BouncyCastle.Asn1.Cms;
using System.Threading.Tasks;  // ← هذه المكتبة مهمة للـ async/await

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
        private List<PictureBox> selectedPictureBoxesFlow2 = new List<PictureBox>(); // للتحديد المتعدد في flowLayoutPanel2
        private bool isMultiSelectModeFlow2 = false; // وضع التحديد المتعدد
        private Point dragStartPoint;
        private bool isDragging = false;
        private List<PictureBox> selectedPictureBoxes = new List<PictureBox>(); // للتحديد المتعدد
        private bool isMultiSelectMode = false; // وضع التحديد المتعدد
        private int currentUserId; // إضافة متغير لتخزين معرف المستخدم الحالي

        public AddFile(MySqlDatabase database, int userId)
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
            this.AcceptButton = null;
            currentUserId = userId; // حفظ معرف المستخدم


            panel1.MouseDown += Pb_MouseDown;
            SetupImageApostille();
            SetupFlowLayoutPanel1();
            SetupFlowLayoutPanel2();     // إضافة تهيئة flowLayoutPanel2 مع دعم إعادة الترتيب

            this.Click += EmptySpace_Click;
            panel1.Click += EmptySpace_Click;
            flowLayoutPanel1.Click += EmptySpace_Click;
            flowLayoutPanel2.Click += EmptySpace_Click;
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
                // إنشاء قائمة بجميع الصور المحددة من جميع الأماكن
                List<PictureBox> allSelectedPictures = new List<PictureBox>();
                allSelectedPictures.AddRange(selectedPictureBoxesFlow1);
                allSelectedPictures.AddRange(selectedPictureBoxesFlow2);
                allSelectedPictures.AddRange(selectedPictureBoxes);

                // إضافة صورة imageApostille إذا كانت محددة
                PictureBox apostilleBox = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
                if (apostilleBox != null && apostilleBox.Image != null && apostilleBox.BorderStyle == BorderStyle.FixedSingle)
                {
                    allSelectedPictures.Add(apostilleBox);
                }

                // إذا كان هناك أي صور محددة، عرض رسالة تأكيد
                if (allSelectedPictures.Count > 0)  
                {
                    var confirmResult = MessageBox.Show(
                        $"Are you sure you want to delete {allSelectedPictures.Count} selected images?",
                        "Confirm Deletion",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirmResult == DialogResult.Yes)
                    {
                        // حذف الصور المحددة من جميع الأماكن
                        foreach (var pb in allSelectedPictures.ToList())
                        {
                            if (pb.Parent == flowLayoutPanel1)
                            {
                                flowLayoutPanel1.Controls.Remove(pb);
                                pb.Dispose();
                            }
                            else if (pb.Parent == flowLayoutPanel2)
                            {
                                flowLayoutPanel2.Controls.Remove(pb);
                                pb.Dispose();
                            }
                            else if (pb.Parent == panel1)
                            {
                                panel1.Controls.Remove(pb);
                                pb.Dispose();
                                ReArrangeImages(); // إعادة ترتيب الصور المتبقية
                            }
                            else if (pb.Name == "imageApostille")
                            {
                                pb.Image?.Dispose();
                                pb.Image = null;
                                pb.Tag = null;
                                pb.BorderStyle = BorderStyle.None;
                            }
                        }

                        // مسح جميع قوائم التحديد
                        selectedPictureBoxesFlow1.Clear();
                        selectedPictureBoxesFlow2.Clear();
                        selectedPictureBoxes.Clear();
                    }
                }
            }
            else if (e.KeyCode == Keys.Enter)
            {

                if (AreAnyImagesSelected())
                {
                    OpenSelectedImages();
                }
                else
                {
                    btnUplaod.PerformClick();
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
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
            // أولاً: التعامل مع السحب المتعدد (هنا لا يوجد تبديل، فقط إعادة الصور)
            // هذا الجزء يبقى كما هو
            if (e.Data.GetDataPresent("MultiDragFlow1"))
            {
                PictureBox[] selectedPictureBoxes = (PictureBox[])e.Data.GetData("MultiDragFlow1");
                foreach (PictureBox sourcePb in selectedPictureBoxes)
                {
                    if (sourcePb?.Tag != null && sourcePb.Image != null)
                    {
                        AddImageBackToPanel1(sourcePb.Tag.ToString());
                    }
                }
                foreach (PictureBox sourcePb in selectedPictureBoxes)
                {
                    if (sourcePb.Parent != null) sourcePb.Parent.Controls.Remove(sourcePb);
                    sourcePb.Dispose();
                }
                ClearSelectionFlow1();
                return; // الخروج بعد المعالجة
            }

            if (e.Data.GetDataPresent("MultiDragFlow2"))
            {
                PictureBox[] selectedPictureBoxes = (PictureBox[])e.Data.GetData("MultiDragFlow2");
                foreach (PictureBox sourcePb in selectedPictureBoxes)
                {
                    if (sourcePb?.Tag != null && sourcePb.Image != null)
                    {
                        AddImageBackToPanel1(sourcePb.Tag.ToString());
                    }
                }
                foreach (PictureBox sourcePb in selectedPictureBoxes)
                {
                    if (sourcePb.Parent != null) sourcePb.Parent.Controls.Remove(sourcePb);
                    sourcePb.Dispose();
                }
                ClearSelectionFlow2();
                return; // الخروج بعد المعالجة
            }


            if (e.Data.GetDataPresent("ReturnToPanel1"))
            {
                PictureBox sourcePb = (PictureBox)e.Data.GetData("ReturnToPanel1");
                if (sourcePb == null || sourcePb.Tag == null) return;

                Point dropPoint = panel1.PointToClient(new Point(e.X, e.Y));

                PictureBox targetPb = panel1.GetChildAtPoint(dropPoint) as PictureBox;

                if (targetPb != null && targetPb != sourcePb)
                {
                    SwapImagesBetweenControls(sourcePb, targetPb);
                }
                else
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

            string downloadsPath = db.GetSavedPathById(currentUserId, "downloads");

            if (string.IsNullOrEmpty(downloadsPath) || !Directory.Exists(downloadsPath))
            {
                MessageBox.Show("Please set a downloads directory first.", "Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All Supported Files|*.rar;*.zip;*.jpg;*.jpeg;*.png;*.pdf;*.docx;*.xlsx|RAR Files|*.rar|ZIP Files|*.zip|Images|*.jpg;*.jpeg;*.png|PDF Files|*.pdf|All Files|*.*";
            ofd.Multiselect = true;

            // تعيين المسار الابتدائي لمجلد التنزيلات
            ofd.InitialDirectory = downloadsPath;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string outputFolder = Path.Combine(Application.StartupPath, "ExtractedFiles");

                if (Directory.Exists(outputFolder))
                    Directory.Delete(outputFolder, true);

                Directory.CreateDirectory(outputFolder);

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
                        File.Copy(path, Path.Combine(outputFolder, Path.GetFileName(path)), true);
                    }
                }

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
            using (var archive = ArchiveFactory.Open(zipPath))
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
        // دالة Pb_Click_Panel1 المعدّلة
        private void Pb_Click_Panel1(object sender, EventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null) return;

            if ((DateTime.Now - lastClickTime).TotalMilliseconds < SystemInformation.DoubleClickTime)
            {
                return;
            }
            lastClickTime = DateTime.Now;
            // *** نهاية الإضافة ***

            if (Control.ModifierKeys == Keys.Control)
            {
                // لا تمسح التحديدات الأخرى في وضع Ctrl
                if (selectedPictureBoxes.Contains(pb))
                {
                    selectedPictureBoxes.Remove(pb);
                    pb.BorderStyle = BorderStyle.None;
                }
                else
                {
                    selectedPictureBoxes.Add(pb);
                    pb.BorderStyle = BorderStyle.FixedSingle;
                }
            }
            else
            {
                // استخدم الدالة الجديدة
                ClearAllSelections();

                // تحديد الصورة الحالية
                selectedPictureBoxes.Add(pb);
                pb.BorderStyle = BorderStyle.FixedSingle;
            }
            panel1.Focus();

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
            // إضافة الأحداث الجديدة للتحديد بواسطة المستطيل
            flowLayoutPanel1.MouseDown += FlowLayoutPanel1_MouseDown_ForSelection;
            flowLayoutPanel1.MouseMove += FlowLayoutPanel1_MouseMove_ForSelection;
            flowLayoutPanel1.MouseUp += FlowLayoutPanel1_MouseUp_ForSelection;
            flowLayoutPanel1.Paint += FlowLayoutPanel1_Paint_SelectionRectangle;
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

                // *** إضافة جديدة: التعامل مع السحب المتعدد من flowLayoutPanel2 ***
                if (e.Data.GetDataPresent("MultiDragFlow2"))
                {
                    PictureBox[] selectedPictureBoxes = (PictureBox[])e.Data.GetData("MultiDragFlow2");

                    foreach (PictureBox sourcePb in selectedPictureBoxes)
                    {
                        if (sourcePb?.Tag != null && sourcePb.Image != null)
                        {
                            string filePath = sourcePb.Tag.ToString();

                            // البحث عن PictureBox فارغ في flowLayoutPanel1
                            PictureBox targetPic = FindEmptyPictureBox();

                            if (targetPic == null)
                            {
                                targetPic = CreateNewPictureBox();
                                flowLayoutPanel1.Controls.Add(targetPic);
                            }

                            // نقل الصورة
                            using (var imgTemp = Image.FromFile(filePath))
                            {
                                if (targetPic.Image != null)
                                    targetPic.Image.Dispose();

                                targetPic.Image = new Bitmap(imgTemp);
                                targetPic.Tag = filePath;
                            }
                        }
                    }

                    // إزالة جميع الصور المحددة من flowLayoutPanel2
                    foreach (PictureBox sourcePb in selectedPictureBoxes)
                    {
                        if (sourcePb.Parent != null)
                        {
                            sourcePb.Parent.Controls.Remove(sourcePb);
                        }
                        sourcePb.Dispose();
                    }

                    // مسح التحديد
                    ClearSelectionFlow2();
                    return;
                }

                // باقي الكود الأصلي للحالات الأخرى...
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
        }
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
            lastClickTime = DateTime.Now;

            if (Control.ModifierKeys == Keys.Control)
            {
                // لا تمسح التحديدات الأخرى في وضع Ctrl
                if (selectedPictureBoxesFlow1.Contains(pb))
                {
                    selectedPictureBoxesFlow1.Remove(pb);
                    pb.BorderStyle = BorderStyle.None;
                    pb.BackColor = Color.Transparent;
                }
                else
                {
                    selectedPictureBoxesFlow1.Add(pb);
                    pb.BorderStyle = BorderStyle.FixedSingle;
                    pb.BackColor = Color.LightBlue;
                }
            }
            else
            {
                // *** التعديل هنا: استخدم الدالة الجديدة ***
                ClearAllSelections();

                // تحديد الصورة الحالية
                if (pb.Image != null)
                {
                    selectedPictureBoxesFlow1.Add(pb);
                    pb.BorderStyle = BorderStyle.FixedSingle;
                    pb.BackColor = Color.LightBlue;
                }
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

                        // *** تعديل جديد: دعم السحب المتعدد من flowLayoutPanel2 ***
                        DataObject dataObject = new DataObject();
                        List<string> filePathsToTransfer = new List<string>();

                        // إذا كانت الصورة المسحوبة ضمن التحديد المتعدد وهناك أكثر من صورة محددة
                        if (selectedPictureBoxesFlow2.Count > 1 && selectedPictureBoxesFlow2.Contains(draggedPictureBoxPanel2))
                        {
                            // سحب جميع الصور المحددة
                            foreach (var selectedPb in selectedPictureBoxesFlow2)
                            {
                                if (selectedPb.Tag != null && selectedPb.Image != null)
                                {
                                    filePathsToTransfer.Add(selectedPb.Tag.ToString());
                                }
                            }
                            // إضافة معرف خاص للسحب المتعدد من flowLayoutPanel2
                            dataObject.SetData("MultiDragFlow2", selectedPictureBoxesFlow2.ToArray());
                        }
                        else
                        {
                            // سحب الصورة الحالية فقط
                            if (draggedPictureBoxPanel2.Tag != null)
                            {
                                filePathsToTransfer.Add(draggedPictureBoxPanel2.Tag.ToString());
                            }
                        }

                        // إعداد البيانات للسحب
                        dataObject.SetData("FlowPanel2Reorder", draggedPictureBoxPanel2); // لإعادة الترتيب الداخلي
                        dataObject.SetData("ReturnToPanel1", draggedPictureBoxPanel2);      // للإرجاع إلى panel1
                        dataObject.SetData("DragSource", dragSourcePanel?.Name ?? "unknown");

                        // إرسال مسارات الملفات
                        if (filePathsToTransfer.Count > 0)
                        {
                            string dataToTransfer = string.Join("|", filePathsToTransfer);
                            dataObject.SetData(DataFormats.StringFormat, dataToTransfer);
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

            // التعامل أولاً مع إعادة الترتيب الداخلي
            if (e.Data.GetDataPresent("FlowPanelReorder"))
            {
                PictureBox sourcePic = (PictureBox)e.Data.GetData("FlowPanelReorder");
                if (sourcePic != targetPic)
                {
                    SwapPictureBoxes(sourcePic, targetPic);
                }
                return;
            }

            string filePath = null;
            PictureBox sourcePictureBox = null;

            try
            {
                // --- استخلاص مسار الملف المصدر ---
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) // من ملف خارجي
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0) filePath = files[0];
                }
                else if (e.Data.GetDataPresent(DataFormats.StringFormat)) // من داخل البرنامج
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    filePath = data.Split('|')[0];

                    // البحث عن مصدر الصورة في الحاويات الأخرى
                    sourcePictureBox = FindPictureBoxInFlowPanel(flowLayoutPanel2, filePath); // البحث في اللوحة الثانية
                    if (sourcePictureBox == null)
                    {
                        PictureBox apostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
                        if (apostille?.Tag?.ToString() == filePath) sourcePictureBox = apostille; // البحث في أبوستيل
                    }
                    if (sourcePictureBox == null)
                    {
                        foreach (Control c in panel1.Controls) // البحث في اللوحة الرئيسية
                        {
                            if (c is PictureBox pb && pb.Tag?.ToString() == filePath)
                            {
                                sourcePictureBox = pb;
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

                // --- تطبيق منطق التبديل أو النقل ---
                string ext = Path.GetExtension(filePath).ToLower();
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                {
                    // *** تعديل: التحقق من وجود صورة في الهدف ومصدر داخلي للتبديل ***
                    if (sourcePictureBox != null && targetPic.Image != null)
                    {
                        SwapImagesBetweenControls(sourcePictureBox, targetPic);
                    }
                    else
                    {
                        // النقل العادي إلى الهدف
                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            targetPic.Image?.Dispose();
                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

                        // حذف من المصدر في حالة النقل
                        if (sourcePictureBox != null)
                        {
                            // إذا كان المصدر هو أبوستيل، فقط امسح محتواه
                            if (sourcePictureBox.Name == "imageApostille")
                            {
                                sourcePictureBox.Image = null;
                                sourcePictureBox.Tag = null;
                            }
                            else // إذا كان من لوحة أخرى، قم بإزالته بالكامل
                            {
                                RemoveImageFromFlowLayoutPanel(flowLayoutPanel2, filePath);
                                RemoveImageFromPanel1(filePath);
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
        // دالة جديدة لتبديل الصور بين أي عنصرين
        private void SwapImagesBetweenControls(PictureBox control1, PictureBox control2)
        {
            try
            {
                Image image1 = control1.Image;
                object tag1 = control1.Tag;

                Image image2 = control2.Image;
                object tag2 = control2.Tag;

                control1.Image = image2;
                control1.Tag = tag2;

                control2.Image = image1;
                control2.Tag = tag1;

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
            if (!flowLayoutPanel1.Controls.OfType<PictureBox>().Any())
            {
                MessageBox.Show("Please upload at least one translation image.", "No Images", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.ActiveControl = null; 

                return; // إيقاف العملية لأن لا توجد صور أساسية

            }
            // 1. التحقق من اختيار العميل

            if (Company_Client.SelectedItem == null)
            {
                MessageBox.Show("Please select a client or company.", "Input Error");           
                    Company_Client.Focus();
                return; // إيقاف العملية
            }

            // 2. التحقق من اختيار نوع الوثيقة
            if (!(comboDocumentType.SelectedItem is KeyValuePair<int, string> selectedDocTypePair))
            {
                MessageBox.Show("Please select a document type.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboDocumentType.Focus();
                return; // إيقاف العملية
            }

            // 3. التحقق من اختيار نوع الترجمة
            if (!(comboTranslation.SelectedItem is KeyValuePair<int, string> selectedTranslationPair))
            {
                MessageBox.Show("Please select a translation type.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboTranslation.Focus();
                return; // إيقاف العملية
            }
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

            string documentType = selectedDocTypePair.Value;
            string translationType = selectedTranslationPair.Value;

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
                    CleanWorkspace();
                    this.ActiveControl = null;

                }
                else
                {
                    MessageBox.Show("⚠️ Project saved but some images failed to save.");
                    this.ActiveControl = null;

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

            string projectFolder = db.GetSavedPathById(currentUserId, "save");
            if (string.IsNullOrEmpty(projectFolder))
            {
                MessageBox.Show("Please set a save directory first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            // إنشاء مجلد للمشروع إذا لم يكن موجوداً
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

                        if (imageCounter == 1)
                        {
                            if (!string.IsNullOrWhiteSpace(txtnotes.Text))
                            {
                                string rawNote = txtnotes.Text.Trim();
                                string sanitizedNote = new string(rawNote.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray()).Replace(" ", "_");
                                if (sanitizedNote.Length > 30)
                                    sanitizedNote = sanitizedNote.Substring(0, 30);
                                imageName += $"------------------------{sanitizedNote}----------------------";
                            }
                            else
                            {
                                imageName += "--------------------------------------------------------------";
                            }
                        }

                        string originalPath = pb.Tag.ToString();
                        string extension = Path.GetExtension(originalPath);
                        string fullImageName = imageName + extension;
                        string imagePath = Path.Combine(projectFolder, fullImageName);

                        if (File.Exists(originalPath))
                        {
                            File.Copy(originalPath, imagePath, true);
                        }
                        else
                        {
                            pb.Image.Save(imagePath);
                        }

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
                        else
                        {
                            // زيادة العداد فقط عند النجاح
                            imageCounter++;
                        }
                    }
                    catch (Exception ex)
                    {
                        allSaved = false;
                        MessageBox.Show($"Error saving image {imageCounter}: {ex.Message}");
                    }
                }
            }

            // 2. حفظ صورة imageApostille
            PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            if (imageApostille != null && imageApostille.Image != null && imageApostille.Tag != null)
            {
                try
                {
                    string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Apostille";
                    string originalPath = imageApostille.Tag.ToString();
                    string extension = Path.GetExtension(originalPath);
                    string fullImageName = imageName + extension;
                    string imagePath = Path.Combine(projectFolder, fullImageName);

                    if (File.Exists(originalPath))
                    {
                        File.Copy(originalPath, imagePath, true);
                    }
                    else
                    {
                        imageApostille.Image.Save(imagePath);
                    }

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
                    else
                    {
                        imageCounter++;
                    }
                }
                catch (Exception ex)
                {
                    allSaved = false;
                    MessageBox.Show($"Error saving Apostille image: {ex.Message}");
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
                        string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_{attachmentType}";
                        string originalPath = pb.Tag.ToString();
                        string extension = Path.GetExtension(originalPath);
                        string fullImageName = imageName + extension;
                        string imagePath = Path.Combine(projectFolder, fullImageName);

                        if (File.Exists(originalPath))
                        {
                            File.Copy(originalPath, imagePath, true);
                        }
                        else
                        {
                            pb.Image.Save(imagePath);
                        }

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
                        else
                        {
                            imageCounter++;
                        }
                    }
                    catch (Exception ex)
                    {
                        allSaved = false;
                        MessageBox.Show($"Error saving attachment image {imageCounter}: {ex.Message}");
                    }
                }
            }

            // ================== بداية التعديل المطلوب ==================

            // 4. حجز رقم لملف OCR وزيادة العداد الرئيسي فوراً
            int ocrFileNumber = imageCounter;
            imageCounter++;

            // 5. حفظ ملفات Word المرفوعة (OCR) باستخدام الرقم المحجوز
            foreach (Control control in panelDocx.Controls)
            {
                if (control is Label lbl && lbl.Tag != null)
                {
                    try
                    {
                        string wordFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{ocrFileNumber}_OCR";
                        string originalPath = lbl.Tag.ToString();
                        string extension = Path.GetExtension(originalPath);
                        string fullWordFileName = wordFileName + extension;
                        string wordPath = Path.Combine(projectFolder, fullWordFileName);

                        if (File.Exists(originalPath))
                        {
                            File.Copy(originalPath, wordPath, true);

                            string insertWordQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) 
                                      VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";
                            List<MySqlParameter> wordParameters = new List<MySqlParameter>
                    {
                        new MySqlParameter("@project_id", projectId),
                        new MySqlParameter("@image_name", fullWordFileName),
                        new MySqlParameter("@image_path", wordPath),
                        new MySqlParameter("@attachment_type", "WORD")
                    };
                            bool wordSaved = db.ExecuteNonQuery(insertWordQuery, wordParameters);
                            if (!wordSaved)
                            {
                                allSaved = false;
                                MessageBox.Show($"Failed to save Word file: {fullWordFileName}");
                            }
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
                        MessageBox.Show($"Error saving Word file {ocrFileNumber}: {ex.Message}");
                    }
                }
            }

            // 6. إنشاء ملف Word جديد بإسم Google Driver
            try
            {
                string googleDriverFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Google Driver.docx";
                string googleDriverPath = Path.Combine(projectFolder, googleDriverFileName);
                using (var fs = File.Create(googleDriverPath)) { }

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
                else
                {
                    imageCounter++;
                }
            }
            catch (Exception ex)
            {
                allSaved = false;
                MessageBox.Show($"❌ Error creating Google Driver file {imageCounter}: {ex.Message}");
            }

            // 7. إنشاء ملف Word جديد بإسم Traducción Preliminar
            try
            {
                string traduccionFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Traducción Preliminar.docx";
                string traduccionPath = Path.Combine(projectFolder, traduccionFileName);
                using (var fs = File.Create(traduccionPath)) { }

                string insertTraduccionQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) 
                  VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";
                List<MySqlParameter> traduccionParams = new List<MySqlParameter>
        {
            new MySqlParameter("@project_id", projectId),
            new MySqlParameter("@image_name", traduccionFileName),
            new MySqlParameter("@image_path", traduccionPath),
            new MySqlParameter("@attachment_type", "Traducción Preliminar")
        };
                bool traduccionSaved = db.ExecuteNonQuery(insertTraduccionQuery, traduccionParams);
                if (!traduccionSaved)
                {
                    allSaved = false;
                    MessageBox.Show($"❌ Failed to save Traducción Preliminar file: {traduccionFileName}");
                }
                else
                {
                    imageCounter++;
                }
            }
            catch (Exception ex)
            {
                allSaved = false;
                MessageBox.Show($"❌ Error creating Traducción Preliminar file {imageCounter}: {ex.Message}");
            }

            // 8. إنشاء ملف Word جديد بإسم Informe revisión
            try
            {
                string informeFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Informe revisión.docx";
                string informePath = Path.Combine(projectFolder, informeFileName);
                using (var fs = File.Create(informePath)) { }

                string insertInformeQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) 
          VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";
                List<MySqlParameter> informeParams = new List<MySqlParameter>
        {
            new MySqlParameter("@project_id", projectId),
            new MySqlParameter("@image_name", informeFileName),
            new MySqlParameter("@image_path", informePath),
            new MySqlParameter("@attachment_type", "Informe revisión")
        };
                bool informeSaved = db.ExecuteNonQuery(insertInformeQuery, informeParams);
                if (!informeSaved)
                {
                    allSaved = false;
                    MessageBox.Show($"❌ Failed to save Informe revisión file: {informeFileName}");
                }
                else
                {
                    imageCounter++;
                }
            }
            catch (Exception ex)
            {
                allSaved = false;
                MessageBox.Show($"❌ Error creating Informe revisión file {imageCounter}: {ex.Message}");
            }

            // =================== نهاية التعديل المطلوب ===================

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

         

            UpdateExistingPictureBoxEvents();
            Reception_Date.Value = DateTime.Now;

            LoadClientsAndCompanies();
            Time.Value = DateTime.Now; // تعيين الوقت الحالي
            LoadDocumentTypesToComboBox();
            LoadLanguagePairsToComboBox();

        }


        private void btnAddWord_Click(object sender, EventArgs e)
        {
            string projectFolder = db.GetSavedPathById(currentUserId, "archive");
            if (string.IsNullOrEmpty(projectFolder))
            {
                MessageBox.Show("Please set a save directory first.", "Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            OpenFileDialog ofd = new OpenFileDialog();
            // يمكنك جعل المسار الابتدائي هو مسار الحفظ الذي تم تحديده
            ofd.InitialDirectory = projectFolder; // بدلاً من المسار الثابت
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
            // إضافة الأحداث الجديدة للتحديد بواسطة المستطيل
            flowLayoutPanel2.MouseDown += FlowLayoutPanel2_MouseDown_ForSelection;
            flowLayoutPanel2.MouseMove += FlowLayoutPanel2_MouseMove_ForSelection;
            flowLayoutPanel2.MouseUp += FlowLayoutPanel2_MouseUp_ForSelection;
            flowLayoutPanel2.Paint += FlowLayoutPanel2_Paint_SelectionRectangle;

        }
        private void FlowLayoutPanel2_MouseDown_ForSelection(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Control clickedControl = flowLayoutPanel2.GetChildAtPoint(e.Location);
                if (clickedControl != null && clickedControl is PictureBox)
                {
                    isSelecting = false;
                    return;
                }

                isSelecting = true;
                selectionStartPoint = e.Location;

                if (Control.ModifierKeys != Keys.Control)
                {
                    ClearSelectionFlow2();
                }

                flowLayoutPanel2.Invalidate();
            }
        }

        private void FlowLayoutPanel2_MouseMove_ForSelection(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                int x = Math.Min(selectionStartPoint.X, e.X);
                int y = Math.Min(selectionStartPoint.Y, e.Y);
                int width = Math.Abs(selectionStartPoint.X - e.X);
                int height = Math.Abs(selectionStartPoint.Y - e.Y);
                selectionRectangle = new Rectangle(x, y, width, height);
                flowLayoutPanel2.Invalidate();
            }
        }

        private void FlowLayoutPanel2_MouseUp_ForSelection(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                isSelecting = false;

                foreach (Control control in flowLayoutPanel2.Controls)
                {
                    if (control is PictureBox pb)
                    {
                        if (selectionRectangle.IntersectsWith(pb.Bounds))
                        {
                            if (!selectedPictureBoxesFlow2.Contains(pb))
                            {
                                selectedPictureBoxesFlow2.Add(pb);
                                pb.BorderStyle = BorderStyle.FixedSingle;
                                pb.BackColor = Color.LightBlue;
                            }
                        }
                    }
                }

                flowLayoutPanel2.Invalidate();
            }
        }
        private void ClearSelectionFlow2()
        {
            foreach (var pb in selectedPictureBoxesFlow2)
            {
                pb.BorderStyle = BorderStyle.None;
                pb.BackColor = Color.Transparent;
            }
            selectedPictureBoxesFlow2.Clear();
        }
        private void Pic_Click_FlowPanel2(object sender, EventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null || pb.Image == null) return;

            if ((DateTime.Now - lastClickTime).TotalMilliseconds < SystemInformation.DoubleClickTime)
            {
                return;
            }
            lastClickTime = DateTime.Now;

            if (Control.ModifierKeys == Keys.Control)
            {
                // لا تمسح التحديدات الأخرى في وضع Ctrl
                if (selectedPictureBoxesFlow2.Contains(pb))
                {
                    selectedPictureBoxesFlow2.Remove(pb);
                    pb.BorderStyle = BorderStyle.None;
                    pb.BackColor = Color.Transparent;
                }
                else
                {
                    selectedPictureBoxesFlow2.Add(pb);
                    pb.BorderStyle = BorderStyle.FixedSingle;
                    pb.BackColor = Color.LightBlue;
                }
            }
            else
            {
                // *** التعديل هنا: استخدم الدالة الجديدة ***
                ClearAllSelections();

                if (pb.Image != null)
                {
                    selectedPictureBoxesFlow2.Add(pb);
                    pb.BorderStyle = BorderStyle.FixedSingle;
                    pb.BackColor = Color.LightBlue;
                }
            }
        }

        private void FlowLayoutPanel2_Paint_SelectionRectangle(object sender, PaintEventArgs e)
        {
            if (isSelecting)
            {
                using (Brush brush = new SolidBrush(Color.FromArgb(70, 0, 120, 215)))
                {
                    e.Graphics.FillRectangle(brush, selectionRectangle);
                }
                using (Pen pen = new Pen(Color.DodgerBlue, 1))
                {
                    e.Graphics.DrawRectangle(pen, selectionRectangle);
                }
            }
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
            pic.BorderStyle = BorderStyle.None;
            pic.SizeMode = PictureBoxSizeMode.StretchImage;
            pic.BackColor = Color.Transparent;
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
            pic.Click += Pic_Click_FlowPanel2;


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

            // التعامل أولاً مع إعادة الترتيب الداخلي
            if (e.Data.GetDataPresent("FlowPanel2Reorder"))
            {
                PictureBox sourcePic = (PictureBox)e.Data.GetData("FlowPanel2Reorder");
                if (sourcePic != targetPic)
                {
                    SwapPictureBoxesPanel2(sourcePic, targetPic);
                }
                return;
            }

            string filePath = null;
            PictureBox sourcePictureBox = null;

            try
            {
                // --- استخلاص مسار الملف المصدر ---
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) // من ملف خارجي
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0) filePath = files[0];
                }
                else if (e.Data.GetDataPresent(DataFormats.StringFormat)) // من داخل البرنامج
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    filePath = data.Split('|')[0];

                    // البحث عن مصدر الصورة في الحاويات الأخرى
                    sourcePictureBox = FindPictureBoxInFlowPanel(flowLayoutPanel1, filePath); // البحث في اللوحة الأولى
                    if (sourcePictureBox == null)
                    {
                        PictureBox apostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
                        if (apostille?.Tag?.ToString() == filePath) sourcePictureBox = apostille; // البحث في أبوستيل
                    }
                    if (sourcePictureBox == null)
                    {
                        foreach (Control c in panel1.Controls) // البحث في اللوحة الرئيسية
                        {
                            if (c is PictureBox pb && pb.Tag?.ToString() == filePath)
                            {
                                sourcePictureBox = pb;
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

                // --- تطبيق منطق التبديل أو النقل ---
                string ext = Path.GetExtension(filePath).ToLower();
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                {
                    // *** تعديل: التحقق من وجود صورة في الهدف ومصدر داخلي للتبديل ***
                    if (sourcePictureBox != null && targetPic.Image != null)
                    {
                        SwapImagesBetweenControls(sourcePictureBox, targetPic);
                    }
                    else
                    {
                        // النقل العادي إلى الهدف
                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            targetPic.Image?.Dispose();
                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

                        // حذف من المصدر في حالة النقل
                        if (sourcePictureBox != null)
                        {
                            if (sourcePictureBox.Name == "imageApostille")
                            {
                                sourcePictureBox.Image = null;
                                sourcePictureBox.Tag = null;
                            }
                            else
                            {
                                RemoveImageFromFlowLayoutPanel(flowLayoutPanel1, filePath);
                                RemoveImageFromPanel1(filePath);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading the image:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }        // سابعاً: أحداث إعادة الترتيب داخل flowLayoutPanel2
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
                imageApostille.BorderStyle = BorderStyle.None;

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
                imageApostille.Click += imageApostille_Click;
              

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

        private void ImageApostille_DragOver(object sender, DragEventArgs e)
        {
            ImageApostille_DragEnter(sender, e);
        }
        private void imageApostille_Click(object sender, EventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb?.Image == null) return; 

            if ((DateTime.Now - lastClickTime).TotalMilliseconds < SystemInformation.DoubleClickTime)
            {
                return;
            }
            lastClickTime = DateTime.Now;

            if (Control.ModifierKeys == Keys.Control)
            {
                if (pb.BorderStyle == BorderStyle.FixedSingle)
                {
                    pb.BorderStyle = BorderStyle.None;
                }
                else
                {
                    pb.BorderStyle = BorderStyle.FixedSingle;
                }
            }
            else
            {
                              bool onlyApostilleIsSelected = pb.BorderStyle == BorderStyle.FixedSingle
                                             && !selectedPictureBoxes.Any()
                                             && !selectedPictureBoxesFlow1.Any()
                                             && !selectedPictureBoxesFlow2.Any();

                if (!onlyApostilleIsSelected)
                {
                    ClearAllSelections();

                    pb.BorderStyle = BorderStyle.FixedSingle;
                }
            }
        }

        private void ImageApostille_DragDrop(object sender, DragEventArgs e)
        {
            PictureBox targetPic = sender as PictureBox;
            if (targetPic == null) return;

            string filePath = null;
            PictureBox sourcePictureBox = null;

            try
            {
                bool isInternalDrag = e.Data.GetDataPresent(DataFormats.StringFormat);

                // --- استخلاص مسار الملف المصدر ---
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) // من ملف خارجي
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0) filePath = files[0];
                }
                else if (isInternalDrag) // من داخل البرنامج
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    if (!string.IsNullOrEmpty(data))
                    {
                        filePath = data.Split('|')[0];
                        // البحث عن مصدر الصورة في الحاويات الأخرى
                        sourcePictureBox = FindPictureBoxInFlowPanel(flowLayoutPanel1, filePath)
                                        ?? FindPictureBoxInFlowPanel(flowLayoutPanel2, filePath);
                        // إذا لم يتم العثور عليه، ابحث في panel1
                        if (sourcePictureBox == null)
                        {
                            foreach (Control c in panel1.Controls)
                            {
                                if (c is PictureBox pb && pb.Tag?.ToString() == filePath)
                                {
                                    sourcePictureBox = pb;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

                // --- تطبيق منطق التبديل أو النقل ---
                string ext = Path.GetExtension(filePath).ToLower();
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                {
                    // *** تعديل: التحقق من وجود صورة في الهدف ومصدر داخلي للتبديل ***
                    if (sourcePictureBox != null && targetPic.Image != null)
                    {
                        // إذا كان هناك صورة في الهدف والمصدر معروف، قم بالتبديل
                        SwapImagesBetweenControls(sourcePictureBox, targetPic);
                    }
                    else
                    {
                        // إذا كان الهدف فارغاً أو المصدر خارجي، قم بالنقل العادي
                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            targetPic.Image?.Dispose();
                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

                        // حذف من المصدر في حالة النقل (وليس التبديل)
                        if (isInternalDrag)
                        {
                            RemoveImageFromPanel1(filePath);
                            RemoveImageFromFlowLayoutPanel(flowLayoutPanel1, filePath);
                            RemoveImageFromFlowLayoutPanel(flowLayoutPanel2, filePath);
                        }
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
        private void FlowLayoutPanel1_MouseDown_ForSelection(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Control clickedControl = flowLayoutPanel1.GetChildAtPoint(e.Location);
                if (clickedControl != null && clickedControl is PictureBox)
                {
                    isSelecting = false;
                    return;
                }

                isSelecting = true;
                selectionStartPoint = e.Location;

                if (Control.ModifierKeys != Keys.Control)
                {
                    ClearSelectionFlow1();
                }

                flowLayoutPanel1.Invalidate();
            }
        }

        private void FlowLayoutPanel1_MouseMove_ForSelection(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                int x = Math.Min(selectionStartPoint.X, e.X);
                int y = Math.Min(selectionStartPoint.Y, e.Y);
                int width = Math.Abs(selectionStartPoint.X - e.X);
                int height = Math.Abs(selectionStartPoint.Y - e.Y);
                selectionRectangle = new Rectangle(x, y, width, height);
                flowLayoutPanel1.Invalidate();
            }
        }

        private void FlowLayoutPanel1_MouseUp_ForSelection(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                isSelecting = false;

                foreach (Control control in flowLayoutPanel1.Controls)
                {
                    if (control is PictureBox pb)
                    {
                        if (selectionRectangle.IntersectsWith(pb.Bounds))
                        {
                            if (!selectedPictureBoxesFlow1.Contains(pb))
                            {
                                selectedPictureBoxesFlow1.Add(pb);
                                pb.BorderStyle = BorderStyle.FixedSingle;
                                pb.BackColor = Color.LightBlue;
                            }
                        }
                    }
                }

                flowLayoutPanel1.Invalidate();
            }
        }

        private void FlowLayoutPanel1_Paint_SelectionRectangle(object sender, PaintEventArgs e)
        {
            if (isSelecting)
            {
                using (Brush brush = new SolidBrush(Color.FromArgb(70, 0, 120, 215)))
                {
                    e.Graphics.FillRectangle(brush, selectionRectangle);
                }
                using (Pen pen = new Pen(Color.DodgerBlue, 1))
                {
                    e.Graphics.DrawRectangle(pen, selectionRectangle);
                }
            }
        }
        private void ClearAllSelections()
        {
            // مسح التحديد من panel1
            foreach (var pb in selectedPictureBoxes)
            {
                pb.BorderStyle = BorderStyle.None;
            }
            selectedPictureBoxes.Clear();

            // مسح التحديد من flowLayoutPanel1
            foreach (var pb in selectedPictureBoxesFlow1)
            {
                pb.BorderStyle = BorderStyle.None;
                pb.BackColor = Color.Transparent;
            }
            selectedPictureBoxesFlow1.Clear();

            // مسح التحديد من flowLayoutPanel2
            foreach (var pb in selectedPictureBoxesFlow2)
            {
                pb.BorderStyle = BorderStyle.None;
                pb.BackColor = Color.Transparent;
            }
            selectedPictureBoxesFlow2.Clear();

            // مسح التحديد من imageApostille
            PictureBox apostilleBox = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            if (apostilleBox != null)
            {
                apostilleBox.BorderStyle = BorderStyle.None;
            }
        }
        private void OpenSelectedImages()
        {
            // جمع كل الصور المحددة من جميع الأماكن
            List<PictureBox> allSelectedPictures = new List<PictureBox>();
            allSelectedPictures.AddRange(selectedPictureBoxesFlow1);
            allSelectedPictures.AddRange(selectedPictureBoxesFlow2);
            allSelectedPictures.AddRange(selectedPictureBoxes);

            // إضافة صورة imageApostille إذا كانت محددة
            PictureBox apostilleBox = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            if (apostilleBox != null && apostilleBox.Image != null && apostilleBox.BorderStyle == BorderStyle.FixedSingle)
            {
                allSelectedPictures.Add(apostilleBox);
            }

            // فتح كل الصور المحددة
            foreach (var pb in allSelectedPictures)
            {
                if (pb?.Tag != null)
                {
                    string filePath = pb.Tag.ToString();
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            var psi = new System.Diagnostics.ProcessStartInfo()
                            {
                                FileName = filePath,
                                UseShellExecute = true
                            };
                            System.Diagnostics.Process.Start(psi);
                        }
                        else
                        {
                            MessageBox.Show("الملف المحدد لم يعد موجوداً في المسار:\n" + filePath,
                                          "ملف غير موجود", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("خطأ في فتح الملف: " + ex.Message,
                                      "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private bool AreAnyImagesSelected()
        {
            // التحقق من قائمة التحديد في panel1
            if (selectedPictureBoxes.Any()) return true;

            // التحقق من قائمة التحديد في flowLayoutPanel1
            if (selectedPictureBoxesFlow1.Any()) return true;

            // التحقق من قائمة التحديد في flowLayoutPanel2
            if (selectedPictureBoxesFlow2.Any()) return true;

            // التحقق من تحديد imageApostille بشكل مباشر
            PictureBox apostilleBox = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            if (apostilleBox != null && apostilleBox.Image != null && apostilleBox.BorderStyle == BorderStyle.FixedSingle)
            {
                return true;
            }

            // إذا وصلنا إلى هنا، فلا يوجد أي تحديد
            return false;
        }
        // دالة جديدة للتعامل مع النقر على المساحات الفارغة
        private void EmptySpace_Click(object sender, EventArgs e)
        {
            // استدعاء الدالة التي تمسح كل التحديدات
            ClearAllSelections();
        }
        private void CleanWorkspace()
        {
            try
            {
                // 1. تنظيف flowLayoutPanel1
                ClearFlowLayoutPanel(flowLayoutPanel1);

                // 2. تنظيف flowLayoutPanel2
                ClearFlowLayoutPanel(flowLayoutPanel2);

                // 3. تنظيف imageApostille
                ClearImageApostille();

                // 4. تنظيف panelDocx
                ClearPanelDocx();

                // 5. مسح جميع التحديدات
                ClearAllSelections();

                // 6. تنظيف الحقول النصية (اختياري)
                ClearFormFields();
                             
            }
            catch (Exception ex)
            {
             
            }
        }
        private void ClearFlowLayoutPanel(FlowLayoutPanel panel)
        {
            List<Control> controlsToRemove = new List<Control>();

            foreach (Control control in panel.Controls)
            {
                if (control is PictureBox pb)
                {
                    controlsToRemove.Add(pb);
                }
            }

            foreach (Control control in controlsToRemove)
            {
                PictureBox pb = control as PictureBox;

                // تنظيف الصورة والموارد
                if (pb.Image != null)
                {
                    pb.Image.Dispose();
                    pb.Image = null;
                }

                // إزالة من الحاوي
                panel.Controls.Remove(pb);

                // تنظيف المتحكم نفسه
                pb.Dispose();
            }
        }

        // *** دالة مساعدة لتنظيف imageApostille ***
        private void ClearImageApostille()
        {
            PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            if (imageApostille != null)
            {
                // تنظيف الصورة
                if (imageApostille.Image != null)
                {
                    imageApostille.Image.Dispose();
                    imageApostille.Image = null;
                }

                // مسح المعرف
                imageApostille.Tag = null;

                // إزالة الحدود
                imageApostille.BorderStyle = BorderStyle.None;
            }
        }

        // *** دالة مساعدة لتنظيف panelDocx ***
        private void ClearPanelDocx()
        {
            List<Control> controlsToRemove = new List<Control>();

            foreach (Control control in panelDocx.Controls)
            {
                if (control is Label lbl)
                {
                    controlsToRemove.Add(lbl);
                }
            }

            foreach (Control control in controlsToRemove)
            {
                panelDocx.Controls.Remove(control);
                control.Dispose();
            }
        }
        private void ClearFormFields()
        {
            // Company_Client.Clear(); // إذا كنت تريد مسح اسم العميل
            // txtnotes.Clear(); // إذا كنت تريد مسح الملاحظات

            Reception_Date.Value = DateTime.Now;

            // إعادة تعيين الوقت (اختياري)
            // Time.Text = DateTime.Now.ToString("HH:mm");
        }
        private void LoadClientsAndCompanies()
        {
            try
            {
                // استعلام واحد لدمج العملاء والشركات
                string query = @"
            SELECT client_id AS id, client_code AS code, 'Client' AS type FROM clients
            UNION ALL
            SELECT company_id AS id, company_code AS code, 'Company' AS type FROM companies
            ORDER BY type, code";

                // جلب البيانات من قاعدة البيانات باستخدام ExecuteQuery بدلاً من GetDataTable
                DataTable combinedData = db.ExecuteQuery(query, null);

                // مسح العناصر الحالية
                Company_Client.Items.Clear();

                // تعبئة ComboBox بالأكواد فقط
                foreach (DataRow row in combinedData.Rows)
                {
                    int entityId = Convert.ToInt32(row["id"]);
                    string entityCode = row["code"].ToString();

                    Company_Client.Items.Add(new KeyValuePair<int, string>(entityId, entityCode));
                }

                // تهيئة خصائص العرض
                Company_Client.DisplayMember = "Value";
                Company_Client.ValueMember = "Key";

                // اختيار أول عنصر إذا كان هناك بيانات
                if (Company_Client.Items.Count > 0)
                {
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ في تحميل العملاء والشركات:\n{ex.Message}",
                              "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Console.WriteLine($"Error in LoadClientsAndCompanies: {ex.ToString()}");
            }
        }
        private void LoadDocumentTypesToComboBox()
        {
            try
            {
                // Single query to get all document types
                string query = @"
        SELECT 
            id AS id, 
            name AS code,
            'Document Type' AS type 
        FROM document_types 
        ORDER BY type, code";

                // Get data from database using ExecuteQuery
                DataTable documentTypesData = db.ExecuteQuery(query, null);

                // Clear current items
                comboDocumentType.Items.Clear();

                // Fill ComboBox with document types
                foreach (DataRow row in documentTypesData.Rows)
                {
                    int documentTypeId = Convert.ToInt32(row["id"]);
                    string documentTypeName = row["code"].ToString();

                    comboDocumentType.Items.Add(new KeyValuePair<int, string>(documentTypeId, documentTypeName));
                }

                // Configure display properties
                comboDocumentType.DisplayMember = "Value";
                comboDocumentType.ValueMember = "Key";

                // Select first item if data exists
                if (comboDocumentType.Items.Count > 0)
                {
                    // Optional: Keep this empty if you don't want auto-selection
                    // comboDocumentType.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading document types:\n{ex.Message}",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Console.WriteLine($"Error in LoadDocumentTypesToComboBox: {ex.ToString()}");
            }
        }
        private void LoadLanguagePairsToComboBox()
        {
            try
            {
                // Single query to get all language pairs
                string query = @"
        SELECT 
            id AS id, 
            name AS code,
            'Language Pair' AS type 
        FROM language_pairs 
        ORDER BY type, code";

                // Get data from database using ExecuteQuery
                DataTable languagePairsData = db.ExecuteQuery(query, null);

                // Clear current items
                comboTranslation.Items.Clear();

                // Fill ComboBox with language pairs
                foreach (DataRow row in languagePairsData.Rows)
                {
                    int languagePairId = Convert.ToInt32(row["id"]);
                    string languagePairName = row["code"].ToString();

                    comboTranslation.Items.Add(new KeyValuePair<int, string>(languagePairId, languagePairName));
                }

                // Configure display properties
                comboTranslation.DisplayMember = "Value";
                comboTranslation.ValueMember = "Key";

                // Select first item if data exists
                if (comboTranslation.Items.Count > 0)
                {
                    // Optional: Keep this empty if you don't want auto-selection
                    // comboTranslation.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading language pairs:\n{ex.Message}",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Console.WriteLine($"Error in LoadLanguagePairsToComboBox: {ex.ToString()}");
            }
        }
    }


}

using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;

using System.Windows.Forms;
using PdfiumViewer;
using System.Data.SQLite;       

namespace Mospuk_1
{

    public partial class AddFile : Form

    {
        private Rectangle selectionRectangle;      
        private Point selectionStartPoint;        
        private bool isSelecting = false;         
        private FlowLayoutPanel dragSourcePanel = null; 
        private List<PictureBox> selectedPictureBoxesFlow1 = new List<PictureBox>(); 
        private DateTime lastClickTime = DateTime.MinValue;
        private List<PictureBox> selectedPictureBoxesFlow2 = new List<PictureBox>();
        private Point dragStartPoint;
        private bool isDragging = false;
        private List<PictureBox> selectedPictureBoxes = new List<PictureBox>(); 
        SQLiteDatabase db;
        private int _insertionIndex = -1; 
        private readonly Pen _insertionLinePen = new Pen(Color.DodgerBlue, 2); 
        public AddFile(SQLiteDatabase database)
        {
            InitializeComponent();
            db = database;
            try
            {
                string tempFolder = Path.Combine(Application.StartupPath, "ExtractedFiles");
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
                Directory.CreateDirectory(tempFolder); 
            }
            catch (Exception ex)
            {
            }

            panel1.AutoScroll = true; // AutoScroll ON

            panel1.Padding = new Padding(10);
            // *** إضافة 2: تفعيل استقبال السحب والإفلات في panel1 ***
            panel1.AllowDrop = true;
            panel1.DragEnter += panel1_DragEnter;
            panel1.DragDrop += panel1_DragDrop;

            this.KeyPreview = true; // *** أضف هذا السطر الهام ***
            this.KeyDown += AddFile_KeyDown; // *** أضف هذا السطر لربط الحدث ***
            this.AcceptButton = null;


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
        private void AddFile_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _insertionLinePen.Dispose();

                string tempFolder = Path.Combine(Application.StartupPath, "ExtractedFiles");
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
            catch
            {
            }
        }
        private void AddFile_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                // إنشاء قائمة بجميع الصور المحددة من جميع الأماكن
                List<PictureBox> allSelectedPictures = new List<PictureBox>();
                allSelectedPictures.AddRange(selectedPictureBoxesFlow1);
                allSelectedPictures.AddRange(selectedPictureBoxesFlow2);
                allSelectedPictures.AddRange(selectedPictureBoxes); //  <<-- الآن imageApostille ستكون هنا إذا كانت محددة

                // *** تم حذف الكتلة البرمجية القديمة التي كانت تتحقق من BorderStyle ***

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
                                selectedPictureBoxesFlow1.Remove(pb); // ضمان الحذف من القائمة
                                pb.Dispose();
                            }
                            else if (pb.Parent == flowLayoutPanel2)
                            {
                                flowLayoutPanel2.Controls.Remove(pb);
                                selectedPictureBoxesFlow2.Remove(pb); // ضمان الحذف من القائمة
                                pb.Dispose();
                            }
                            else if (pb.Parent == panel1)
                            {
                                panel1.Controls.Remove(pb);
                                selectedPictureBoxes.Remove(pb); // ضمان الحذف من القائمة
                                pb.Dispose();
                                ReArrangeImages(); // إعادة ترتيب الصور المتبقية
                            }
                            else if (pb.Name == "imageApostille")
                            {
                                pb.Image?.Dispose();
                                pb.Image = null;
                                pb.Tag = null;
                                // لم نعد نستخدم BorderStyle، لذا لا حاجة لتغييره هنا
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

            string downloadsPath = db.GetSavedPathById( "downloads");

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

                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

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
        
            var existingFilePaths = new HashSet<string>(
                panel1.Controls.OfType<PictureBox>()
                               .Where(pb => pb.Tag != null)
                               .Select(pb => pb.Tag.ToString())
            );
            int padding = 10;
            int maxWidth = 120;
            int maxHeight = 120;
            int itemsPerRow = Math.Max(1, (panel1.ClientSize.Width - padding) / (maxWidth + padding));
            // نحدد كم عدد الصور القديمة بالفعل لحساب موضع الصور الجديدة
            int count = panel1.Controls.Count;
            int x = padding + (count % itemsPerRow) * (maxWidth + padding);
            int y = padding + (count / itemsPerRow) * (maxHeight + padding);
            string[] allFilesInDirectory = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

            var newFilesToDisplay = allFilesInDirectory.Where(file => !existingFilePaths.Contains(file));
            foreach (string file in newFilesToDisplay)
            {
                // الكود الداخلي للتعامل مع كل نوع ملف يبقى كما هو
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
                    pb.MouseDown += Pb_MouseDown_Panel1;
                    pb.MouseMove += Pb_MouseMove_Panel1;
                    pb.MouseUp += Pb_MouseUp_Panel1;
                    pb.Click += Pb_Click_Panel1;
                    pb.Paint += PictureBox_Paint_Selection;

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
                            int dpi = 600;
                            for (int i = 0; i < document.PageCount; i++)
                            {
                                string imagePath = Path.Combine(pdfImagesDir, $"Page_{i + 1}.png");

                                // --- بداية التعديل: التحقق من أن صورة الـ PDF غير معروضة أيضاً ---
                                if (!existingFilePaths.Contains(imagePath))
                                {
                                    if (!File.Exists(imagePath))
                                    {
                                        using (var image = document.Render(i, dpi, dpi, true))
                                        {
                                            image.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                                        }
                                    }
                                }
                                // --- نهاية التعديل ---
                            }
                        }

                        // --- بداية التعديل: نعيد فلترة صور الـ PDF أيضاً ---
                        string[] pdfImages = Directory.GetFiles(pdfImagesDir, "*.png");
                        var newPdfImages = pdfImages.Where(imgFile => !existingFilePaths.Contains(imgFile));

                        foreach (var imgFile in newPdfImages)
                        {
                            // --- نهاية التعديل ---
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
                            pb.Paint += PictureBox_Paint_Selection;

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
                        // هذا الكود سيعرض أيقونة PDF في حال فشل الاستخراج
                        PictureBox pbPdf = new PictureBox();
                        pbPdf.Width = maxWidth;
                        pbPdf.Height = maxHeight;
                        pbPdf.BackColor = Color.White;
                        pbPdf.Paint += (s, e_paint) =>
                        {
                            e_paint.Graphics.DrawString("PDF", new Font("Arial", 10, FontStyle.Bold),
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
                    // هذا الكود للملفات الأخرى غير الصور والـ PDF
                    PictureBox pbOther = new PictureBox();
                    pbOther.Width = maxWidth;
                    pbOther.Height = maxHeight;
                    pbOther.BackColor = Color.LightGray;
                    pbOther.BorderStyle = BorderStyle.None;
                    pbOther.Paint += (s, e_paint) =>
                    {
                        e_paint.Graphics.DrawString(Path.GetFileName(file), new Font("Arial", 8),
                            Brushes.Black, new PointF(5, 40));
                    };
                    pbOther.Location = new Point(x, y);
                    pbOther.Tag = file;
                    pbOther.DoubleClick += OpenImage_DoubleClick;

                    pbOther.MouseDown += Pb_MouseDown_Panel1;
                    pbOther.MouseMove += Pb_MouseMove_Panel1;
                    pbOther.MouseUp += Pb_MouseUp_Panel1;
                    pbOther.Click += Pb_Click_Panel1;
                    pbOther.Paint += PictureBox_Paint_Selection;

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

        private void Pb_Click_Panel1(object sender, EventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null) return;

            // منع معالجة النقر المزدوج كأنه نقرتين فرديتين
            if ((DateTime.Now - lastClickTime).TotalMilliseconds < SystemInformation.DoubleClickTime)
            {
                return;
            }
            lastClickTime = DateTime.Now;

            if (Control.ModifierKeys == Keys.Control)
            {
                // Ctrl + النقر: تحديد/إلغاء تحديد فردي (لا يمسح التحديدات السابقة)
                if (selectedPictureBoxes.Contains(pb))
                {
                    selectedPictureBoxes.Remove(pb);
                }
                else
                {
                    selectedPictureBoxes.Add(pb);
                }
                pb.Invalidate(); // إعادة رسم الـ PictureBox لتطبيق التغييرات
            }
            else if (Control.ModifierKeys == Keys.Shift && selectedPictureBoxes.Count > 0)
            {
                // Shift + النقر: تحديد نطاقي (مثل Windows Explorer)
                var allPictures = panel1.Controls.OfType<PictureBox>()
                    .OrderBy(p => p.Location.Y)
                    .ThenBy(p => p.Location.X)
                    .ToList();

                int currentIndex = allPictures.IndexOf(pb);
                int lastSelectedIndex = allPictures.IndexOf(selectedPictureBoxes.Last());

                if (currentIndex != -1 && lastSelectedIndex != -1)
                {
                    // تحديد النطاق بين آخر عنصر محدد والعنصر الحالي
                    int start = Math.Min(currentIndex, lastSelectedIndex);
                    int end = Math.Max(currentIndex, lastSelectedIndex);

                    // إضافة جميع العناصر في النطاق إلى التحديد (بدون مسح التحديدات السابقة)
                    for (int i = start; i <= end; i++)
                    {
                        PictureBox picture = allPictures[i];
                        if (!selectedPictureBoxes.Contains(picture))
                        {
                            selectedPictureBoxes.Add(picture);
                            picture.Invalidate(); // إعادة رسم الصورة
                        }
                    }
                }
            }
            else
            {
                // النقر العادي: تحديد عنصر واحد (يمسح التحديدات السابقة)
                ClearSelection();
                selectedPictureBoxes.Add(pb);
                pb.Invalidate(); // إعادة رسم الصورة المحددة
            }

            panel1.Focus();
        }
        private void ClearSelection()
        {
            // إنشاء قائمة مؤقتة للصور المحددة
            var picturesToClear = new List<PictureBox>(selectedPictureBoxes);

            foreach (var pb in picturesToClear)
            {
                pb.Invalidate(); // إعادة رسم كل صورة لإزالة التحديد
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

            flowLayoutPanel1.DragOver += FlowLayoutPanel1_DragOver;
            flowLayoutPanel1.DragLeave += FlowLayoutPanel1_DragLeave;

            flowLayoutPanel1.MouseDown += FlowLayoutPanel1_MouseDown_ForSelection;
            flowLayoutPanel1.MouseMove += FlowLayoutPanel1_MouseMove_ForSelection;
            flowLayoutPanel1.MouseUp += FlowLayoutPanel1_MouseUp_ForSelection;
            flowLayoutPanel1.Paint += FlowLayoutPanel1_Paint_SelectionRectangle; // سيتم تعديل هذا الحدث
        }
        private void FlowLayoutPanel1_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("FlowPanelReorder"))
            {
                if (_insertionIndex != -1)
                {
                    _insertionIndex = -1;
                    flowLayoutPanel1.Invalidate();
                }
                return;
            }

            e.Effect = DragDropEffects.Move;
            Point clientPoint = flowLayoutPanel1.PointToClient(new Point(e.X, e.Y));

            // حساب جديد يأخذ في الاعتبار اتجاه التدفق
            int newIndex = CalculateInsertionIndex(clientPoint);

            if (newIndex != _insertionIndex)
            {
                _insertionIndex = newIndex;
                flowLayoutPanel1.Invalidate();
            }
        }

        private int CalculateInsertionIndex(Point clientPoint)
        {
            // البحث عن المكان المناسب لإدراج العنصر
            for (int i = 0; i < flowLayoutPanel1.Controls.Count; i++)
            {
                Control c = flowLayoutPanel1.Controls[i];

                // لا تقارن العنصر مع نفسه
                if (c == draggedPictureBox)
                {
                    continue;
                }

                Rectangle bounds = c.Bounds;

                // التحقق بناءً على اتجاه اللوحة
                if (flowLayoutPanel1.FlowDirection == FlowDirection.LeftToRight)
                {
                    // إذا كان مؤشر الفأرة في النصف الأول من العنصر، فالإدراج يجب أن يكون قبله
                    if (clientPoint.X < bounds.Left + (bounds.Width / 2))
                    {
                        return i;
                    }
                }
                else if (flowLayoutPanel1.FlowDirection == FlowDirection.RightToLeft)
                {
                    // في وضع اليمين لليسار، "البداية" هي الحافة اليمنى
                    // إذا كان مؤشر الفأرة في النصف "الأول" (أي الأيمن) من العنصر، فالإدراج قبله
                    if (clientPoint.X > bounds.Right - (bounds.Width / 2))
                    {
                        return i;
                    }
                }
                // يمكن إضافة منطق لـ TopDown و BottomUp إذا احتجت لذلك
                else if (flowLayoutPanel1.FlowDirection == FlowDirection.TopDown)
                {
                    if (clientPoint.Y < bounds.Top + (bounds.Height / 2))
                    {
                        return i;
                    }
                }
            }

            // إذا لم يتم العثور على مكان (المؤشر بعد كل العناصر)، فالإدراج يكون في النهاية
            return flowLayoutPanel1.Controls.Count;
        }
        private void FlowLayoutPanel1_DragLeave(object sender, EventArgs e)
        {
            // إذا خرج السحب من اللوحة، قم بإخفاء الخط
            if (_insertionIndex != -1)
            {
                _insertionIndex = -1;
                flowLayoutPanel1.Invalidate();
            }
        }
        private void FlowLayoutPanel1_DragEnter(object sender, DragEventArgs e)
        {
            // *** تعديل: السماح دائمًا بعمليات إعادة الترتيب الداخلية أولاً ***
            if (e.Data.GetDataPresent("FlowPanelReorder"))
            {
                e.Effect = DragDropEffects.Move;
                return; // مهم للخروج مبكرًا
            }

            // الكود القديم للتحقق من المصدر والهدف
            if (e.Data.GetDataPresent("DragSource"))
            {
                string dragSource = e.Data.GetData("DragSource").ToString();
                FlowLayoutPanel targetPanel = sender as FlowLayoutPanel;

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
        //****************
        private void FlowLayoutPanel1_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                FlowLayoutPanel panel = sender as FlowLayoutPanel;
                if (panel == null) return;

                // *** بداية الإضافة الجديدة: التعامل مع إعادة ترتيب السحب المتعدد ***
                if (e.Data.GetDataPresent("MultiDragFlow1") && e.Data.GetData("DragSource")?.ToString() == panel.Name)
                {
                    var picturesToMove = (PictureBox[])e.Data.GetData("MultiDragFlow1");

                    // الحصول على الصور المحددة بترتيبها الحالي في اللوحة للحفاظ على الترتيب
                    var orderedPicturesToMove = panel.Controls.OfType<PictureBox>()
                                                     .Where(p => picturesToMove.Contains(p))
                                                     .ToList();

                    if (_insertionIndex != -1 && orderedPicturesToMove.Any())
                    {
                        int targetIndex = _insertionIndex;

                        // حساب عدد العناصر التي يتم نقلها من موضع "قبل" نقطة الإدراج
                        // هذا ضروري لضبط فهرس الإدراج بشكل صحيح
                        int shift = orderedPicturesToMove.Count(p => panel.Controls.GetChildIndex(p) < targetIndex);
                        targetIndex -= shift;

                        // التأكد من أن الفهرس ضمن النطاق الصحيح
                        if (targetIndex < 0) targetIndex = 0;

                        // نقل كل صورة إلى الموقع الجديد بالتسلسل
                        foreach (var pic in orderedPicturesToMove)
                        {
                            panel.Controls.SetChildIndex(pic, targetIndex);
                            targetIndex++; // زيادة الفهرس للصورة التالية في المجموعة
                        }
                        panel.PerformLayout();
                    }

                    // تنظيف وإنهاء
                    _insertionIndex = -1;
                    panel.Invalidate();
                    return; // مهم: الخروج بعد معالجة إعادة الترتيب المتعدد
                }
                // *** نهاية الإضافة الجديدة ***


                // التعامل مع إعادة ترتيب عنصر واحد (الكود الأصلي)
                if (e.Data.GetDataPresent("FlowPanelReorder"))
                {
                    PictureBox sourcePic = (PictureBox)e.Data.GetData("FlowPanelReorder");

                    if (sourcePic != null && _insertionIndex != -1)
                    {
                        int sourceIndex = panel.Controls.GetChildIndex(sourcePic);
                        int targetIndex = _insertionIndex;

                        if (sourceIndex < targetIndex)
                        {
                            targetIndex--;
                        }

                        if (targetIndex < 0) targetIndex = 0;
                        if (targetIndex >= panel.Controls.Count) targetIndex = panel.Controls.Count - 1;

                        panel.Controls.SetChildIndex(sourcePic, targetIndex);
                        panel.PerformLayout();
                    }
                    _insertionIndex = -1;
                    panel.Invalidate();
                    return;
                }

                // باقي الكود الأصلي للتعامل مع السحب من المصادر الأخرى يبقى كما هو
                List<string> filePaths = new List<string>();
                bool isInternalDrag = e.Data.GetDataPresent(DataFormats.StringFormat);

                if (e.Data.GetDataPresent("ReturnToPanel1") && e.Data.GetDataPresent("FromImageApostille"))
                {
                    PictureBox sourceApostille = (PictureBox)e.Data.GetData("ReturnToPanel1");
                    if (sourceApostille?.Tag != null)
                    {
                        string apostilleFilePath = sourceApostille.Tag.ToString();
                        PictureBox targetPic = FindEmptyPictureBox();

                        if (targetPic == null)
                        {
                            targetPic = CreateNewPictureBox();
                            flowLayoutPanel1.Controls.Add(targetPic);
                        }

                        using (var imgTemp = Image.FromFile(apostilleFilePath))
                        {
                            targetPic.Image?.Dispose();
                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = apostilleFilePath;
                        }

                        sourceApostille.Image?.Dispose();
                        sourceApostille.Image = null;
                        sourceApostille.Tag = null;
                        selectedPictureBoxes.Remove(sourceApostille); // ضمان إزالته من التحديد
                        return;
                    }
                }

                if (e.Data.GetDataPresent("MultiDragFlow2"))
                {
                    PictureBox[] selectedPictureBoxesFromFlow2 = (PictureBox[])e.Data.GetData("MultiDragFlow2");

                    foreach (PictureBox sourcePb in selectedPictureBoxesFromFlow2)
                    {
                        if (sourcePb?.Tag != null && sourcePb.Image != null)
                        {
                            string filePath = sourcePb.Tag.ToString();
                            PictureBox targetPic = FindEmptyPictureBox();

                            if (targetPic == null)
                            {
                                targetPic = CreateNewPictureBox();
                                flowLayoutPanel1.Controls.Add(targetPic);
                            }

                            using (var imgTemp = Image.FromFile(filePath))
                            {
                                targetPic.Image?.Dispose();
                                targetPic.Image = new Bitmap(imgTemp);
                                targetPic.Tag = filePath;
                            }
                        }
                    }

                    foreach (PictureBox sourcePb in selectedPictureBoxesFromFlow2)
                    {
                        if (sourcePb.Parent != null) sourcePb.Parent.Controls.Remove(sourcePb);
                        sourcePb.Dispose();
                    }
                    ClearSelectionFlow2();
                    return;
                }

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    filePaths.AddRange(files);
                }
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    filePaths.AddRange(data.Split('|'));
                }

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
                            targetPic.Image?.Dispose();
                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

                        if (isInternalDrag)
                        {
                            RemoveImageFromPanel1(filePath);
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
            finally
            {
                if (_insertionIndex != -1)
                {
                    _insertionIndex = -1;
                    flowLayoutPanel1.Invalidate();
                }
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
            pic.Paint += PictureBox_Paint_Selection; // <<<--- تأكد من وجود هذا السطر

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
                if (selectedPictureBoxesFlow1.Contains(pb))
                {
                    selectedPictureBoxesFlow1.Remove(pb);
                }
                else
                {
                    selectedPictureBoxesFlow1.Add(pb);
                }
                pb.Invalidate(); // <<<--- إضافة: إعادة رسم الصورة لتحديث التحديد
            }
            else if (Control.ModifierKeys == Keys.Shift && selectedPictureBoxesFlow1.Count > 0)
            {
                var allPictures = flowLayoutPanel1.Controls.OfType<PictureBox>()
                    .OrderBy(p => p.Location.Y)
                    .ThenBy(p => p.Location.X)
                    .ToList();

                int currentIndex = allPictures.IndexOf(pb);
                int lastSelectedIndex = allPictures.IndexOf(selectedPictureBoxesFlow1.Last());

                if (currentIndex != -1 && lastSelectedIndex != -1)
                {
                    int start = Math.Min(currentIndex, lastSelectedIndex);
                    int end = Math.Max(currentIndex, lastSelectedIndex);

                    for (int i = start; i <= end; i++)
                    {
                        PictureBox picture = allPictures[i];
                        if (!selectedPictureBoxesFlow1.Contains(picture))
                        {
                            selectedPictureBoxesFlow1.Add(picture);
                            picture.Invalidate(); // <<<--- إضافة: إعادة رسم الصورة
                        }
                    }
                }
            }
            else
            {
                ClearSelectionFlow1(); // هذه الدالة ستعيد رسم العناصر القديمة
                selectedPictureBoxesFlow1.Add(pb);
                pb.Invalidate(); // <<<--- إضافة: إعادة رسم الصورة المحددة
            }

            flowLayoutPanel1.Focus();
        }
        private void ClearSelectionFlow1()
        {
            var picturesToClear = new List<PictureBox>(selectedPictureBoxesFlow1);

            selectedPictureBoxesFlow1.Clear(); // مسح القائمة أولاً

            foreach (var pb in picturesToClear)
            {
                pb.Invalidate();
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

                        DataObject dataObject = new DataObject();
                        List<string> filePathsToTransfer = new List<string>();

                        if (selectedPictureBoxesFlow2.Count > 1 && selectedPictureBoxesFlow2.Contains(draggedPictureBoxPanel2))
                        {
                            foreach (var selectedPb in selectedPictureBoxesFlow2)
                            {
                                if (selectedPb.Tag != null && selectedPb.Image != null)
                                {
                                    filePathsToTransfer.Add(selectedPb.Tag.ToString());
                                }
                            }
                            dataObject.SetData("MultiDragFlow2", selectedPictureBoxesFlow2.ToArray());
                        }
                        else
                        {
                            if (draggedPictureBoxPanel2.Tag != null)
                            {
                                filePathsToTransfer.Add(draggedPictureBoxPanel2.Tag.ToString());
                            }
                        }

                        // *** تعديل مهم: استخدم FlowPanel2Reorder ***
                        dataObject.SetData("FlowPanel2Reorder", draggedPictureBoxPanel2);
                        dataObject.SetData("ReturnToPanel1", draggedPictureBoxPanel2);
                        dataObject.SetData("DragSource", dragSourcePanel?.Name ?? "unknown");

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
                // *** تعديل: الأولوية لعملية إعادة الترتيب ***
                if (e.Data.GetDataPresent("FlowPanelReorder"))
                {
                    PictureBox sourcePic = (PictureBox)e.Data.GetData("FlowPanelReorder");

                    // إذا كان هناك مكان إدراج صالح، انقل الصورة إليه
                    if (sourcePic != null && _insertionIndex != -1)
                    {
                        flowLayoutPanel1.Controls.SetChildIndex(sourcePic, _insertionIndex);
                        flowLayoutPanel1.PerformLayout();
                    }
                    e.Effect = DragDropEffects.None; // لمنع أي معالجة إضافية
                    return; // الخروج بعد معالجة إعادة الترتيب
                }

                // باقي الكود الأصلي للتعامل مع السحب من المصادر الأخرى
                string filePath = null;
                PictureBox sourcePictureBox = null;

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0) filePath = files[0];
                }
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    filePath = data.Split('|')[0];

                    sourcePictureBox = FindPictureBoxInFlowPanel(flowLayoutPanel2, filePath);
                    if (sourcePictureBox == null)
                    {
                        PictureBox apostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
                        if (apostille?.Tag?.ToString() == filePath) sourcePictureBox = apostille;
                    }
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

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

                string ext = Path.GetExtension(filePath).ToLower();
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                {
                    if (sourcePictureBox != null && targetPic.Image != null)
                    {
                        SwapImagesBetweenControls(sourcePictureBox, targetPic);
                    }
                    else
                    {
                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            targetPic.Image?.Dispose();
                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

                        if (sourcePictureBox != null)
                        {
                            if (sourcePictureBox.Name == "imageApostille")
                            {
                                sourcePictureBox.Image = null;
                                sourcePictureBox.Tag = null;
                            }
                            else
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
            finally
            {
                // *** تنظيف أخير لضمان إخفاء الخط ***
                if (_insertionIndex != -1)
                {
                    _insertionIndex = -1;
                    flowLayoutPanel1.Invalidate();
                }
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
        private PictureBox draggedPictureBox = null;

        private void Pic_MouseDown_FlowPanel(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStartPoint = e.Location;
                isDragging = false;
                draggedPictureBox = sender as PictureBox;
                dragSourcePanel = flowLayoutPanel1;

                // إعادة تعيين مؤشر الإدراج
                _insertionIndex = -1;
                flowLayoutPanel1.Invalidate();
            }
        }

        private void Pic_MouseUp_FlowPanel(object sender, MouseEventArgs e)
        {
            isDragging = false;
            draggedPictureBox = null;
            dragSourcePanel = null;

            // إخفاء خط الإدراج عند الانتهاء
            if (_insertionIndex != -1)
            {
                _insertionIndex = -1;
                flowLayoutPanel1.Invalidate();
            }
        }

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
            object result = db.ExecuteScalar(orderQuery, new List<SQLiteParameter>
            {
                new SQLiteParameter("@date", receptionDate.ToString("yyyy-MM-dd"))
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
            // تغيير: استخدام SQLiteParameter
            List<SQLiteParameter> parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@company_client", companyClient),
                new SQLiteParameter("@reception_date", receptionDate.ToString("yyyy-MM-dd")),
                new SQLiteParameter("@reception_time", receptionTime),
                new SQLiteParameter("@delivery_days", deliveryDays),
                new SQLiteParameter("@delivery_date", deliveryDate.ToString("yyyy-MM-dd")),
                new SQLiteParameter("@hours_spent", 24),
                new SQLiteParameter("@project_order", newOrder),
                new SQLiteParameter("@folder_name", folderName),
                new SQLiteParameter("@note", string.IsNullOrWhiteSpace(txtnotes.Text) ? DBNull.Value : (object)txtnotes.Text),
                new SQLiteParameter("@document_type", documentType),
                new SQLiteParameter("@translation_type", translationType)
            };

            bool success = db.ExecuteNonQuery(insertQuery, parameters);
            if (success)
            {
           
                string getLastIdQuery = "SELECT last_insert_rowid()";
                object lastIdResult = db.ExecuteScalar(getLastIdQuery, null); // لا نحتاج لمعاملات هنا
                int projectId = Convert.ToInt32(lastIdResult);

                bool allImagesSaved = SaveProjectImages(projectId, folderName, deliveryDateStr, receptionDateStr, projectOrderStr, receptionTimeStr, companyClient, translationType, documentType);

                if (allImagesSaved)
                {
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

            string projectFolder = db.GetSavedPathById("save");
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

                        // تغيير: استخدام SQLiteParameter
                        List<SQLiteParameter> imageParameters = new List<SQLiteParameter>
                        {
                            new SQLiteParameter("@project_id", projectId),
                            new SQLiteParameter("@image_name", fullImageName),
                            new SQLiteParameter("@image_path", imagePath)
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

                    // تغيير: استخدام SQLiteParameter
                    List<SQLiteParameter> imageParameters = new List<SQLiteParameter>
                    {
                        new SQLiteParameter("@project_id", projectId),
                        new SQLiteParameter("@image_name", fullImageName),
                        new SQLiteParameter("@image_path", imagePath),
                        new SQLiteParameter("@attachment_type", "Apostille")
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
                        List<SQLiteParameter> imageParameters = new List<SQLiteParameter>
                        {
                            new SQLiteParameter("@project_id", projectId),
                            new SQLiteParameter("@image_name", fullImageName),
                            new SQLiteParameter("@image_path", imagePath),
                            new SQLiteParameter("@attachment_type", attachmentType)
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
                    
                            List<SQLiteParameter> wordParameters = new List<SQLiteParameter>
                        {
                            new SQLiteParameter("@project_id", projectId),
                            new SQLiteParameter("@image_name", fullWordFileName),
                            new SQLiteParameter("@image_path", wordPath),
                            new SQLiteParameter("@attachment_type", "WORD")
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
             
                List<SQLiteParameter> googleDriverParams = new List<SQLiteParameter>
                        {
                            new SQLiteParameter("@project_id", projectId),
                            new SQLiteParameter("@image_name", googleDriverFileName),
                            new SQLiteParameter("@image_path", googleDriverPath),
                            new SQLiteParameter("@attachment_type", "Google Driver")
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
            
                List<SQLiteParameter> traduccionParams = new List<SQLiteParameter>
                        {
                            new SQLiteParameter("@project_id", projectId),
                            new SQLiteParameter("@image_name", traduccionFileName),
                            new SQLiteParameter("@image_path", traduccionPath),
                            new SQLiteParameter("@attachment_type", "Traducción Preliminar")
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
            try
            {
                string informeFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Informe revisión.docx";
                string informePath = Path.Combine(projectFolder, informeFileName);
                using (var fs = File.Create(informePath)) { }

                string insertInformeQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) 
          VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";
        
                List<SQLiteParameter> informeParams = new List<SQLiteParameter>
                        {
                            new SQLiteParameter("@project_id", projectId),
                            new SQLiteParameter("@image_name", informeFileName),
                            new SQLiteParameter("@image_path", informePath),
                            new SQLiteParameter("@attachment_type", attachmentType)
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
            return allSaved;
        }
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
                if (pbToRemove.Image != null)
                {
                    pbToRemove.Image.Dispose();
                    pbToRemove.Image = null;
                }
                panel1.Controls.Remove(pbToRemove);
                pbToRemove.Dispose();
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

            pb.DoubleClick += OpenImage_DoubleClick;
            pb.MouseDown += Pb_MouseDown_Panel1; //  مهم للسحب
            pb.MouseMove += Pb_MouseMove_Panel1; //  مهم للسحب
            pb.MouseUp += Pb_MouseUp_Panel1;   //  مهم للسحب
            pb.Click += Pb_Click_Panel1;       //  مهم للتحديد الفردي والمتعدد
            pb.Paint += PictureBox_Paint_Selection;
            panel1.Controls.Add(pb);
            panel1.Controls.SetChildIndex(pb, 0); // ضعه في بداية القائمة
            ReArrangeImages();
        }

        private void AddFile_Load(object sender, EventArgs e)
        {
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
            string projectFolder = db.GetSavedPathById( "archive");
            if (string.IsNullOrEmpty(projectFolder))
            {
                MessageBox.Show("Please set a save directory first.", "Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            OpenFileDialog ofd = new OpenFileDialog();
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
                        ToolTip toolTip = new ToolTip();
                        toolTip.SetToolTip(lbl, fullPath);
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
        private void SetupFlowLayoutPanel2()
        {
            flowLayoutPanel2.AllowDrop = true;

            // أحداث السحب والإفلات الأساسية
            flowLayoutPanel2.DragEnter += FlowLayoutPanel2_DragEnter;
            flowLayoutPanel2.DragDrop += FlowLayoutPanel2_DragDrop;

            // أحداث جديدة لإعادة الترتيب (رسم خط الإدراج)
            flowLayoutPanel2.DragOver += FlowLayoutPanel2_DragOver;
            flowLayoutPanel2.DragLeave += FlowLayoutPanel2_DragLeave;

            // أحداث التحديد ورسم المستطيل
            flowLayoutPanel2.MouseDown += FlowLayoutPanel2_MouseDown_ForSelection;
            flowLayoutPanel2.MouseMove += FlowLayoutPanel2_MouseMove_ForSelection;
            flowLayoutPanel2.MouseUp += FlowLayoutPanel2_MouseUp_ForSelection;
            flowLayoutPanel2.Paint += FlowLayoutPanel2_Paint_SelectionRectangle;
        }
        private void FlowLayoutPanel2_DragOver(object sender, DragEventArgs e)
        {
            // تحقق من أننا نقوم بإعادة ترتيب عنصر داخل flowLayoutPanel2
            if (!e.Data.GetDataPresent("FlowPanel2Reorder"))
            {
                if (_insertionIndex != -1)
                {
                    _insertionIndex = -1;
                    flowLayoutPanel2.Invalidate();
                }
                return;
            }

            e.Effect = DragDropEffects.Move;
            Point clientPoint = flowLayoutPanel2.PointToClient(new Point(e.X, e.Y));

            // استخدم دالة حساب مخصصة لـ flowLayoutPanel2
            int newIndex = CalculateInsertionIndexForPanel2(clientPoint);

            if (newIndex != _insertionIndex)
            {
                _insertionIndex = newIndex;
                flowLayoutPanel2.Invalidate();
            }
        }

        private void FlowLayoutPanel2_DragLeave(object sender, EventArgs e)
        {
            // عند مغادرة الفأرة، قم بإخفاء الخط
            if (_insertionIndex != -1)
            {
                _insertionIndex = -1;
                flowLayoutPanel2.Invalidate();
            }
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
                            }
                        }
                    }
                }

                flowLayoutPanel2.Invalidate(); // إعادة رسم اللوحة لتحديث التحديدات
            }
        }
        private void ClearSelectionFlow2()
        {
            var picturesToClear = new List<PictureBox>(selectedPictureBoxesFlow2);

            selectedPictureBoxesFlow2.Clear();

            foreach (var pb in picturesToClear)
            {
                pb.Invalidate();
            }
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
                if (selectedPictureBoxesFlow2.Contains(pb))
                {
                    selectedPictureBoxesFlow2.Remove(pb);
                }
                else
                {
                    selectedPictureBoxesFlow2.Add(pb);
                }
                pb.Invalidate(); // <<<--- إضافة: إعادة رسم الصورة
            }
            else if (Control.ModifierKeys == Keys.Shift && selectedPictureBoxesFlow2.Count > 0)
            {
                var allPictures = flowLayoutPanel2.Controls.OfType<PictureBox>()
                    .OrderBy(p => p.Location.Y)
                    .ThenBy(p => p.Location.X)
                    .ToList();

                int currentIndex = allPictures.IndexOf(pb);
                int lastSelectedIndex = allPictures.IndexOf(selectedPictureBoxesFlow2.Last());

                if (currentIndex != -1 && lastSelectedIndex != -1)
                {
                    int start = Math.Min(currentIndex, lastSelectedIndex);
                    int end = Math.Max(currentIndex, lastSelectedIndex);

                    for (int i = start; i <= end; i++)
                    {
                        PictureBox picture = allPictures[i];
                        if (!selectedPictureBoxesFlow2.Contains(picture))
                        {
                            selectedPictureBoxesFlow2.Add(picture);
                            picture.Invalidate(); // <<<--- إضافة: إعادة رسم الصورة
                        }
                    }
                }
            }
            else
            {
                ClearSelectionFlow2();
                if (pb.Image != null)
                {
                    selectedPictureBoxesFlow2.Add(pb);
                    pb.Invalidate(); // <<<--- إضافة: إعادة رسم الصورة
                }
            }

            flowLayoutPanel2.Focus();
        }

        private void FlowLayoutPanel2_Paint_SelectionRectangle(object sender, PaintEventArgs e)
        {
            // 1. رسم مستطيل التحديد (الكود الأصلي)
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

            // 2. إضافة رسم خط الإدراج (الكود الجديد)
            // تحقق من أن السحب بدأ من اللوحة الثانية
            if (_insertionIndex != -1 && draggedPictureBoxPanel2 != null)
            {
                Point lineStart, lineEnd;
                int margin = 3;

                if (_insertionIndex < flowLayoutPanel2.Controls.Count)
                {
                    Control targetControl = flowLayoutPanel2.Controls[_insertionIndex];
                    if (flowLayoutPanel2.FlowDirection == FlowDirection.LeftToRight)
                    {
                        lineStart = new Point(targetControl.Left - margin, targetControl.Top);
                        lineEnd = new Point(targetControl.Left - margin, targetControl.Bottom);
                    }
                    else // RightToLeft
                    {
                        lineStart = new Point(targetControl.Right + margin, targetControl.Top);
                        lineEnd = new Point(targetControl.Right + margin, targetControl.Bottom);
                    }
                }
                else if (flowLayoutPanel2.Controls.Count > 0)
                {
                    Control lastControl = flowLayoutPanel2.Controls[flowLayoutPanel2.Controls.Count - 1];
                    if (flowLayoutPanel2.FlowDirection == FlowDirection.LeftToRight)
                    {
                        lineStart = new Point(lastControl.Right + margin, lastControl.Top);
                        lineEnd = new Point(lastControl.Right + margin, lastControl.Bottom);
                    }
                    else // RightToLeft
                    {
                        lineStart = new Point(lastControl.Left - margin, lastControl.Top);
                        lineEnd = new Point(lastControl.Left - margin, lastControl.Bottom);
                    }
                }
                else
                {
                    if (flowLayoutPanel2.FlowDirection == FlowDirection.LeftToRight)
                    {
                        lineStart = new Point(flowLayoutPanel2.Padding.Left, flowLayoutPanel2.Padding.Top);
                        lineEnd = new Point(flowLayoutPanel2.Padding.Left, flowLayoutPanel2.ClientSize.Height - flowLayoutPanel2.Padding.Bottom);
                    }
                    else // RightToLeft
                    {
                        lineStart = new Point(flowLayoutPanel2.ClientSize.Width - flowLayoutPanel2.Padding.Right, flowLayoutPanel2.Padding.Top);
                        lineEnd = new Point(flowLayoutPanel2.ClientSize.Width - flowLayoutPanel2.Padding.Right, flowLayoutPanel2.ClientSize.Height - flowLayoutPanel2.Padding.Bottom);
                    }
                }

                e.Graphics.DrawLine(_insertionLinePen, lineStart, lineEnd);
            }
        }
        private void FlowLayoutPanel2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("DragSource"))
            {
                string dragSource = e.Data.GetData("DragSource").ToString();
                FlowLayoutPanel targetPanel = sender as FlowLayoutPanel;
                if (dragSource == targetPanel?.Name)
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }
            }

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
                FlowLayoutPanel panel = sender as FlowLayoutPanel;
                if (panel == null) return;

                // *** بداية الإضافة الجديدة: التعامل مع إعادة ترتيب السحب المتعدد ***
                if (e.Data.GetDataPresent("MultiDragFlow2") && e.Data.GetData("DragSource")?.ToString() == panel.Name)
                {
                    var picturesToMove = (PictureBox[])e.Data.GetData("MultiDragFlow2");

                    var orderedPicturesToMove = panel.Controls.OfType<PictureBox>()
                                                     .Where(p => picturesToMove.Contains(p))
                                                     .ToList();

                    if (_insertionIndex != -1 && orderedPicturesToMove.Any())
                    {
                        int targetIndex = _insertionIndex;
                        int shift = orderedPicturesToMove.Count(p => panel.Controls.GetChildIndex(p) < targetIndex);
                        targetIndex -= shift;

                        if (targetIndex < 0) targetIndex = 0;

                        foreach (var pic in orderedPicturesToMove)
                        {
                            panel.Controls.SetChildIndex(pic, targetIndex);
                            targetIndex++;
                        }
                        panel.PerformLayout();
                    }

                    _insertionIndex = -1;
                    panel.Invalidate();
                    return;
                }
                // *** نهاية الإضافة الجديدة ***


                // التعامل مع إعادة ترتيب عنصر واحد (الكود الأصلي)
                if (e.Data.GetDataPresent("FlowPanel2Reorder"))
                {
                    PictureBox sourcePic = (PictureBox)e.Data.GetData("FlowPanel2Reorder");

                    if (sourcePic != null && _insertionIndex != -1)
                    {
                        int sourceIndex = panel.Controls.GetChildIndex(sourcePic);
                        int targetIndex = _insertionIndex;

                        if (sourceIndex < targetIndex)
                        {
                            targetIndex--;
                        }

                        if (targetIndex < 0) targetIndex = 0;
                        if (targetIndex >= panel.Controls.Count) targetIndex = panel.Controls.Count - 1;

                        panel.Controls.SetChildIndex(sourcePic, targetIndex);
                        panel.PerformLayout();
                    }

                    _insertionIndex = -1;
                    panel.Invalidate();
                    return;
                }

                // باقي الكود الأصلي للسحب من لوحات أخرى
                List<string> filePaths = new List<string>();
                bool isInternalDrag = e.Data.GetDataPresent(DataFormats.StringFormat);

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
                            targetPic.Image?.Dispose();
                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = apostilleFilePath;
                        }

                        sourceApostille.Image?.Dispose();
                        sourceApostille.Image = null;
                        sourceApostille.Tag = null;
                        selectedPictureBoxes.Remove(sourceApostille);
                        return;
                    }
                }

                if (e.Data.GetDataPresent("MultiDragFlow1"))
                {
                    PictureBox[] selectedPictureBoxesFromFlow1 = (PictureBox[])e.Data.GetData("MultiDragFlow1");
                    foreach (var sourcePb in selectedPictureBoxesFromFlow1)
                    {
                        if (sourcePb?.Tag != null && sourcePb.Image != null)
                        {
                            string filePath = sourcePb.Tag.ToString();
                            PictureBox targetPic = FindEmptyPictureBoxInPanel2();
                            if (targetPic == null)
                            {
                                targetPic = CreateNewPictureBoxForPanel2();
                                panel.Controls.Add(targetPic);
                            }
                            using (var imgTemp = Image.FromFile(filePath))
                            {
                                targetPic.Image?.Dispose();
                                targetPic.Image = new Bitmap(imgTemp);
                                targetPic.Tag = filePath;
                            }
                        }
                    }
                    foreach (var sourcePb in selectedPictureBoxesFromFlow1)
                    {
                        if (sourcePb.Parent != null) sourcePb.Parent.Controls.Remove(sourcePb);
                        sourcePb.Dispose();
                    }
                    ClearSelectionFlow1();
                    return;
                }


                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    filePaths.AddRange(files);
                }
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    filePaths.AddRange(data.Split('|'));
                }

                foreach (string filePath in filePaths)
                {
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) continue;

                    string ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
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
                        if (isInternalDrag)
                        {
                            RemoveImageFromPanel1(filePath);
                            RemoveImageFromFlowLayoutPanel(flowLayoutPanel1, filePath);
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
            finally
            {
                if (_insertionIndex != -1)
                {
                    _insertionIndex = -1;
                    flowLayoutPanel2.Invalidate();
                }
            }
        }
        private int CalculateInsertionIndexForPanel2(Point clientPoint)
        {
            for (int i = 0; i < flowLayoutPanel2.Controls.Count; i++)
            {
                Control c = flowLayoutPanel2.Controls[i];
                if (c == draggedPictureBoxPanel2) continue; // استخدم المتغير الخاص باللوحة الثانية

                Rectangle bounds = c.Bounds;

                if (flowLayoutPanel2.FlowDirection == FlowDirection.LeftToRight)
                {
                    if (clientPoint.X < bounds.Left + (bounds.Width / 2))
                    {
                        return i;
                    }
                }
                else if (flowLayoutPanel2.FlowDirection == FlowDirection.RightToLeft)
                {
                    if (clientPoint.X > bounds.Right - (bounds.Width / 2))
                    {
                        return i;
                    }
                }
                else if (flowLayoutPanel2.FlowDirection == FlowDirection.TopDown)
                {
                    if (clientPoint.Y < bounds.Top + (bounds.Height / 2))
                    {
                        return i;
                    }
                }
            }
            return flowLayoutPanel2.Controls.Count;
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
            pic.DragEnter += Pic_DragEnter_Panel2;
            pic.DragDrop += Pic_DragDrop_Panel2;
            pic.MouseDown += Pic_MouseDown_Panel2;
            pic.MouseMove += Pic_MouseMove_Panel2;
            pic.MouseUp += Pic_MouseUp_Panel2;

            pic.DoubleClick += OpenImage_DoubleClick;
            pic.Click += Pic_Click_FlowPanel2;
            pic.Paint += PictureBox_Paint_Selection; // <<<--- تأكد من وجود هذا السطر


            return pic;
        }

        private void Pic_DragEnter_Panel2(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("DragSource"))
            {
                string dragSource = e.Data.GetData("DragSource").ToString();
                PictureBox targetPic = sender as PictureBox;
                FlowLayoutPanel targetPanel = targetPic?.Parent as FlowLayoutPanel;

                if (dragSource == targetPanel?.Name)
                {
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
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) // من ملف خارجي
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0) filePath = files[0];
                }
                else if (e.Data.GetDataPresent(DataFormats.StringFormat)) // من داخل البرنامج
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    filePath = data.Split('|')[0];

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
                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            targetPic.Image?.Dispose();
                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }

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
        }       
        private PictureBox draggedPictureBoxPanel2 = null;

        private void Pic_MouseDown_Panel2(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStartPoint = e.Location;
                isDragging = false;
                draggedPictureBoxPanel2 = sender as PictureBox; // استخدم المتغير الصحيح

                if (draggedPictureBoxPanel2 != null)
                {
                    dragSourcePanel = draggedPictureBoxPanel2.Parent as FlowLayoutPanel;
                }

                // إعادة تعيين مؤشر الإدراج
                _insertionIndex = -1;
                flowLayoutPanel2.Invalidate();
            }
        }

        private void Pic_MouseUp_Panel2(object sender, MouseEventArgs e)
        {
            isDragging = false;
            draggedPictureBoxPanel2 = null; // نظّف المتغير الصحيح
            dragSourcePanel = null;

            // إخفاء خط الإدراج عند الانتهاء
            if (_insertionIndex != -1)
            {
                _insertionIndex = -1;
                flowLayoutPanel2.Invalidate();
            }
        }
        private void SwapPictureBoxesPanel2(PictureBox source, PictureBox target)
        {
            Image sourceImage = source.Image;
            object sourceTag = source.Tag;

            Image targetImage = target.Image;
            object targetTag = target.Tag;

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
                imageApostille.BorderStyle = BorderStyle.None; //  <<-- سيبقى دائمًا بدون إطار

                imageApostille.DragEnter += ImageApostille_DragEnter;
                imageApostille.DragDrop += ImageApostille_DragDrop;

                imageApostille.DragOver += ImageApostille_DragOver;
                imageApostille.DoubleClick += OpenImage_DoubleClick;

                imageApostille.MouseDown += imageApostille_MouseDown;
                imageApostille.MouseMove += imageApostille_MouseMove;
                imageApostille.MouseUp += imageApostille_MouseUp;
                imageApostille.Click += imageApostille_Click;

                // *** الإضافة الجديدة والمهمة هنا ***
                imageApostille.Paint += PictureBox_Paint_Selection;
            }
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
        private void ImageApostille_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
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
        // استبدل الدالة القديمة بهذه النسخة الجديدة
        private void imageApostille_Click(object sender, EventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb?.Image == null) return;

            // منع معالجة النقر المزدوج كنقرة فردية
            if ((DateTime.Now - lastClickTime).TotalMilliseconds < SystemInformation.DoubleClickTime)
            {
                return;
            }
            lastClickTime = DateTime.Now;

            if (Control.ModifierKeys == Keys.Control)
            {
                // Ctrl + Click: تبديل حالة التحديد
                if (selectedPictureBoxes.Contains(pb))
                {
                    selectedPictureBoxes.Remove(pb);
                }
                else
                {
                    selectedPictureBoxes.Add(pb);
                }
            }
            else
            {
                // النقر العادي: تحديد هذا العنصر فقط
                // التحقق مما إذا كان هذا هو العنصر الوحيد المحدد بالفعل
                bool onlyThisIsSelected = selectedPictureBoxes.Count == 1 && selectedPictureBoxes.Contains(pb);
                if (!onlyThisIsSelected)
                {
                    ClearAllSelections();
                    selectedPictureBoxes.Add(pb);
                }
            }

            // إعادة رسم الصورة لتحديث مظهر التحديد
            pb.Invalidate();
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
                        sourcePictureBox = FindPictureBoxInFlowPanel(flowLayoutPanel1, filePath)
                                        ?? FindPictureBoxInFlowPanel(flowLayoutPanel2, filePath);
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

                string ext = Path.GetExtension(filePath).ToLower();
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                {
                    if (sourcePictureBox != null && targetPic.Image != null)
                    {
                        SwapImagesBetweenControls(sourcePictureBox, targetPic);
                    }
                    else
                    {
                        using (var imgTemp = Image.FromFile(filePath))
                        {
                            targetPic.Image?.Dispose();
                            targetPic.Image = new Bitmap(imgTemp);
                            targetPic.Tag = filePath;
                        }
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
         
            if (e.Button == MouseButtons.Left)
            {
                // التحقق مما إذا كان النقر فوق عنصر تحكم فرعي (صورة)
                Control clickedControl = panel1.GetChildAtPoint(e.Location);
                if (clickedControl != null && clickedControl is PictureBox)
                {              
                    isSelecting = false;
                    return;
                }

                isSelecting = true;
                selectionStartPoint = e.Location;

                if (Control.ModifierKeys != Keys.Control)
                {
                    ClearSelection();
                }

                panel1.Invalidate();
            }
        }

        private void panel1_MouseMove_ForSelection(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                int x = Math.Min(selectionStartPoint.X, e.X);
                int y = Math.Min(selectionStartPoint.Y, e.Y);
                int width = Math.Abs(selectionStartPoint.X - e.X);
                int height = Math.Abs(selectionStartPoint.Y - e.Y);
                selectionRectangle = new Rectangle(x, y, width, height);

                panel1.Invalidate();
            }
        }

        private void panel1_MouseUp_ForSelection(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                
                isSelecting = false;

                foreach (Control control in panel1.Controls)
                {
                    if (control is PictureBox pb)
                    {
                       
                        if (selectionRectangle.Width > 2 && selectionRectangle.Height > 2 && selectionRectangle.IntersectsWith(pb.Bounds))
                        {
                            // إذا تقاطعت، قم بتحديد الصورة
                            if (!selectedPictureBoxes.Contains(pb))
                            {
                                selectedPictureBoxes.Add(pb);
                                pb.Invalidate();
                            }
                        }
                    }
                }

        
                selectionRectangle = Rectangle.Empty;

                panel1.Invalidate();
            }
        }

        private void panel1_Paint_SelectionRectangle(object sender, PaintEventArgs e)
        {
            if (isSelecting && selectionRectangle != Rectangle.Empty)
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
                if (selectedPictureBoxesFlow1.Contains(pbToRemove))
                {
                    selectedPictureBoxesFlow1.Remove(pbToRemove);
                }

                panel.Controls.Remove(pbToRemove);
                pbToRemove.Image?.Dispose();
                pbToRemove.Dispose();
            }
        }
        private void UpdateExistingPictureBoxEvents()
        {
            foreach (Control control in flowLayoutPanel1.Controls)
            {
                if (control is PictureBox pb)
                {
                    pb.Click -= Pic_Click_FlowPanel1;
                    pb.Click += Pic_Click_FlowPanel1;
                }
            }
        }
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
                            }
                        }
                    }
                }
                flowLayoutPanel1.Invalidate();
            }
        }

        // *** تعديل 7: دمج رسم مستطيل التحديد مع رسم خط الإدراج ***
        private void FlowLayoutPanel1_Paint_SelectionRectangle(object sender, PaintEventArgs e)
        {
            // رسم مستطيل التحديد (الكود الأصلي)
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

            // رسم خط الإدراج العمودي
            if (_insertionIndex != -1 && draggedPictureBox != null)
            {
                Point lineStart, lineEnd;
                int margin = 3; // هامش صغير ليبدو الخط أفضل

                // إذا كان الإدراج ليس في نهاية القائمة
                if (_insertionIndex < flowLayoutPanel1.Controls.Count)
                {
                    Control targetControl = flowLayoutPanel1.Controls[_insertionIndex];
                    if (flowLayoutPanel1.FlowDirection == FlowDirection.LeftToRight)
                    {
                        lineStart = new Point(targetControl.Left - margin, targetControl.Top);
                        lineEnd = new Point(targetControl.Left - margin, targetControl.Bottom);
                    }
                    else // RightToLeft
                    {
                        lineStart = new Point(targetControl.Right + margin, targetControl.Top);
                        lineEnd = new Point(targetControl.Right + margin, targetControl.Bottom);
                    }
                }
                // إذا كان الإدراج في نهاية القائمة
                else if (flowLayoutPanel1.Controls.Count > 0)
                {
                    Control lastControl = flowLayoutPanel1.Controls[flowLayoutPanel1.Controls.Count - 1];
                    if (flowLayoutPanel1.FlowDirection == FlowDirection.LeftToRight)
                    {
                        lineStart = new Point(lastControl.Right + margin, lastControl.Top);
                        lineEnd = new Point(lastControl.Right + margin, lastControl.Bottom);
                    }
                    else // RightToLeft
                    {
                        lineStart = new Point(lastControl.Left - margin, lastControl.Top);
                        lineEnd = new Point(lastControl.Left - margin, lastControl.Bottom);
                    }
                }
                else // إذا كانت اللوحة فارغة
                {
                    if (flowLayoutPanel1.FlowDirection == FlowDirection.LeftToRight)
                    {
                        lineStart = new Point(flowLayoutPanel1.Padding.Left, flowLayoutPanel1.Padding.Top);
                        lineEnd = new Point(flowLayoutPanel1.Padding.Left, flowLayoutPanel1.ClientSize.Height - flowLayoutPanel1.Padding.Bottom);
                    }
                    else // RightToLeft
                    {
                        lineStart = new Point(flowLayoutPanel1.ClientSize.Width - flowLayoutPanel1.Padding.Right, flowLayoutPanel1.Padding.Top);
                        lineEnd = new Point(flowLayoutPanel1.ClientSize.Width - flowLayoutPanel1.Padding.Right, flowLayoutPanel1.ClientSize.Height - flowLayoutPanel1.Padding.Bottom);
                    }
                }

                // ارسم الخط باستخدام القلم المحدد
                e.Graphics.DrawLine(_insertionLinePen, lineStart, lineEnd);
            }
        }
        private void ClearAllSelections()
        {
            var picturesToInvalidate = new List<PictureBox>();
            picturesToInvalidate.AddRange(selectedPictureBoxes);
            picturesToInvalidate.AddRange(selectedPictureBoxesFlow1);
            picturesToInvalidate.AddRange(selectedPictureBoxesFlow2);

            selectedPictureBoxes.Clear();
            selectedPictureBoxesFlow1.Clear();
            selectedPictureBoxesFlow2.Clear();

            foreach (var pb in picturesToInvalidate)
            {
                pb.Invalidate(); // هذا سيقوم بإعادة رسم الصورة بدون التحديد
            }

            // *** تم حذف الكتلة البرمجية القديمة التي كانت تتعامل مع apostilleBox.BorderStyle ***
        }
        private void OpenSelectedImages()
        {
            List<PictureBox> allSelectedPictures = new List<PictureBox>();
            allSelectedPictures.AddRange(selectedPictureBoxesFlow1);
            allSelectedPictures.AddRange(selectedPictureBoxesFlow2);
            allSelectedPictures.AddRange(selectedPictureBoxes);

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
            return selectedPictureBoxes.Any() ||
                   selectedPictureBoxesFlow1.Any() ||
                   selectedPictureBoxesFlow2.Any();
        }
        private void EmptySpace_Click(object sender, EventArgs e)
        {
            ClearAllSelections();

            isSelecting = false;
            selectionRectangle = Rectangle.Empty;

            panel1.Invalidate();
            flowLayoutPanel1.Invalidate();
            flowLayoutPanel2.Invalidate();
        }
        private void CleanWorkspace()
        {
            try
            {
                ClearFlowLayoutPanel(flowLayoutPanel1);

                ClearFlowLayoutPanel(flowLayoutPanel2);

                ClearImageApostille();

                ClearPanelDocx();

                ClearAllSelections();

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

                panel.Controls.Remove(pb);

                pb.Dispose();
            }
        }

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

                imageApostille.Tag = null;

                imageApostille.BorderStyle = BorderStyle.None;
            }
        }

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
            txtnotes.Clear();
            comboDocumentType.SelectedIndex = -1; // يلغي التحديد
            comboTranslation.SelectedIndex = -1; // يلغي التحديد

            Reception_Date.Value = DateTime.Now;

        }
        private void LoadClientsAndCompanies()
        {
            try
            {
                string query = @"
            SELECT client_id AS id, client_code AS code, 'Client' AS type FROM clients
            UNION ALL
            SELECT company_id AS id, company_code AS code, 'Company' AS type FROM companies
            ORDER BY type, code";
                DataTable combinedData = db.ExecuteQuery(query, null);

                Company_Client.Items.Clear();
                foreach (DataRow row in combinedData.Rows)
                {
                    int entityId = Convert.ToInt32(row["id"]);
                    string entityCode = row["code"].ToString();

                    Company_Client.Items.Add(new KeyValuePair<int, string>(entityId, entityCode));
                }

                Company_Client.DisplayMember = "Value";
                Company_Client.ValueMember = "Key";
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
                DataTable documentTypesData = db.ExecuteQuery(query, null);
                comboDocumentType.Items.Clear();
                foreach (DataRow row in documentTypesData.Rows)
                {
                    int documentTypeId = Convert.ToInt32(row["id"]);
                    string documentTypeName = row["code"].ToString();

                    comboDocumentType.Items.Add(new KeyValuePair<int, string>(documentTypeId, documentTypeName));
                }
                comboDocumentType.DisplayMember = "Value";
                comboDocumentType.ValueMember = "Key";

                // Select first item if data exists
                if (comboDocumentType.Items.Count > 0)
                {
                
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
                comboTranslation.DisplayMember = "Value";
                comboTranslation.ValueMember = "Key";
                if (comboTranslation.Items.Count > 0)
                {
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading language pairs:\n{ex.Message}",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Console.WriteLine($"Error in LoadLanguagePairsToComboBox: {ex.ToString()}");
            }
        }
        private void PictureBox_Paint_Selection(object sender, PaintEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null) return;

            // التحقق مما إذا كان هذا الـ PictureBox محدداً حالياً في أي من قوائم التحديد
            bool isSelected = selectedPictureBoxes.Contains(pb) ||
                              selectedPictureBoxesFlow1.Contains(pb) ||
                              selectedPictureBoxesFlow2.Contains(pb);

            // *** تم حذف الشرط الخاص بـ imageApostille لأنه لم يعد ضرورياً ***

            // إذا كان محدداً، ارسم الطبقة الزرقاء الشفافة
            if (isSelected)
            {
                Color overlayColor = Color.FromArgb(100, SystemColors.Highlight);
                using (Brush overlayBrush = new SolidBrush(overlayColor))
                {
                    e.Graphics.FillRectangle(overlayBrush, pb.ClientRectangle);
                }
            }
        }
    }
    //************
}

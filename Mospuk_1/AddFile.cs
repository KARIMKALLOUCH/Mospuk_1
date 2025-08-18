using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using IronPdf;

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
        private PictureBox draggedPictureBox = null; // **تصحيح 1: متغير واحد للصور المسحوبة**
        private bool isMultiPanelDrag = false;
        private List<PictureBox> multiPanelSelectedItems = new List<PictureBox>();
        private FlowLayoutPanel _currentDragOverPanel = null;
        private bool _isDragOverExternalPanel = false;
        private List<KeyboardDragItem> _keyboardDragItems = new List<KeyboardDragItem>();

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
            catch (Exception) { }

            this.KeyPreview = true;
            this.KeyDown += AddFile_KeyDown;
            this.AcceptButton = null;



            panel1.KeyDown += HandleSelectAll;
            flowLayoutPanel1.KeyDown += HandleSelectAll;
            flowLayoutPanel2.KeyDown += HandleSelectAll;

            panel1.MouseDown += panel1_MouseDown;
            panel1.AutoScroll = true;
            panel1.Padding = new Padding(10);
            panel1.AllowDrop = true;
            panel1.DragEnter += panel1_DragEnter;
            panel1.DragDrop += panel1_DragDrop;
            flowLayoutPanel1.MouseDown += (s, e) => flowLayoutPanel1.Focus();
            flowLayoutPanel2.MouseDown += (s, e) => flowLayoutPanel2.Focus();
            SetupFlowLayoutPanel(flowLayoutPanel1);
            SetupFlowLayoutPanel(flowLayoutPanel2);

            panel1.MouseDown += Pb_MouseDown;
            SetupImageApostille();

            this.Click += EmptySpace_Click;
            panel1.Click += EmptySpace_Click;
            flowLayoutPanel1.Click += EmptySpace_Click;
            flowLayoutPanel2.Click += EmptySpace_Click;

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
                ResetDragState(); // إضافة هذا السطر

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
            if (e.Control && e.KeyCode == Keys.X)
            {
                HandleKeyboardDragStart();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                HandleKeyboardDrop();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                List<PictureBox> allSelectedPictures = new List<PictureBox>();
                allSelectedPictures.AddRange(selectedPictureBoxesFlow1);
                allSelectedPictures.AddRange(selectedPictureBoxesFlow2);
                allSelectedPictures.AddRange(selectedPictureBoxes);

                if (allSelectedPictures.Count > 0)
                {
                    var confirmResult = MessageBox.Show(
                        $"Are you sure you want to delete {allSelectedPictures.Count} selected images?",
                        "Confirm Deletion",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirmResult == DialogResult.Yes)
                    {
                        bool panel1NeedsRearrange = false;

                        // *** بداية التعديل: قائمة لتخزين ملفات PDF المرشحة للحذف ***
                        var potentialPdfsToCheck = new HashSet<string>();

                        foreach (var pb in allSelectedPictures.ToList())
                        {
                            string filePathToDelete = pb.Tag?.ToString();

                            if (pb.Parent == flowLayoutPanel1)
                            {
                                flowLayoutPanel1.Controls.Remove(pb);
                                selectedPictureBoxesFlow1.Remove(pb);
                                pb.Image?.Dispose();
                                pb.Dispose();
                            }
                            else if (pb.Parent == flowLayoutPanel2)
                            {
                                flowLayoutPanel2.Controls.Remove(pb);
                                selectedPictureBoxesFlow2.Remove(pb);
                                pb.Image?.Dispose();
                                pb.Dispose();
                            }
                            else if (pb.Parent == panel1)
                            {
                                panel1NeedsRearrange = true;
                                panel1.Controls.Remove(pb);
                                selectedPictureBoxes.Remove(pb);
                                pb.Image?.Dispose();
                                pb.Dispose();
                            }
                            else if (pb.Name == "imageApostille")
                            {
                                pb.Image?.Dispose();
                                pb.Image = null;
                                pb.Tag = null;
                            }

                            if (!string.IsNullOrEmpty(filePathToDelete) && File.Exists(filePathToDelete))
                            {
                                // *** التعديل الثاني: التحقق إذا كانت الصورة من PDF وإضافة الـ PDF للقائمة ***
                                try
                                {
                                    DirectoryInfo parentDir = Directory.GetParent(filePathToDelete);
                                    if (parentDir != null && parentDir.Name.EndsWith("_Images"))
                                    {
                                        // استنتاج اسم ملف الـ PDF الأصلي
                                        string pdfBaseName = parentDir.Name.Substring(0, parentDir.Name.Length - "_Images".Length);
                                        string mainDirectory = Directory.GetParent(parentDir.FullName).FullName;
                                        string sourcePdfPath = Path.Combine(mainDirectory, pdfBaseName + ".pdf");

                                        if (File.Exists(sourcePdfPath))
                                        {
                                            potentialPdfsToCheck.Add(sourcePdfPath);
                                        }
                                    }

                                    // حذف ملف الصورة نفسه
                                    File.Delete(filePathToDelete);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Could not delete the file: {Path.GetFileName(filePathToDelete)}\nError: {ex.Message}",
                                                    "File Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }

                        selectedPictureBoxesFlow1.Clear();
                        selectedPictureBoxesFlow2.Clear();
                        selectedPictureBoxes.Clear();

                        // *** التعديل الثالث: فحص وحذف ملفات PDF إذا لم يتبق لها صور ***
                        if (potentialPdfsToCheck.Any())
                        {
                            // جمع كل الصور المتبقية في التطبيق بعد الحذف
                            var allRemainingPictureBoxes = panel1.Controls.OfType<PictureBox>()
                                .Concat(flowLayoutPanel1.Controls.OfType<PictureBox>())
                                .Concat(flowLayoutPanel2.Controls.OfType<PictureBox>());

                            var remainingFilePaths = new HashSet<string>(
                                allRemainingPictureBoxes
                                    .Where(pb => pb != null && pb.Tag != null)
                                    .Select(pb => pb.Tag.ToString())
                            );

                            foreach (var pdfPath in potentialPdfsToCheck)
                            {
                                string uniqueImageFolderName = Path.GetFileNameWithoutExtension(pdfPath) + "_Images";

                                // التحقق مما إذا كانت هناك أي صورة متبقية من هذا الـ PDF
                                bool anyImageRemains = remainingFilePaths.Any(path => path.Contains(uniqueImageFolderName));

                                if (!anyImageRemains)
                                {
                                    try
                                    {
                                        // لم يتبق أي صور، لذا يمكن حذف الـ PDF والمجلد الخاص به
                                        if (File.Exists(pdfPath))
                                        {
                                            File.Delete(pdfPath);
                                        }

                                        string imageDirectoryPath = Path.Combine(Path.GetDirectoryName(pdfPath), uniqueImageFolderName);
                                        if (Directory.Exists(imageDirectoryPath))
                                        {
                                            Directory.Delete(imageDirectoryPath, true); // true لحذف المجلد ومحتوياته (التي يجب أن تكون فارغة)
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show($"Could not clean up PDF resources for: {Path.GetFileName(pdfPath)}\nError: {ex.Message}",
                                                   "Cleanup Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    }
                                }
                            }
                        }

                        if (panel1NeedsRearrange)
                        {
                            ReArrangeImages();
                        }
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
        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
          
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                return; 
            }
           
            if (e.Data.GetDataPresent("DragSource") && e.Data.GetData("DragSource").ToString() == "panel1")
            {
                e.Effect = DragDropEffects.None; // عرض أيقونة المنع
                return;
            }

            if (e.Data.GetDataPresent("ReturnToPanel1") ||
                e.Data.GetDataPresent("MultiDragFlow1") ||
                e.Data.GetDataPresent("MultiDragFlow2") ||
                e.Data.GetDataPresent("MultiPanelDrag") ||
                e.Data.GetDataPresent("FromImageApostille"))
            {
                e.Effect = DragDropEffects.Move; // السماح بالإفلات من المصادر الأخرى
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);

                string outputFolder = Path.Combine(Application.StartupPath, "ExtractedFiles");
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                foreach (string path in filePaths)
                {
                    try
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
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error processing file '{Path.GetFileName(path)}':\n{ex.Message}",
                                        "Drag & Drop Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                DisplayFiles(outputFolder);

                return;
            }
        
            if (e.Data.GetDataPresent("MultiPanelDrag"))
            {
                var draggedItems = (List<PictureBoxDragInfo>)e.Data.GetData("MultiPanelDrag");

                foreach (var item in draggedItems)
                {
                    if (item.PictureBox?.Tag != null && item.PictureBox.Image != null)
                    {
                        AddImageBackToPanel1(item.PictureBox.Tag.ToString());
                    }
                }

                // تنظيف المصادر
                foreach (var item in draggedItems)
                {
                    CleanupSourcePictureBox(item.PictureBox, item.SourceType);
                }

                ClearAllSelections();
                return;
            }

            // السحب المتعدد من flowLayoutPanel1
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
                    CleanupSourcePictureBox(sourcePb, "flowLayoutPanel1");
                }
                ClearSelectionForPanel(flowLayoutPanel1);
                return;
            }

            // السحب المتعدد من flowLayoutPanel2
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
                    CleanupSourcePictureBox(sourcePb, "flowLayoutPanel2");
                }
                ClearSelectionForPanel(flowLayoutPanel2);
                return;
            }

            // السحب من imageApostille
            if (e.Data.GetDataPresent("FromImageApostille"))
            {
                PictureBox sourcePb = (PictureBox)e.Data.GetData("ReturnToPanel1");
                if (sourcePb?.Tag != null && sourcePb.Image != null)
                {
                    Point dropPoint = panel1.PointToClient(new Point(e.X, e.Y));
                    PictureBox targetPb = panel1.GetChildAtPoint(dropPoint) as PictureBox;

                    if (targetPb != null && targetPb != sourcePb)
                    {
                        SwapImagesBetweenControls(sourcePb, targetPb);
                    }
                    else
                    {
                        AddImageBackToPanel1(sourcePb.Tag.ToString());
                        CleanupSourcePictureBox(sourcePb, "imageApostille");
                    }
                }
                return;
            }

            // السحب الفردي العادي
            if (e.Data.GetDataPresent("ReturnToPanel1"))
            {
                PictureBox sourcePb = (PictureBox)e.Data.GetData("ReturnToPanel1");
                if (sourcePb?.Tag != null)
                {
                    Point dropPoint = panel1.PointToClient(new Point(e.X, e.Y));
                    PictureBox targetPb = panel1.GetChildAtPoint(dropPoint) as PictureBox;

                    if (targetPb != null && targetPb != sourcePb)
                    {
                        SwapImagesBetweenControls(sourcePb, targetPb);
                    }
                    else
                    {
                        AddImageBackToPanel1(sourcePb.Tag.ToString());
                        string sourceType = DetermineSourceType(sourcePb);
                        CleanupSourcePictureBox(sourcePb, sourceType);
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
            var allPictureBoxes = panel1.Controls.OfType<PictureBox>()
                .Concat(flowLayoutPanel1.Controls.OfType<PictureBox>())
                .Concat(flowLayoutPanel2.Controls.OfType<PictureBox>());

            var existingFilePaths = new HashSet<string>(
                allPictureBoxes
                    .Where(pb => pb != null && pb.Tag != null)
                    .Select(pb => pb.Tag.ToString())
            );

            PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            if (imageApostille != null && imageApostille.Tag != null)
            {
                existingFilePaths.Add(imageApostille.Tag.ToString());
            }

            int padding = 10;
            int maxWidth = 120;
            int maxHeight = 120;
            int itemsPerRow = Math.Max(1, (panel1.ClientSize.Width - padding) / (maxWidth + padding));
            int count = panel1.Controls.Count;
            int x = padding + (count % itemsPerRow) * (maxWidth + padding);
            int y = padding + (count / itemsPerRow) * (maxHeight + padding);

            string[] allFilesInDirectory = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

            foreach (string file in allFilesInDirectory)
            {
                string ext = Path.GetExtension(file).ToLower();

                if (ext == ".pdf")
                {
                    try
                    {
                        string pdfImagesDir = Path.Combine(directory, Path.GetFileNameWithoutExtension(file) + "_Images");
                        if (!Directory.Exists(pdfImagesDir))
                            Directory.CreateDirectory(pdfImagesDir);

                        using (var document = IronPdf.PdfDocument.FromFile(file))
                        {
                            document.RasterizeToImageFiles(
     Path.Combine(pdfImagesDir, "Page_*.png")
    );

                            // الخطوة ب: الآن قم بالمرور على الصور التي تم إنشاؤها وأضفها للواجهة
                            var createdImagePaths = Directory.GetFiles(pdfImagesDir, "Page_*.png");
                            foreach (var imagePath in createdImagePaths)
                            {
                                if (existingFilePaths.Contains(imagePath))
                                {
                                    continue;
                                }

                                // نفس الكود السابق لإنشاء PictureBox وعرض الصورة
                                PictureBox pb = new PictureBox();
                                pb.Width = maxWidth;
                                pb.Height = maxHeight;
                                pb.SizeMode = PictureBoxSizeMode.Zoom;
                                pb.BorderStyle = BorderStyle.None;
                                pb.BackColor = Color.White;
                                pb.Tag = imagePath;

                                try
                                {
                                    using (var imgTemp = Image.FromFile(imagePath))
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
                    }


                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error processing PDF '{Path.GetFileName(file)}' with IronPdf:\n{ex.Message}",
                                        "PDF Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    if (existingFilePaths.Contains(file))
                    {
                        continue;
                    }

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
                    }
                    else if (ext == ".docx" || ext == ".doc")
                    {
                        PictureBox pbWord = new PictureBox();
                        pbWord.Width = maxWidth;
                        pbWord.Height = maxHeight;
                        pbWord.SizeMode = PictureBoxSizeMode.Zoom;
                        pbWord.BorderStyle = BorderStyle.None;
                        pbWord.BackColor = Color.Transparent;
                        pbWord.Tag = file;

                        try
                        {
                            pbWord.Image = Properties.Resources.wordicon;
                        }
                        catch { }

                        pbWord.Location = new Point(x, y);
                        pbWord.DoubleClick += OpenImage_DoubleClick;
                        pbWord.MouseDown += Pb_MouseDown_Panel1;
                        pbWord.MouseMove += Pb_MouseMove_Panel1;
                        pbWord.MouseUp += Pb_MouseUp_Panel1;
                        pbWord.Click += Pb_Click_Panel1;
                        pbWord.Paint += PictureBox_Paint_Selection;

                        panel1.Controls.Add(pbWord);
                    }
                    else
                    {
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
                    }

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
                ClearAllSelections(); // مسح جميع التحديدات في كل اللوحات
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

                        // إنشاء كائن البيانات
                        DataObject dataObject = new DataObject();

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
                            // إضافة البيانات بتنسيقات متعددة لضمان التوافق
                            string dataToTransfer = string.Join("|", filePaths);
                            dataObject.SetData(DataFormats.StringFormat, dataToTransfer);

                            // إضافة مرجع للـ PictureBox للسماح بالتبديل
                            dataObject.SetData("ReturnToPanel1", pb);

                            // إضافة معرف المصدر
                            dataObject.SetData("DragSource", "panel1");

                            pb.DoDragDrop(dataObject, DragDropEffects.Move);
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

        private void savebtn_Click(object sender, EventArgs e)
        {
            string companyClient = Company_Client.Text.Trim();
            DateTime receptionDate = Reception_Date.Value.Date;
            if (!Time.MaskCompleted)
            {
                MessageBox.Show("Please enter a valid time (HH:mm).", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Time.Focus();
                return; // إيقاف العملية
            }

            // 2. التحقق من أن النص المدخل يمثل وقتاً صالحاً (خطوة أمان إضافية)
            if (!DateTime.TryParseExact(Time.Text, "HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
            {
                MessageBox.Show("The time entered is not valid. Please use 24-hour format (e.g., 14:30).", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Time.Focus();
                return; // إيقاف العملية
            }

            string receptionTime = Time.Text; if (!flowLayoutPanel1.Controls.OfType<PictureBox>().Any())
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

            if (!Directory.Exists(projectFolder))
            {
                Directory.CreateDirectory(projectFolder);
            }

            // 1. حفظ عناصر flowLayoutPanel1 (الصور الأساسية وملفات الوورد)
            foreach (Control control in flowLayoutPanel1.Controls)
            {
                if (control is PictureBox pb && pb.Tag != null) // تم إزالة شرط وجود الصورة للسماح بملفات الوورد التي ليس لها صورة حقيقية
                {
                    try
                    {
                        // تكوين اسم العنصر
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
                        string fullItemName = imageName + extension;
                        string destinationPath = Path.Combine(projectFolder, fullItemName);

                        // <<-- بداية التعديل الرئيسي هنا -->>
                        // التحقق من نوع الملف وحفظه بالطريقة الصحيحة
                        if (extension.Equals(".docx", StringComparison.OrdinalIgnoreCase) || extension.Equals(".doc", StringComparison.OrdinalIgnoreCase))
                        {
                            // إذا كان ملف وورد، استخدم File.Copy
                            File.Copy(originalPath, destinationPath, true);
                        }
                        else if (pb.Image != null)
                        {
                            // إذا كان صورة، استخدم Image.Save
                            pb.Image.Save(destinationPath);
                        }
                        else
                        {
                            // في حال كان Tag موجوداً ولكن لا توجد صورة (حالة غير متوقعة)
                            allSaved = false;
                            MessageBox.Show($"Could not save item (no image found): {originalPath}");
                            continue; // انتقل إلى العنصر التالي
                        }
                        // <<-- نهاية التعديل الرئيسي هنا -->>

                        // تعديل تاريخ الملف
                        File.SetCreationTime(destinationPath, DateTime.Now);
                        File.SetLastWriteTime(destinationPath, DateTime.Now);

                        string insertImageQuery = @"INSERT INTO items (project_id, image_name, image_path, registration_date, last_update_date) 
                                  VALUES (@project_id, @image_name, @image_path, CURRENT_DATE, CURRENT_TIMESTAMP)";

                        List<SQLiteParameter> imageParameters = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@project_id", projectId),
                    new SQLiteParameter("@image_name", fullItemName),
                    new SQLiteParameter("@image_path", destinationPath)
                };

                        bool imageSaved = db.ExecuteNonQuery(insertImageQuery, imageParameters);
                        if (!imageSaved)
                        {
                            allSaved = false;
                            MessageBox.Show($"Failed to save database record for: {fullItemName}");
                        }
                        else
                        {
                            imageCounter++;
                        }
                    }
                    catch (Exception ex)
                    {
                        allSaved = false;
                        MessageBox.Show($"Error saving item {imageCounter}: {ex.Message}");
                    }
                }
            }

            // 2. حفظ صورة imageApostille (هذه ستبقى للصور فقط)
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

                    // حفظ الصورة مع تعديل التاريخ
                    imageApostille.Image.Save(imagePath);
                    File.SetCreationTime(imagePath, DateTime.Now);
                    File.SetLastWriteTime(imagePath, DateTime.Now);

                    string insertImageQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) 
                              VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";

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

            // 3. حفظ عناصر flowLayoutPanel2 (المرفقات)
            string attachmentType = "A";
            foreach (Control control in flowLayoutPanel2.Controls)
            {
                if (control is PictureBox pb && pb.Tag != null) // تم إزالة شرط وجود الصورة
                {
                    try
                    {
                        string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_{attachmentType}";
                        string originalPath = pb.Tag.ToString();
                        string extension = Path.GetExtension(originalPath);
                        string fullItemName = imageName + extension;
                        string destinationPath = Path.Combine(projectFolder, fullItemName);

                        // <<-- نفس التعديل هنا لـ flowLayoutPanel2 -->>
                        if (extension.Equals(".docx", StringComparison.OrdinalIgnoreCase) || extension.Equals(".doc", StringComparison.OrdinalIgnoreCase))
                        {
                            File.Copy(originalPath, destinationPath, true);
                        }
                        else if (pb.Image != null)
                        {
                            pb.Image.Save(destinationPath);
                        }
                        else
                        {
                            allSaved = false;
                            MessageBox.Show($"Could not save attachment (no image found): {originalPath}");
                            continue;
                        }

                        File.SetCreationTime(destinationPath, DateTime.Now);
                        File.SetLastWriteTime(destinationPath, DateTime.Now);

                        string insertImageQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) 
                                  VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";

                        List<SQLiteParameter> imageParameters = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@project_id", projectId),
                    new SQLiteParameter("@image_name", fullItemName),
                    new SQLiteParameter("@image_path", destinationPath),
                    new SQLiteParameter("@attachment_type", attachmentType)
                };

                        bool imageSaved = db.ExecuteNonQuery(insertImageQuery, imageParameters);
                        if (!imageSaved)
                        {
                            allSaved = false;
                            MessageBox.Show($"Failed to save attachment record: {fullItemName}");
                        }
                        else
                        {
                            imageCounter++;
                        }
                    }
                    catch (Exception ex)
                    {
                        allSaved = false;
                        MessageBox.Show($"Error saving attachment {imageCounter}: {ex.Message}");
                    }
                }
            }

            // 4. حفظ ملفات Word المرفوعة (OCR) - هذا الجزء كان صحيحاً بالفعل
            int ocrFileNumber = imageCounter;
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
                            File.SetCreationTime(wordPath, DateTime.Now);
                            File.SetLastWriteTime(wordPath, DateTime.Now);

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
                                MessageBox.Show($"Failed to save Word file record: {fullWordFileName}");
                            }
                            else
                            {
                                ocrFileNumber++; // زيادة العداد لكل ملف وورد
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
            imageCounter = ocrFileNumber; // تحديث العداد الرئيسي

            // 5. إنشاء ملف Word جديد بإسم Google Drive
            try
            {
                string googleDriverFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Google Drive.docx";
                string googleDriverPath = Path.Combine(projectFolder, googleDriverFileName);
                using (var fs = File.Create(googleDriverPath)) { }

                File.SetCreationTime(googleDriverPath, DateTime.Now);
                File.SetLastWriteTime(googleDriverPath, DateTime.Now);

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

            // 6. إنشاء ملف Word جديد بإسم Traducción Preliminar
            try
            {
                string traduccionFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Traducción Preliminar.docx";
                string traduccionPath = Path.Combine(projectFolder, traduccionFileName);
                using (var fs = File.Create(traduccionPath)) { }

                File.SetCreationTime(traduccionPath, DateTime.Now);
                File.SetLastWriteTime(traduccionPath, DateTime.Now);

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

            // 7. إنشاء ملف Word جديد بإسم Informe revisión
            try
            {
                string informeFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Informe revisión.docx";
                string informePath = Path.Combine(projectFolder, informeFileName);
                using (var fs = File.Create(informePath)) { }

                File.SetCreationTime(informePath, DateTime.Now);
                File.SetLastWriteTime(informePath, DateTime.Now);

                string insertInformeQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) 
                          VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";

                List<SQLiteParameter> informeParams = new List<SQLiteParameter>
        {
            new SQLiteParameter("@project_id", projectId),
            new SQLiteParameter("@image_name", informeFileName),
            new SQLiteParameter("@image_path", informePath),
            new SQLiteParameter("@attachment_type", "Informe revisión") // تم تصحيح النوع هنا
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
            pb.BorderStyle = BorderStyle.None;
            pb.Tag = filePath;

            // --- بداية التعديل المهم هنا ---
            string ext = Path.GetExtension(filePath).ToLower();

            if (ext == ".docx" || ext == ".doc")
            {
                // التعامل مع ملفات الوورد بشكل خاص
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                pb.BackColor = Color.Transparent;
                try
                {
                    // قم بتعيين أيقونة الوورد من الموارد مباشرة
                    pb.Image = Properties.Resources.wordicon;
                }
                catch
                {
                    // في حال عدم وجود المورد، ارسم نصًا كحل بديل
                    pb.BackColor = Color.LightGray;
                    pb.Paint += (s, e_paint) =>
                    {
                        e_paint.Graphics.DrawString("DOCX", new Font("Arial", 10, FontStyle.Bold),
                            Brushes.Black, new PointF(35, 50));
                    };
                }
            }
            else
            {
                // التعامل مع ملفات الصور (المنطق الأصلي)
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                pb.BackColor = Color.Transparent;
                try
                {
                    using (var imgTemp = Image.FromFile(filePath))
                    {
                        pb.Image = new Bitmap(imgTemp);
                    }
                }
                catch
                {
                    // في حال فشل تحميل الصورة
                    pb.BackColor = Color.LightGray;
                }
            }
            // --- نهاية التعديل ---

            pb.Location = new Point(x, y);

            pb.DoubleClick += OpenImage_DoubleClick;
            pb.MouseDown += Pb_MouseDown_Panel1;
            pb.MouseMove += Pb_MouseMove_Panel1;
            pb.MouseUp += Pb_MouseUp_Panel1;
            pb.Click += Pb_Click_Panel1;
            pb.Paint += PictureBox_Paint_Selection;
            panel1.Controls.Add(pb);

        
        }
        private void AddFile_Load(object sender, EventArgs e)
        {
            Delivery_Date.Items.Add(new KeyValuePair<string, int>("Default", 3));
            Delivery_Date.Items.Add(new KeyValuePair<string, int>("Urgent (2 days)", 2));
            Delivery_Date.Items.Add(new KeyValuePair<string, int>("Very Urgent (1 day)", 1));
            Delivery_Date.DisplayMember = "Key";
            Delivery_Date.ValueMember = "Value";
            Delivery_Date.SelectedIndex = 0;
            Reception_Date.Value = DateTime.Now;
            LoadClientsAndCompanies();
            Time.Text = DateTime.Now.ToString("HH:mm"); // تعيين الوقت الحالي كنص منسق
            LoadDocumentTypesToComboBox();
            LoadLanguagePairsToComboBox();
            SetupMultiPanelDragSupport();
            Time.KeyUp += Time_KeyUp_AutoJump;
            Time.TextChanged += Time_TextChanged_Validate; // ربط حدث التحقق


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

                        // التحقق من وجود عناصر محددة أخرى للسحب المتعدد
                        List<PictureBoxDragInfo> multiPanelItems = new List<PictureBoxDragInfo>();

                        // إضافة imageApostille
                        if (pb.Tag != null)
                        {
                            multiPanelItems.Add(new PictureBoxDragInfo
                            {
                                PictureBox = pb,
                                SourceType = "imageApostille",
                                FilePath = pb.Tag.ToString()
                            });
                        }

                        // إضافة العناصر المحددة من flowLayoutPanel1
                        foreach (var selectedPb in selectedPictureBoxesFlow1)
                        {
                            if (selectedPb.Image != null && selectedPb.Tag != null)
                            {
                                multiPanelItems.Add(new PictureBoxDragInfo
                                {
                                    PictureBox = selectedPb,
                                    SourceType = "flowLayoutPanel1",
                                    FilePath = selectedPb.Tag.ToString()
                                });
                            }
                        }

                        // إضافة العناصر المحددة من flowLayoutPanel2
                        foreach (var selectedPb in selectedPictureBoxesFlow2)
                        {
                            if (selectedPb.Image != null && selectedPb.Tag != null)
                            {
                                multiPanelItems.Add(new PictureBoxDragInfo
                                {
                                    PictureBox = selectedPb,
                                    SourceType = "flowLayoutPanel2",
                                    FilePath = selectedPb.Tag.ToString()
                                });
                            }
                        }

                        if (multiPanelItems.Count > 1)
                        {
                            // سحب متعدد
                            data.SetData("MultiPanelDrag", multiPanelItems);
                            var filePaths = multiPanelItems.Select(item => item.FilePath).Where(fp => fp != null);
                            data.SetData(DataFormats.StringFormat, string.Join("|", filePaths));
                        }
                        else
                        {
                            // سحب فردي عادي
                            data.SetData("ReturnToPanel1", pb);
                            if (pb.Tag != null)
                            {
                                data.SetData(DataFormats.StringFormat, pb.Tag.ToString());
                            }
                            data.SetData("FromImageApostille", true);
                        }

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
                if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    if (!string.IsNullOrEmpty(data) && data.Contains("|"))
                    {
                        e.Effect = DragDropEffects.None; 
                        return; 
                    }
                }

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 1)
                    {
                        e.Effect = DragDropEffects.None; 
                        return; 
                    }
                }

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
        private void ClearAllSelections()
        {
            var tempSelected = new List<PictureBox>(selectedPictureBoxes);
            var tempSelectedFlow1 = new List<PictureBox>(selectedPictureBoxesFlow1);
            var tempSelectedFlow2 = new List<PictureBox>(selectedPictureBoxesFlow2);
            selectedPictureBoxes.Clear();
            selectedPictureBoxesFlow1.Clear();
            selectedPictureBoxesFlow2.Clear();
            foreach (var pb in tempSelected) pb.Invalidate();
            foreach (var pb in tempSelectedFlow1) pb.Invalidate();
            foreach (var pb in tempSelectedFlow2) pb.Invalidate();
        }
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            panel1.Focus(); // إعطاء التركيز لـ panel1 عند النقر على أي مكان فيه
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
            bool isSelected = selectedPictureBoxes.Contains(pb) ||
                              selectedPictureBoxesFlow1.Contains(pb) ||
                              selectedPictureBoxesFlow2.Contains(pb);
            if (isSelected)
            {
                Color overlayColor = Color.FromArgb(100, SystemColors.Highlight);
                using (Brush overlayBrush = new SolidBrush(overlayColor))
                {
                    e.Graphics.FillRectangle(overlayBrush, pb.ClientRectangle);
                }
            }
        }
        private void HandleSelectAll(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                Control focusedControl = this.ActiveControl;

                if (focusedControl == panel1)
                {
                    ClearAllSelections();
                    foreach (PictureBox pb in panel1.Controls.OfType<PictureBox>())
                    {
                        selectedPictureBoxes.Add(pb);
                        pb.Invalidate();
                    }
                    panel1.Invalidate();
                }
                else if (focusedControl == flowLayoutPanel1)
                {
                    ClearAllSelections();
                    foreach (PictureBox pb in flowLayoutPanel1.Controls.OfType<PictureBox>().Where(p => p.Image != null))
                    {
                        selectedPictureBoxesFlow1.Add(pb);
                        pb.Invalidate();
                    }
                }
                else if (focusedControl == flowLayoutPanel2)
                {
                    ClearAllSelections();
                    foreach (PictureBox pb in flowLayoutPanel2.Controls.OfType<PictureBox>().Where(p => p.Image != null))
                    {
                        selectedPictureBoxesFlow2.Add(pb);
                        pb.Invalidate();
                    }
                }
            }
        }
        private void HandleMultiPanelSelection(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.A)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                // تحديد جميع الصور في جميع اللوحات
                ClearAllSelections();

                // تحديد صور panel1
                foreach (PictureBox pb in panel1.Controls.OfType<PictureBox>())
                {
                    selectedPictureBoxes.Add(pb);
                    pb.Invalidate();
                }

                // تحديد صور flowLayoutPanel1
                foreach (PictureBox pb in flowLayoutPanel1.Controls.OfType<PictureBox>().Where(p => p.Image != null))
                {
                    selectedPictureBoxesFlow1.Add(pb);
                    pb.Invalidate();
                }

                // تحديد صور flowLayoutPanel2
                foreach (PictureBox pb in flowLayoutPanel2.Controls.OfType<PictureBox>().Where(p => p.Image != null))
                {
                    selectedPictureBoxesFlow2.Add(pb);
                    pb.Invalidate();
                }

                // تحديد imageApostille إذا كان يحتوي على صورة
                PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
                if (imageApostille?.Image != null)
                {
                    selectedPictureBoxes.Add(imageApostille);
                    imageApostille.Invalidate();
                }
            }
        }
        private List<PictureBox> GetSelectionListForPanel(FlowLayoutPanel panel)
        {
            if (panel == flowLayoutPanel1)
                return selectedPictureBoxesFlow1;
            if (panel == flowLayoutPanel2)
                return selectedPictureBoxesFlow2;
            return new List<PictureBox>();
        }

        private string GetMultiDragKeyForPanel(FlowLayoutPanel panel)
        {
            return (panel == flowLayoutPanel1) ? "MultiDragFlow1" : "MultiDragFlow2";
        }

        private string GetReorderKeyForPanel(FlowLayoutPanel panel)
        {
            return (panel == flowLayoutPanel1) ? "FlowPanelReorder" : "FlowPanel2Reorder";
        }

        private void SetupFlowLayoutPanel(FlowLayoutPanel panel)
        {
            panel.AllowDrop = true;
            panel.DragEnter += Generic_FlowLayoutPanel_DragEnter;
            panel.DragDrop += Generic_FlowLayoutPanel_DragDrop;
            panel.DragOver += Generic_FlowLayoutPanel_DragOver;
            panel.DragLeave += Generic_FlowLayoutPanel_DragLeave;
            panel.MouseDown += Generic_FlowLayoutPanel_MouseDown_ForSelection;
            panel.MouseMove += Generic_FlowLayoutPanel_MouseMove_ForSelection;
            panel.MouseUp += Generic_FlowLayoutPanel_MouseUp_ForSelection;
            panel.Paint += Generic_FlowLayoutPanel_Paint_SelectionRectangle;
        }

        private PictureBox CreateNewPictureBoxForPanel(FlowLayoutPanel panel)
        {
            var pic = new Guna.UI2.WinForms.Guna2PictureBox
            {
                Name = $"_{panel.Name}_{panel.Controls.Count + 1}",
                Width = 130,
                Height = 157,
                BorderStyle = BorderStyle.None,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent,
                Margin = new Padding(5),
                AllowDrop = true
            };

            pic.DragEnter += Generic_Pic_DragEnter_FlowPanel;
            pic.DragDrop += Generic_Pic_DragDrop_FlowPanel;
            pic.MouseDown += Generic_Pic_MouseDown_FlowPanel;
            pic.MouseMove += Generic_Pic_MouseMove_FlowPanel;
            pic.MouseUp += Generic_Pic_MouseUp_FlowPanel;
            pic.Click += Generic_Pic_Click_FlowPanel;
            pic.DoubleClick += OpenImage_DoubleClick;
            pic.Paint += PictureBox_Paint_Selection;

            return pic;
        }

        private PictureBox FindEmptyPictureBoxInPanel(FlowLayoutPanel panel)
        {
            return panel.Controls.OfType<PictureBox>().FirstOrDefault(pb => pb.Image == null);
        }

        private int CalculateInsertionIndexForPanel(FlowLayoutPanel panel, Point clientPoint)
        {
            var pictureBoxes = panel.Controls.OfType<PictureBox>().ToList();

            if (pictureBoxes.Count == 0)
                return 0;

            for (int i = 0; i < pictureBoxes.Count; i++)
            {
                var pb = pictureBoxes[i];

                // تجاهل العنصر المسحوب إذا كان من نفس اللوحة
                if (pb == draggedPictureBox)
                    continue;

                Rectangle bounds = pb.Bounds;

                // إذا كان المؤشر في النصف الأيسر من الصورة، أدرج قبلها
                if (clientPoint.X < bounds.Left + (bounds.Width / 2))
                    return Math.Max(0, panel.Controls.GetChildIndex(pb));
            }

            // إذا لم نجد موضع مناسب، أدرج في النهاية
            return panel.Controls.Count;
        }

        private void Generic_Pic_Click_FlowPanel(object sender, EventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb?.Image == null) return;
            FlowLayoutPanel parentPanel = pb.Parent as FlowLayoutPanel;
            if (parentPanel == null) return;

            var selectionList = GetSelectionListForPanel(parentPanel);
            if ((DateTime.Now - lastClickTime).TotalMilliseconds < SystemInformation.DoubleClickTime) return;
            lastClickTime = DateTime.Now;

            if (Control.ModifierKeys == Keys.Control)
            {
                if (selectionList.Contains(pb)) selectionList.Remove(pb);
                else selectionList.Add(pb);
            }
            else if (Control.ModifierKeys == Keys.Shift && selectionList.Any())
            {
                var allPictures = parentPanel.Controls.OfType<PictureBox>().ToList();
                int currentIndex = allPictures.IndexOf(pb);
                int lastSelectedIndex = allPictures.IndexOf(selectionList.Last());
                if (currentIndex != -1 && lastSelectedIndex != -1)
                {
                    int start = Math.Min(currentIndex, lastSelectedIndex);
                    int end = Math.Max(currentIndex, lastSelectedIndex);
                    for (int i = start; i <= end; i++)
                    {
                        if (!selectionList.Contains(allPictures[i]))
                            selectionList.Add(allPictures[i]);
                    }
                }
            }
            else
            {
                ClearAllSelections();
                selectionList.Add(pb);
            }
            parentPanel.Controls.OfType<PictureBox>().ToList().ForEach(p => p.Invalidate());
            parentPanel.Focus();
        }

        private void Generic_Pic_MouseDown_FlowPanel(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStartPoint = e.Location;
                isDragging = false;
                draggedPictureBox = sender as PictureBox;
                dragSourcePanel = draggedPictureBox?.Parent as FlowLayoutPanel;
            }
        }

        private void Generic_Pic_MouseMove_FlowPanel(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && draggedPictureBox?.Image != null && !isDragging)
            {
                Size dragSize = SystemInformation.DragSize;
                if (Math.Abs(e.X - dragStartPoint.X) > dragSize.Width || Math.Abs(e.Y - dragStartPoint.Y) > dragSize.Height)
                {
                    isDragging = true;
                    DataObject dataObject = new DataObject();

                    // التحقق من وجود عناصر محددة من لوحات متعددة
                    List<PictureBoxDragInfo> multiPanelItems = new List<PictureBoxDragInfo>();

                    // جمع العناصر المحددة من flowLayoutPanel1
                    foreach (var pb in selectedPictureBoxesFlow1)
                    {
                        if (pb.Image != null && pb.Tag != null)
                        {
                            multiPanelItems.Add(new PictureBoxDragInfo
                            {
                                PictureBox = pb,
                                SourceType = "flowLayoutPanel1",
                                FilePath = pb.Tag.ToString()
                            });
                        }
                    }

                    // جمع العناصر المحددة من flowLayoutPanel2
                    foreach (var pb in selectedPictureBoxesFlow2)
                    {
                        if (pb.Image != null && pb.Tag != null)
                        {
                            multiPanelItems.Add(new PictureBoxDragInfo
                            {
                                PictureBox = pb,
                                SourceType = "flowLayoutPanel2",
                                FilePath = pb.Tag.ToString()
                            });
                        }
                    }

                    // إضافة imageApostille إذا كان محدداً
                    PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
                    if (imageApostille?.Image != null && selectedPictureBoxes.Contains(imageApostille))
                    {
                        multiPanelItems.Add(new PictureBoxDragInfo
                        {
                            PictureBox = imageApostille,
                            SourceType = "imageApostille",
                            FilePath = imageApostille.Tag?.ToString()
                        });
                    }

                    // إذا كان هناك عناصر من لوحات متعددة
                    if (multiPanelItems.Count > 1)
                    {
                        dataObject.SetData("MultiPanelDrag", multiPanelItems);
                        var filePaths = multiPanelItems.Select(item => item.FilePath).Where(fp => fp != null);
                        dataObject.SetData(DataFormats.StringFormat, string.Join("|", filePaths));
                    }
                    else
                    {
                        // السحب العادي من لوحة واحدة
                        var selectionList = GetSelectionListForPanel(dragSourcePanel);
                        string multiDragKey = GetMultiDragKeyForPanel(dragSourcePanel);
                        string reorderKey = GetReorderKeyForPanel(dragSourcePanel);

                        var itemsToDrag = new List<PictureBox>();
                        if (selectionList.Count > 1 && selectionList.Contains(draggedPictureBox))
                        {
                            itemsToDrag.AddRange(selectionList);
                            dataObject.SetData(multiDragKey, selectionList.ToArray());
                        }
                        else
                        {
                            itemsToDrag.Add(draggedPictureBox);
                        }

                        var filePaths = itemsToDrag.Select(p => p.Tag?.ToString()).Where(t => t != null);
                        if (filePaths.Any())
                        {
                            dataObject.SetData(DataFormats.StringFormat, string.Join("|", filePaths));
                        }

                        dataObject.SetData(reorderKey, draggedPictureBox);
                        dataObject.SetData("ReturnToPanel1", draggedPictureBox);
                        dataObject.SetData("DragSource", dragSourcePanel?.Name ?? "unknown");
                    }

                    draggedPictureBox.DoDragDrop(dataObject, DragDropEffects.Move);
                }
            }
        }
        private void Generic_Pic_MouseUp_FlowPanel(object sender, MouseEventArgs e)
        {
            isDragging = false;
            draggedPictureBox = null;
            if (_insertionIndex != -1)
            {
                _insertionIndex = -1;
                dragSourcePanel?.Invalidate();
            }
            dragSourcePanel = null;
        }

        private void Generic_Pic_DragEnter_FlowPanel(object sender, DragEventArgs e)
        {
            PictureBox targetPic = sender as PictureBox;
            FlowLayoutPanel targetPanel = targetPic?.Parent as FlowLayoutPanel;
            string dragSource = e.Data.GetDataPresent("DragSource") ? e.Data.GetData("DragSource").ToString() : string.Empty;

            if (dragSource == targetPanel?.Name)
            {
                string reorderKey = GetReorderKeyForPanel(targetPanel);
                if (e.Data.GetDataPresent(reorderKey))
                {
                    e.Effect = DragDropEffects.Move;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.StringFormat) ||  
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
        private void Generic_Pic_DragDrop_FlowPanel(object sender, DragEventArgs e)
        {
            PictureBox targetPic = sender as PictureBox;
            if (targetPic == null) return;

            FlowLayoutPanel targetPanel = targetPic.Parent as FlowLayoutPanel;
            if (targetPanel == null) return;

            try
            {
                // الأولوية للتعامل مع السحب الداخلي عبر كائن PictureBox مباشرة
                // هذا يعالج السحب من imageApostille, panel1, أو flowLayoutPanel آخر
                if (e.Data.GetDataPresent("ReturnToPanel1"))
                {
                    var sourcePic = (PictureBox)e.Data.GetData("ReturnToPanel1");
                    if (sourcePic == null || sourcePic == targetPic) return; // لا تسحب وتفلت على نفسها

                    // هذا هو الجزء الأهم: التحقق من وجود صورة في الهدف لتحديد التبديل
                    if (targetPic.Image != null)
                    {
                        // إذا كان الهدف يحتوي على صورة، قم بالتبديل
                        SwapImagesBetweenControls(sourcePic, targetPic);
                    }
                    else // الحالة الثانية: الهدف فارغ، لذا نقوم بعملية "نقل" وليس تبديل
                    {
                        if (sourcePic.Tag != null && sourcePic.Image != null)
                        {
                            // انقل الصورة والبيانات إلى الهدف
                            targetPic.Image = sourcePic.Image;
                            targetPic.Tag = sourcePic.Tag;

                            // تفريغ المصدر بعد النقل
                            sourcePic.Image = null; // هام: لمنع التخلص من الصورة مرتين
                            CleanupSourcePictureBox(sourcePic, DetermineSourceType(sourcePic));
                        }
                    }
                }
                // التعامل مع السحب المتعدد من panel1
                else if (e.Data.GetDataPresent(DataFormats.StringFormat) &&
                         e.Data.GetDataPresent("DragSource") &&
                         e.Data.GetData("DragSource").ToString() == "panel1")
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    if (!string.IsNullOrEmpty(data))
                    {
                        string[] filePaths = data.Split('|');
                        // إذا كان هناك أكثر من ملف، فقم بإضافتهم إلى أماكن فارغة
                        if (filePaths.Length > 1)
                        {
                            foreach (string filePath in filePaths)
                            {
                                if (File.Exists(filePath))
                                {
                                    PictureBox newTargetPic = FindEmptyPictureBoxInPanel(targetPanel) ?? CreateNewPictureBoxForPanel(targetPanel);
                                    if (newTargetPic.Parent == null) targetPanel.Controls.Add(newTargetPic);

                                    using (var imgTemp = Image.FromFile(filePath))
                                    {
                                        newTargetPic.Image?.Dispose();
                                        newTargetPic.Image = new Bitmap(imgTemp);
                                        newTargetPic.Tag = filePath;
                                    }
                                    RemoveImageFromPanel1(filePath);
                                }
                            }
                        }
                    }
                }
                // التعامل مع إفلات الملفات من خارج البرنامج (مثل مستكشف ويندوز)
                else if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0 && File.Exists(files[0]))
                    {
                        string ext = Path.GetExtension(files[0]).ToLower();
                        if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                        {
                            using (var imgTemp = Image.FromFile(files[0]))
                            {
                                targetPic.Image?.Dispose(); // تنظيف الصورة السابقة
                                targetPic.Image = new Bitmap(imgTemp);
                                targetPic.Tag = files[0];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during drag drop: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Generic_FlowLayoutPanel_DragEnter(object sender, DragEventArgs e)
        {
            // السماح بجميع أنواع السحب المدعومة
            if (e.Data.GetDataPresent(DataFormats.StringFormat) ||
                e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetDataPresent("ReturnToPanel1") ||
                e.Data.GetDataPresent("MultiDragFlow1") ||
                e.Data.GetDataPresent("MultiDragFlow2") ||
                e.Data.GetDataPresent("MultiPanelDrag"))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Generic_FlowLayoutPanel_DragOver(object sender, DragEventArgs e)
        {
            FlowLayoutPanel panel = sender as FlowLayoutPanel;
            if (panel == null) return;

            _currentDragOverPanel = panel;

            string reorderKey = GetReorderKeyForPanel(panel);
            bool isInternalReorder = e.Data.GetDataPresent(reorderKey) && dragSourcePanel == panel;

            // التحقق من السحب الخارجي (من لوحة أخرى أو مصادر خارجية)
            bool isExternalDrag = e.Data.GetDataPresent("ReturnToPanel1") ||
                                 e.Data.GetDataPresent("MultiDragFlow1") ||
                                 e.Data.GetDataPresent("MultiDragFlow2") ||
                                 e.Data.GetDataPresent("MultiPanelDrag") ||
                                 e.Data.GetDataPresent(DataFormats.FileDrop) ||
                                 (e.Data.GetDataPresent("DragSource") &&
                                  e.Data.GetData("DragSource").ToString() != panel.Name);

            if (isInternalReorder || isExternalDrag)
            {
                e.Effect = DragDropEffects.Move;
                Point clientPoint = panel.PointToClient(new Point(e.X, e.Y));
                int newIndex = CalculateInsertionIndexForPanel(panel, clientPoint);

                _isDragOverExternalPanel = !isInternalReorder;

                if (newIndex != _insertionIndex)
                {
                    _insertionIndex = newIndex;
                    panel.Invalidate();
                }
            }
            else
            {
                if (_insertionIndex != -1)
                {
                    _insertionIndex = -1;
                    _isDragOverExternalPanel = false;
                    panel.Invalidate();
                }
                e.Effect = DragDropEffects.None;
            }
        }
        private void Generic_FlowLayoutPanel_DragLeave(object sender, EventArgs e)
        {
            FlowLayoutPanel panel = sender as FlowLayoutPanel;
            if (panel == _currentDragOverPanel)
            {
                if (_insertionIndex != -1)
                {
                    _insertionIndex = -1;
                    _isDragOverExternalPanel = false;
                    panel?.Invalidate();
                }
                _currentDragOverPanel = null;
            }
        }

        //*************
        private void Generic_FlowLayoutPanel_DragDrop(object sender, DragEventArgs e)
        {
            FlowLayoutPanel panel = sender as FlowLayoutPanel;
            if (panel == null) return;

            try
            {
                int targetIndex = _insertionIndex != -1 ? _insertionIndex : panel.Controls.Count;

                // **معالجة السحب المتعدد عبر اللوحات (MultiPanelDrag) - الأولوية الأعلى**
                if (e.Data.GetDataPresent("MultiPanelDrag"))
                {
                    var draggedItems = (List<PictureBoxDragInfo>)e.Data.GetData("MultiPanelDrag");

                    for (int i = 0; i < draggedItems.Count; i++)
                    {
                        var item = draggedItems[i];
                        if (item.PictureBox?.Tag != null)
                        {
                            PictureBox targetPic = CreateNewPictureBoxForPanel(panel);
                            SetPictureBoxContent(targetPic, item.PictureBox.Tag.ToString());

                            panel.Controls.Add(targetPic);
                            if (targetIndex + i < panel.Controls.Count)
                            {
                                panel.Controls.SetChildIndex(targetPic, targetIndex + i);
                            }
                        }
                    }

                    foreach (var item in draggedItems)
                    {
                        CleanupSourcePictureBox(item.PictureBox, item.SourceType);
                    }

                    ClearAllSelections();
                    return;
                }

                string reorderKey = GetReorderKeyForPanel(panel);

                // **الحالة 1: إعادة ترتيب داخل نفس اللوحة - هذا هو الجزء المهم المطلوب تصحيحه**
                if (e.Data.GetDataPresent(reorderKey) && dragSourcePanel == panel)
                {
                    string multiDragKey = GetMultiDragKeyForPanel(panel);
                    List<PictureBox> picturesToMove;

                    if (e.Data.GetDataPresent(multiDragKey))
                    {
                        picturesToMove = ((PictureBox[])e.Data.GetData(multiDragKey)).ToList();
                    }
                    else
                    {
                        picturesToMove = new List<PictureBox> { (PictureBox)e.Data.GetData(reorderKey) };
                    }

                    // *** التصحيح الرئيسي هنا ***
                    if (targetIndex != -1 && picturesToMove.Any())
                    {
                        // 1. احصل على قائمة جميع العناصر مع فهارسهم الحالية
                        var allControls = panel.Controls.OfType<PictureBox>().ToList();
                        var movedControlsIndices = picturesToMove.Select(p => allControls.IndexOf(p)).OrderBy(i => i).ToList();

                        // 2. احسب المؤشر المُصحح بناءً على العناصر التي ستُزال قبل المؤشر الهدف
                        int adjustedTargetIndex = targetIndex;
                        foreach (int index in movedControlsIndices)
                        {
                            if (index < targetIndex)
                            {
                                adjustedTargetIndex--;
                            }
                        }

                        // 3. تأكد من أن المؤشر ضمن النطاق الصحيح
                        adjustedTargetIndex = Math.Max(0, Math.Min(adjustedTargetIndex, allControls.Count - picturesToMove.Count));

                        // 4. قم بإزالة العناصر المسحوبة مؤقتاً (بترتيب عكسي لتجنب تغيير الفهارس)
                        foreach (var pic in picturesToMove.OrderByDescending(p => allControls.IndexOf(p)))
                        {
                            panel.Controls.Remove(pic);
                        }

                        // 5. أعد إدراج العناصر في المكان الصحيح
                        for (int i = 0; i < picturesToMove.Count; i++)
                        {
                            panel.Controls.Add(picturesToMove[i]);
                            int finalIndex = Math.Min(adjustedTargetIndex + i, panel.Controls.Count - 1);
                            panel.Controls.SetChildIndex(picturesToMove[i], finalIndex);
                        }
                    }
                }
                // الحالة 2: السحب المتعدد من panel1 مباشرة
                else if (e.Data.GetDataPresent(DataFormats.StringFormat) &&
                         e.Data.GetDataPresent("DragSource") &&
                         e.Data.GetData("DragSource").ToString() == "panel1")
                {
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    if (!string.IsNullOrEmpty(data))
                    {
                        string[] filePaths = data.Split('|');
                        var newPictureBoxes = new List<PictureBox>();

                        foreach (string filePath in filePaths)
                        {
                            if (File.Exists(filePath))
                            {
                                PictureBox targetPic = CreateNewPictureBoxForPanel(panel);
                                SetPictureBoxContent(targetPic, filePath);
                                newPictureBoxes.Add(targetPic);
                            }
                        }

                        for (int i = 0; i < newPictureBoxes.Count; i++)
                        {
                            panel.Controls.Add(newPictureBoxes[i]);
                            if (targetIndex + i < panel.Controls.Count)
                            {
                                panel.Controls.SetChildIndex(newPictureBoxes[i], targetIndex + i);
                            }
                        }

                        foreach (string filePath in filePaths)
                        {
                            RemoveImageFromPanel1(filePath);
                        }
                    }
                }
                // **الحالة 3: السحب المتعدد بين FlowLayoutPanels**
                else if (e.Data.GetDataPresent("MultiDragFlow1") || e.Data.GetDataPresent("MultiDragFlow2"))
                {
                    PictureBox[] draggedPictures = null;
                    FlowLayoutPanel sourcePanel = null;

                    if (e.Data.GetDataPresent("MultiDragFlow1"))
                    {
                        draggedPictures = (PictureBox[])e.Data.GetData("MultiDragFlow1");
                        sourcePanel = flowLayoutPanel1;
                    }
                    else if (e.Data.GetDataPresent("MultiDragFlow2"))
                    {
                        draggedPictures = (PictureBox[])e.Data.GetData("MultiDragFlow2");
                        sourcePanel = flowLayoutPanel2;
                    }

                    if (draggedPictures != null && draggedPictures.Length > 0)
                    {
                        var imageDataList = new List<(Image image, object tag)>();
                        foreach (var draggedPic in draggedPictures)
                        {
                            if (draggedPic.Image != null && draggedPic.Tag != null)
                            {
                                Image imageCopy = new Bitmap(draggedPic.Image);
                                imageDataList.Add((imageCopy, draggedPic.Tag));
                            }
                        }

                        var sourceSelectionList = GetSelectionListForPanel(sourcePanel);
                        foreach (var draggedPic in draggedPictures.ToList())
                        {
                            sourceSelectionList.Remove(draggedPic);
                            sourcePanel.Controls.Remove(draggedPic);
                            draggedPic.Image?.Dispose();
                            draggedPic.Dispose();
                        }

                        var newPictureBoxes = new List<PictureBox>();
                        foreach (var (image, tag) in imageDataList)
                        {
                            PictureBox targetPic = CreateNewPictureBoxForPanel(panel);
                            targetPic.Image?.Dispose();
                            targetPic.Image = image;
                            targetPic.Tag = tag;
                            newPictureBoxes.Add(targetPic);
                        }

                        for (int i = 0; i < newPictureBoxes.Count; i++)
                        {
                            panel.Controls.Add(newPictureBoxes[i]);
                            if (targetIndex + i < panel.Controls.Count)
                            {
                                panel.Controls.SetChildIndex(newPictureBoxes[i], targetIndex + i);
                            }
                        }

                        ClearAllSelections();
                    }
                }
                // **الحالة 4: السحب الفردي بين اللوحات**
                else if (e.Data.GetDataPresent("ReturnToPanel1"))
                {
                    var sourceCtrl = (PictureBox)e.Data.GetData("ReturnToPanel1");
                    if (sourceCtrl?.Tag != null)
                    {
                        string filePath = sourceCtrl.Tag.ToString();
                        PictureBox targetPic = CreateNewPictureBoxForPanel(panel);
                        SetPictureBoxContent(targetPic, filePath);

                        panel.Controls.Add(targetPic);
                        if (targetIndex < panel.Controls.Count)
                        {
                            panel.Controls.SetChildIndex(targetPic, targetIndex);
                        }

                        string sourceType = DetermineSourceType(sourceCtrl);
                        CleanupSourcePictureBox(sourceCtrl, sourceType);
                    }
                }
                // الحالة 5: إفلات من خارج البرنامج
                else if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    var newPictureBoxes = new List<PictureBox>();

                    foreach (string filePath in files)
                    {
                        if (File.Exists(filePath))
                        {
                            PictureBox targetPic = CreateNewPictureBoxForPanel(panel);
                            SetPictureBoxContent(targetPic, filePath);

                            if (targetPic.Image != null)
                            {
                                newPictureBoxes.Add(targetPic);
                            }
                            else
                            {
                                targetPic.Dispose();
                            }
                        }
                    }

                    for (int i = 0; i < newPictureBoxes.Count; i++)
                    {
                        panel.Controls.Add(newPictureBoxes[i]);
                        if (targetIndex + i < panel.Controls.Count)
                        {
                            panel.Controls.SetChildIndex(newPictureBoxes[i], targetIndex + i);
                        }
                    }
                }

                ClearAllSelections();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in drag drop operation: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _insertionIndex = -1;
                _isDragOverExternalPanel = false;
                _currentDragOverPanel = null;
                panel.Invalidate();
                isDragging = false;
                draggedPictureBox = null;
                dragSourcePanel = null;
            }
        }
        private void Generic_FlowLayoutPanel_MouseDown_ForSelection(object sender, MouseEventArgs e)
        {
            FlowLayoutPanel panel = sender as FlowLayoutPanel;
            if (panel == null || e.Button != MouseButtons.Left) return;

            if (panel.GetChildAtPoint(e.Location) is PictureBox)
            {
                isSelecting = false;
                return;
            }

            isSelecting = true;
            selectionStartPoint = e.Location;
            if (Control.ModifierKeys != Keys.Control)
            {
                ClearSelectionForPanel(panel);
            }
            panel.Invalidate();
        }

        private void Generic_FlowLayoutPanel_MouseMove_ForSelection(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                int x = Math.Min(selectionStartPoint.X, e.X);
                int y = Math.Min(selectionStartPoint.Y, e.Y);
                selectionRectangle = new Rectangle(x, y, Math.Abs(selectionStartPoint.X - e.X), Math.Abs(selectionStartPoint.Y - e.Y));
                (sender as FlowLayoutPanel)?.Invalidate();
            }
        }

        private void Generic_FlowLayoutPanel_MouseUp_ForSelection(object sender, MouseEventArgs e)
        {
            FlowLayoutPanel panel = sender as FlowLayoutPanel;
            if (panel == null || !isSelecting) return;

            isSelecting = false;
            var selectionList = GetSelectionListForPanel(panel);

            foreach (PictureBox pb in panel.Controls.OfType<PictureBox>())
            {
                if (selectionRectangle.IntersectsWith(pb.Bounds) && !selectionList.Contains(pb))
                {
                    selectionList.Add(pb);
                    pb.Invalidate();
                }
            }
            selectionRectangle = Rectangle.Empty;
            panel.Invalidate();
        }

        private void Generic_FlowLayoutPanel_Paint_SelectionRectangle(object sender, PaintEventArgs e)
        {
            FlowLayoutPanel panel = sender as FlowLayoutPanel;
            if (panel == null) return;

            // رسم مستطيل التحديد
            if (isSelecting && selectionRectangle.Width > 1 && selectionRectangle.Height > 1)
            {
                using (Brush brush = new SolidBrush(Color.FromArgb(70, 0, 120, 215)))
                using (Pen pen = new Pen(Color.DodgerBlue, 1))
                {
                    e.Graphics.FillRectangle(brush, selectionRectangle);
                    e.Graphics.DrawRectangle(pen, selectionRectangle);
                }
            }

            // رسم خط الإدراج - يعمل للسحب الداخلي والخارجي
            if (_insertionIndex != -1 && panel == _currentDragOverPanel)
            {
                Point lineStart, lineEnd;
                int margin = 3;

                if (_insertionIndex < panel.Controls.Count)
                {
                    Control target = panel.Controls[_insertionIndex];
                    lineStart = new Point(target.Left - margin, target.Top);
                    lineEnd = new Point(target.Left - margin, target.Bottom);
                }
                else if (panel.Controls.Count > 0)
                {
                    Control last = panel.Controls[panel.Controls.Count - 1];
                    lineStart = new Point(last.Right + margin, last.Top);
                    lineEnd = new Point(last.Right + margin, last.Bottom);
                }
                else
                {
                    lineStart = new Point(panel.Padding.Left, panel.Padding.Top);
                    lineEnd = new Point(panel.Padding.Left, panel.ClientSize.Height - panel.Padding.Bottom);
                }

                // استخدام لون مختلف للسحب الخارجي لتمييزه
                using (Pen insertionPen = new Pen(_isDragOverExternalPanel ? Color.Green : Color.DodgerBlue, 2))
                {
                    e.Graphics.DrawLine(insertionPen, lineStart, lineEnd);
                }
            }
        }
        private void ClearSelectionForPanel(FlowLayoutPanel panel)
        {
            var selectionList = GetSelectionListForPanel(panel);
            if (!selectionList.Any()) return;
            var picturesToClear = new List<PictureBox>(selectionList);
            selectionList.Clear();
            foreach (var pb in picturesToClear) pb.Invalidate();
        }
        private void RemoveImageFromFlowLayoutPanel(FlowLayoutPanel panel, string filePath)
        {
            PictureBox pbToRemove = panel.Controls.OfType<PictureBox>()
                .FirstOrDefault(pb => pb.Tag?.ToString() == filePath);

            if (pbToRemove != null)
            {
                var selectionList = GetSelectionListForPanel(panel);
                selectionList.Remove(pbToRemove);

                panel.Controls.Remove(pbToRemove);
                pbToRemove.Image?.Dispose();
                pbToRemove.Dispose();
            }
        }
        public class PictureBoxDragInfo
        {
            public PictureBox PictureBox { get; set; }
            public string SourceType { get; set; }
            public string FilePath { get; set; }
        }

        private string DetermineSourceType(PictureBox pb)
        {
            if (pb.Name == "imageApostille") return "imageApostille";
            if (pb.Parent == flowLayoutPanel1) return "flowLayoutPanel1";
            if (pb.Parent == flowLayoutPanel2) return "flowLayoutPanel2";
            return "panel1";
        }

        private void CleanupSourcePictureBox(PictureBox pb, string sourceType)
        {
            if (pb == null) return;

            switch (sourceType)
            {
                case "imageApostille":
                    pb.Image?.Dispose();
                    pb.Image = null;
                    pb.Tag = null;
                    break;
                case "flowLayoutPanel1":
                    if (pb.Parent != null)
                    {
                        selectedPictureBoxesFlow1.Remove(pb);
                        pb.Parent.Controls.Remove(pb);
                    }
                    pb.Image?.Dispose();
                    pb.Dispose();
                    break;
                case "flowLayoutPanel2":
                    if (pb.Parent != null)
                    {
                        selectedPictureBoxesFlow2.Remove(pb);
                        pb.Parent.Controls.Remove(pb);
                    }
                    pb.Image?.Dispose();
                    pb.Dispose();
                    break;
                default:
                    if (pb.Parent != null)
                    {
                        pb.Parent.Controls.Remove(pb);
                    }
                    pb.Image?.Dispose();
                    pb.Dispose();
                    break;
            }
        }
        private void SetupMultiPanelDragSupport()
        {
            // إضافة دعم Ctrl+Shift+A لتحديد جميع العناصر
            this.KeyDown += HandleMultiPanelSelection;
            panel1.KeyDown += HandleMultiPanelSelection;
            flowLayoutPanel1.KeyDown += HandleMultiPanelSelection;
            flowLayoutPanel2.KeyDown += HandleMultiPanelSelection;
        }
        //****************
        private void ResetDragState()
        {
            _insertionIndex = -1;
            _isDragOverExternalPanel = false;
            _currentDragOverPanel = null;
            isDragging = false;
            draggedPictureBox = null;
            dragSourcePanel = null;

            panel1?.Invalidate();
            flowLayoutPanel1?.Invalidate();
            flowLayoutPanel2?.Invalidate();
        }

        private void Time_Enter(object sender, EventArgs e)
        {
            this.Time.Select(0, 2);

        }

        private void Time_Click(object sender, EventArgs e)
        {
            this.Time.Select(0, 2);

        }
        private void Time_KeyUp_AutoJump(object sender, KeyEventArgs e)
        {
            MaskedTextBox mtb = sender as MaskedTextBox;
            if (mtb == null) return;

           
            if (mtb.SelectionStart == 2)
            {
        
                mtb.Select(3, 2);
            }
        }
        private void Time_TextChanged_Validate(object sender, EventArgs e)
        {
            MaskedTextBox mtb = sender as MaskedTextBox;
            if (mtb == null) return;

                       mtb.TextChanged -= Time_TextChanged_Validate;

                      if (!mtb.Text.Substring(0, 2).Contains(mtb.PromptChar))
            {
                if (int.TryParse(mtb.Text.Substring(0, 2), out int hour))
                {
                    if (hour > 23)
                    {
                        int currentSelection = mtb.SelectionStart;
                        mtb.Text = "23" + mtb.Text.Substring(2);
                        mtb.SelectionStart = currentSelection;
                    }
                }
            }

          
            if (mtb.MaskCompleted)
            {
                if (int.TryParse(mtb.Text.Substring(3, 2), out int minute))
                {
                    if (minute > 59)
                    {
                        int currentSelection = mtb.SelectionStart;
                        mtb.Text = mtb.Text.Substring(0, 3) + "59";
                        mtb.SelectionStart = currentSelection;
                    }
                }
            }

            mtb.TextChanged += Time_TextChanged_Validate;
        }
        public class KeyboardDragItem
        {
            public string FilePath { get; set; }
            public PictureBox SourceControl { get; set; }
        }

        private void HandleKeyboardDragStart()
        {
            _keyboardDragItems.Clear();

            var allSelected = new List<PictureBox>();
            allSelected.AddRange(selectedPictureBoxes.Where(pb => pb.Name != "imageApostille")); // from panel1 (exclude apostille for now)
            allSelected.AddRange(selectedPictureBoxesFlow1);
            allSelected.AddRange(selectedPictureBoxesFlow2);

            PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            if (imageApostille != null && selectedPictureBoxes.Contains(imageApostille))
            {
                if (!allSelected.Contains(imageApostille))
                {
                    allSelected.Add(imageApostille);
                }
            }

            if (!allSelected.Any()) return;

            foreach (var pb in allSelected)
            {
                if (pb?.Tag != null && pb.Image != null) // التأكد من وجود صورة
                {
                    _keyboardDragItems.Add(new KeyboardDragItem
                    {
                        FilePath = pb.Tag.ToString(),
                        SourceControl = pb
                    });
                }
            }
        }

        private void HandleKeyboardDrop()
        {
            if (!_keyboardDragItems.Any()) return;

            Control targetControl = this.ActiveControl;
            PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;

            if (targetControl == imageApostille)
            {
                // لا يمكن لصق ملف وورد في خانة Apostille
                var itemToCheck = _keyboardDragItems.FirstOrDefault();
                if (itemToCheck != null)
                {
                    string ext = Path.GetExtension(itemToCheck.FilePath).ToLower();
                    if (ext == ".doc" || ext == ".docx")
                    {
                        MessageBox.Show("Cannot paste a Word document into the Apostille slot.", "Invalid Operation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                if (imageApostille.Image != null)
                {
                    return;
                }

                if (_keyboardDragItems.Count != 1)
                {
                    MessageBox.Show("You can only paste a single image into the Apostille slot.", "Invalid Operation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var itemToDrop = _keyboardDragItems[0];
                if (itemToDrop.SourceControl == null || itemToDrop.SourceControl.IsDisposed) return;

                using (var imgTemp = Image.FromFile(itemToDrop.FilePath))
                {
                    imageApostille.Image = new Bitmap(imgTemp);
                }
                imageApostille.Tag = itemToDrop.FilePath;

                string sourceType = DetermineSourceType(itemToDrop.SourceControl);
                CleanupSourcePictureBox(itemToDrop.SourceControl, sourceType);

                if (sourceType == "panel1")
                {
                    ReArrangeImages();
                }

                _keyboardDragItems.Clear();
                ClearAllSelections();
                return; // إنهاء العملية هنا
            }

            if (!(targetControl is FlowLayoutPanel) && targetControl != panel1)
            {
                return;
            }

            bool wasPanel1Modified = false;

            foreach (var item in _keyboardDragItems)
            {
                if (item.SourceControl == null || item.SourceControl.IsDisposed) continue;

                if (targetControl is FlowLayoutPanel flowTarget)
                {
                    // ===== بداية التعديل =====
                    PictureBox targetPic = CreateNewPictureBoxForPanel(flowTarget);

                    // استخدم الدالة المساعدة لتعيين المحتوى الصحيح (صورة أو أيقونة وورد)
                    SetPictureBoxContent(targetPic, item.FilePath);

                    flowTarget.Controls.Add(targetPic);
                    // ===== نهاية التعديل =====
                }
                else if (targetControl == panel1)
                {
                    // ===== بداية التعديل =====
                    // دالة AddImageBackToPanel1 لا تعمل مع ملفات الوورد
                    // سنقوم بإعادة إنشاء العنصر هنا أيضاً
                    string ext = Path.GetExtension(item.FilePath).ToLower();
                    if (ext == ".doc" || ext == ".docx")
                    {
                        // إذا كان الملف وورد، استخدم نفس منطق DisplayFiles
                        int padding = 10;
                        int maxWidth = 120;
                        int maxHeight = 120;
                        int itemsPerRow = Math.Max(1, (panel1.ClientSize.Width - padding) / (maxWidth + padding));
                        int count = panel1.Controls.Count;
                        int x = padding + (count % itemsPerRow) * (maxWidth + padding);
                        int y = padding + (count / itemsPerRow) * (maxHeight + padding);

                        PictureBox pbWord = new PictureBox();
                        pbWord.Width = maxWidth;
                        pbWord.Height = maxHeight;
                        pbWord.SizeMode = PictureBoxSizeMode.Zoom;
                        pbWord.BorderStyle = BorderStyle.None;
                        pbWord.BackColor = Color.Transparent;
                        pbWord.Tag = item.FilePath;
                        pbWord.Image = Properties.Resources.wordicon; // تأكد أن اسم المورد صحيح

                        pbWord.Location = new Point(x, y);
                        pbWord.DoubleClick += OpenImage_DoubleClick;
                        pbWord.MouseDown += Pb_MouseDown_Panel1;
                        pbWord.MouseMove += Pb_MouseMove_Panel1;
                        pbWord.MouseUp += Pb_MouseUp_Panel1;
                        pbWord.Click += Pb_Click_Panel1;
                        pbWord.Paint += PictureBox_Paint_Selection;

                        panel1.Controls.Add(pbWord);
                    }
                    else
                    {
                        // إذا كان صورة، استخدم الدالة القديمة
                        AddImageBackToPanel1(item.FilePath);
                    }
                    // ===== نهاية التعديل =====
                }

                string sourceType = DetermineSourceType(item.SourceControl);
                if (sourceType == "panel1")
                {
                    wasPanel1Modified = true;
                }
                CleanupSourcePictureBox(item.SourceControl, sourceType);
            }

            // إعادة ترتيب العناصر في panel1 إذا تم تعديلها
            ReArrangeImages();

            _keyboardDragItems.Clear();
            ClearAllSelections();
        }
        private void SetPictureBoxContent(PictureBox targetPic, string filePath)
        {
            if (targetPic == null || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            string ext = Path.GetExtension(filePath).ToLower();

            targetPic.Tag = filePath;

            targetPic.Image?.Dispose();

            if (ext == ".docx" || ext == ".doc")
            {
                targetPic.Image = Properties.Resources.wordicon;
            }
            else 
            {
                try
                {
                    using (var imgTemp = Image.FromFile(filePath))
                    {
                        targetPic.Image = new Bitmap(imgTemp);
                    }
                }
                catch
                {
                    targetPic.Image = null;
                    targetPic.BackColor = Color.Red;
                }
            }
        }
    }
    //*********************************

}

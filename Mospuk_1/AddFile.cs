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
using System.Diagnostics;
using Org.BouncyCastle.Asn1.Cmp;

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

            panelDocx.Click += EmptySpace_Click;

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
                HandleKeyboardDragStart(); // This is the "Cut" operation
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
               
                if (_keyboardDragItems.Any())
                {
                    HandleKeyboardDrop();
                }
                else 
                {
                    HandlePaste();
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                // This is the new "Copy" operation.
                HandleCopy();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            // --- END: MODIFIED SECTION ---
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
            PictureBox pb = sender as PictureBox;
            if (pb != null)
            {
                // <<-- التعديل هنا: استدعاء الدالة الجديدة بدلاً من الكود القديم -->>
                OpenFileInIsolatedDirectory(pb);

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
            // 1. جمع كل مسارات الملفات المعروضة حالياً في كل اللوحات
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

            // إعدادات عرض الصور
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

                        using (var document = PdfDocument.Load(file))
                        {
                            int dpi = 600;
                            for (int i = 0; i < document.PageCount; i++)
                            {
                                string imagePath = Path.Combine(pdfImagesDir, $"Page_{i + 1}.png");

                                // **** التعديل الجوهري والنهائي هنا ****
                                // هذا الشرط الآن يعالج كلتا الحالتين بشكل صحيح.
                                // إذا كانت صورة هذه الصفحة المحددة موجودة بالفعل على الشاشة، نتجاهلها.
                                if (existingFilePaths.Contains(imagePath))
                                {
                                    continue; // تخطى هذه الصفحة فقط وانتقل للتالية
                                }
                                // **** نهاية التعديل ****

                                // إذا وصلنا إلى هنا، فهذا يعني أن هذه الصورة غير معروضة ويجب إضافتها.
                                // أولاً، تأكد من وجود ملف الصورة على القرص، وإذا لم يكن موجوداً، قم بإنشائه.
                                if (!File.Exists(imagePath))
                                {
                                    using (var image = document.Render(i, dpi, dpi, true))
                                    {
                                        image.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                                    }
                                }

                                // الآن، قم بإنشاء وعرض الـ PictureBox لهذه الصورة
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

                                // تحديث مكان الصورة التالية
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
                        MessageBox.Show($"Error processing PDF '{Path.GetFileName(file)}':\n{ex.Message}",
                                        "PDF Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else // التعامل مع كل الملفات الأخرى (JPG, PNG, DOCX, etc.)
                {
                    // تجاهل الملف إذا كان معروضًا بالفعل
                    if (existingFilePaths.Contains(file))
                    {
                        continue;
                    }

                    // ... بقية الكود يبقى كما هو تماماً ...
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
            lblStatus.Text = "Saving, please wait...";
            lblStatus.Visible = true;
            this.Enabled = false;
            Application.DoEvents();

            try
            {
                // ... (كل كود التحقق من المدخلات حتى الوصول إلى جملة if(success) يبقى كما هو) ...
                string companyClient = Company_Client.Text.Trim();
                DateTime receptionDate = Reception_Date.Value.Date;
                if (!Time.MaskCompleted)
                {
                    MessageBox.Show("Please enter a valid time (HH:mm).", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Time.Focus();
                    return;
                }

                if (!DateTime.TryParseExact(Time.Text, "HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                {
                    MessageBox.Show("The time entered is not valid. Please use 24-hour format (e.g., 14:30).", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Time.Focus();
                    return;
                }

                string receptionTime = Time.Text;
                if (!flowLayoutPanel1.Controls.OfType<PictureBox>().Any())
                {
                    MessageBox.Show("Please upload at least one translation image.", "No Images", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.ActiveControl = null;
                    return;
                }

                if (Company_Client.SelectedItem == null)
                {
                    MessageBox.Show("Please select a client or company.", "Input Error");
                    Company_Client.Focus();
                    return;
                }

                if (!(comboDocumentType.SelectedItem is KeyValuePair<int, string> selectedDocTypePair))
                {
                    MessageBox.Show("Please select a document type.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    comboDocumentType.Focus();
                    return;
                }

                if (!(comboTranslation.SelectedItem is KeyValuePair<int, string> selectedTranslationPair))
                {
                    MessageBox.Show("Please select a translation type.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    comboTranslation.Focus();
                    return;
                }
                int deliveryDays = ((KeyValuePair<string, int>)Delivery_Date.SelectedItem).Value;
                DateTime deliveryDate = receptionDate.AddDays(deliveryDays);
                string orderQuery = "SELECT IFNULL(MAX(project_order), 0) FROM projects WHERE reception_date = @date";
                object result = db.ExecuteScalar(orderQuery, new List<SQLiteParameter> { new SQLiteParameter("@date", receptionDate.ToString("yyyy-MM-dd")) });
                int lastOrder = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
                int newOrder = lastOrder + 1;
                string documentType = selectedDocTypePair.Value;
                string translationType = selectedTranslationPair.Value;
                string hoursSpent = "24";
                string deliveryDateStr = deliveryDate.ToString("yyyyMMdd");
                string receptionDateStr = receptionDate.ToString("yyMMdd");
                string projectOrderStr = newOrder.ToString("D2");
                string receptionTimeStr = receptionTime.Replace(":", "");
                if (deliveryDays == 1)
                {
                    if (!string.IsNullOrEmpty(deliveryDateStr))
                    {
                        deliveryDateStr = "0" + deliveryDateStr.Substring(1);
                    }
                }
                string folderName = $"{deliveryDateStr}{hoursSpent}_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}";
                if (!string.IsNullOrWhiteSpace(txtnotes.Text))
                {
                    string rawNote = txtnotes.Text.Trim();
                    string sanitizedNote = new string(rawNote.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '+' || c == '-').ToArray()).Replace(" ", "_");
                    if (sanitizedNote.Length > 30)
                        sanitizedNote = sanitizedNote.Substring(0, 30);
                    folderName += $"-------------{sanitizedNote}-----------";
                }
                string insertQuery = @"INSERT INTO projects (company_client, reception_date, reception_time, delivery_days, delivery_date, hours_spent, project_order, folder_name, note, document_type, translation_type, registration_date, last_update_date) VALUES (@company_client, @reception_date, @reception_time, @delivery_days, @delivery_date, @hours_spent, @project_order, @folder_name, @note, @document_type, @translation_type, CURRENT_DATE, CURRENT_TIMESTAMP)";
                List<SQLiteParameter> parameters = new List<SQLiteParameter>
        {
            new SQLiteParameter("@company_client", companyClient), new SQLiteParameter("@reception_date", receptionDate.ToString("yyyy-MM-dd")), new SQLiteParameter("@reception_time", receptionTime), new SQLiteParameter("@delivery_days", deliveryDays), new SQLiteParameter("@delivery_date", deliveryDate.ToString("yyyy-MM-dd")), new SQLiteParameter("@hours_spent", 24), new SQLiteParameter("@project_order", newOrder), new SQLiteParameter("@folder_name", folderName), new SQLiteParameter("@note", string.IsNullOrWhiteSpace(txtnotes.Text) ? DBNull.Value : (object)txtnotes.Text), new SQLiteParameter("@document_type", documentType), new SQLiteParameter("@translation_type", translationType)
        };
                bool success = db.ExecuteNonQuery(insertQuery, parameters);

                if (success)
                {
                    string getLastIdQuery = "SELECT last_insert_rowid()";
                    object lastIdResult = db.ExecuteScalar(getLastIdQuery, null);
                    int projectId = Convert.ToInt32(lastIdResult);

                    // استدعاء دالة الحفظ واستقبال النتيجة وقائمة المسارات
                    var (allImagesSaved, savedSourcePaths) = SaveProjectImages(projectId, folderName, deliveryDateStr, receptionDateStr, projectOrderStr, receptionTimeStr, companyClient, translationType, documentType);

                    if (allImagesSaved)
                    {
                        // 1. تنظيف الواجهة الرسومية
                        CleanUpSavedProject();

                        // 2. حذف الملفات المصدر من المجلد المؤقت
                        foreach (string path in savedSourcePaths)
                        {
                            try
                            {
                                if (File.Exists(path))
                                {
                                    File.Delete(path);
                                }
                            }
                            catch (Exception ex)
                            {
                                // اختياري: إظهار رسالة إذا فشل الحذف
                                Console.WriteLine($"Could not delete temporary file {path}: {ex.Message}");
                            }
                        }

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
            finally
            {
                lblStatus.Visible = false;
                this.Enabled = true;
            }
        }
        private (bool success, List<string> savedSourcePaths) SaveProjectImages(int projectId, string folderName, string deliveryDateStr, string receptionDateStr, string projectOrderStr, string receptionTimeStr, string companyClient, string translationType, string documentType)
        {
            bool allSaved = true;
            int imageCounter = 1;
            var savedSourcePaths = new List<string>(); // قائمة لتخزين مسارات الملفات التي تم حفظها

            string projectFolder = db.GetSavedPathById("save");
            if (string.IsNullOrEmpty(projectFolder))
            {
                MessageBox.Show("Please set a save directory first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (false, savedSourcePaths);
            }

            if (!Directory.Exists(projectFolder))
            {
                Directory.CreateDirectory(projectFolder);
            }

            // 1. حفظ عناصر flowLayoutPanel1
            foreach (Control control in flowLayoutPanel1.Controls)
            {
                if (control is PictureBox pb && pb.Tag != null)
                {
                    try
                    {
                        string originalPath = pb.Tag.ToString();
                        string extension = Path.GetExtension(originalPath);
                        string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}";

                        if (imageCounter == 1)
                        {
                            if (!string.IsNullOrWhiteSpace(txtnotes.Text))
                            {
                                string rawNote = txtnotes.Text.Trim();
                                string sanitizedNote = new string(rawNote.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '+' || c == '-').ToArray()).Replace(" ", "_");
                                if (sanitizedNote.Length > 30)
                                    sanitizedNote = sanitizedNote.Substring(0, 30);
                                imageName += $"------------------------{sanitizedNote}----------------------";
                            }
                            else
                            {
                                imageName += "--------------------------------------------------------------";
                            }
                        }

                        string fullItemName = imageName + extension;
                        string destinationPath = Path.Combine(projectFolder, fullItemName);

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
                            MessageBox.Show($"Could not save item (no image found): {originalPath}");
                            continue;
                        }

                        File.SetCreationTime(destinationPath, DateTime.Now);
                        File.SetLastWriteTime(destinationPath, DateTime.Now);

                        string insertImageQuery = @"INSERT INTO items (project_id, image_name, image_path, registration_date, last_update_date) VALUES (@project_id, @image_name, @image_path, CURRENT_DATE, CURRENT_TIMESTAMP)";
                        List<SQLiteParameter> imageParameters = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@project_id", projectId),
                    new SQLiteParameter("@image_name", fullItemName),
                    new SQLiteParameter("@image_path", destinationPath)
                };

                        if (db.ExecuteNonQuery(insertImageQuery, imageParameters))
                        {
                            imageCounter++;
                            savedSourcePaths.Add(originalPath); // إضافة المسار المصدر للقائمة عند النجاح
                        }
                        else
                        {
                            allSaved = false;
                            MessageBox.Show($"Failed to save database record for: {fullItemName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        allSaved = false;
                        MessageBox.Show($"Error saving item {imageCounter}: {ex.Message}");
                    }
                }
            }

            // 2. حفظ صورة imageApostille
            PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            if (imageApostille != null && imageApostille.Image != null && imageApostille.Tag != null)
            {
                try
                {
                    string originalPath = imageApostille.Tag.ToString();
                    string extension = Path.GetExtension(originalPath);
                    string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Apostille";
                    string fullImageName = imageName + extension;
                    string imagePath = Path.Combine(projectFolder, fullImageName);
                    imageApostille.Image.Save(imagePath);
                    File.SetCreationTime(imagePath, DateTime.Now);
                    File.SetLastWriteTime(imagePath, DateTime.Now);

                    string insertImageQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";
                    List<SQLiteParameter> imageParameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@project_id", projectId),
                new SQLiteParameter("@image_name", fullImageName),
                new SQLiteParameter("@image_path", imagePath),
                new SQLiteParameter("@attachment_type", "Apostille")
            };

                    if (db.ExecuteNonQuery(insertImageQuery, imageParameters))
                    {
                        imageCounter++;
                        savedSourcePaths.Add(originalPath); // إضافة المسار المصدر للقائمة
                    }
                    else
                    {
                        allSaved = false;
                        MessageBox.Show($"Failed to save Apostille image: {fullImageName}");
                    }
                }
                catch (Exception ex)
                {
                    allSaved = false;
                    MessageBox.Show($"Error saving Apostille image: {ex.Message}");
                }
            }

            // 3. حفظ عناصر flowLayoutPanel2
            string attachmentType = "A";
            foreach (Control control in flowLayoutPanel2.Controls)
            {
                if (control is PictureBox pb && pb.Tag != null)
                {
                    try
                    {
                        string originalPath = pb.Tag.ToString();
                        string extension = Path.GetExtension(originalPath);
                        string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_{attachmentType}";
                        string fullItemName = imageName + extension;
                        string destinationPath = Path.Combine(projectFolder, fullItemName);

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

                        string insertImageQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";
                        List<SQLiteParameter> imageParameters = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@project_id", projectId),
                    new SQLiteParameter("@image_name", fullItemName),
                    new SQLiteParameter("@image_path", destinationPath),
                    new SQLiteParameter("@attachment_type", attachmentType)
                };

                        if (db.ExecuteNonQuery(insertImageQuery, imageParameters))
                        {
                            imageCounter++;
                            savedSourcePaths.Add(originalPath); // إضافة المسار المصدر للقائمة
                        }
                        else
                        {
                            allSaved = false;
                            MessageBox.Show($"Failed to save attachment record: {fullItemName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        allSaved = false;
                        MessageBox.Show($"Error saving attachment {imageCounter}: {ex.Message}");
                    }
                }
            }

            // 4. حفظ ملفات Word المرفوعة (OCR)
            int ocrFileNumber = imageCounter;
            bool hasOcrFiles = false; // متغير لتتبع وجود ملفات OCR

            foreach (Control control in panelDocx.Controls)
            {
                if (control is Label lbl && lbl.Tag != null)
                {
                    hasOcrFiles = true; // تم العثور على ملف OCR
                    try
                    {
                        string originalPath = lbl.Tag.ToString();
                        string extension = Path.GetExtension(originalPath);
                        string wordFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{ocrFileNumber}_OCR";
                        string fullWordFileName = wordFileName + extension;
                        string wordPath = Path.Combine(projectFolder, fullWordFileName);

                        if (File.Exists(originalPath))
                        {
                            File.Copy(originalPath, wordPath, true);
                            File.SetCreationTime(wordPath, DateTime.Now);
                            File.SetLastWriteTime(wordPath, DateTime.Now);
                            string insertWordQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";
                            List<SQLiteParameter> wordParameters = new List<SQLiteParameter>
                    {
                        new SQLiteParameter("@project_id", projectId),
                        new SQLiteParameter("@image_name", fullWordFileName),
                        new SQLiteParameter("@image_path", wordPath),
                        new SQLiteParameter("@attachment_type", "WORD")
                    };

                            if (db.ExecuteNonQuery(insertWordQuery, wordParameters))
                            {
                                ocrFileNumber++;
                                savedSourcePaths.Add(originalPath); // إضافة المسار المصدر للقائمة
                            }
                            else
                            {
                                allSaved = false;
                                MessageBox.Show($"Failed to save Word file record: {fullWordFileName}");
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

            // الأجزاء التالية تنشئ ملفات فارغة، لذا ليس لديها "مسار مصدر" ليتم حذفه
            if (!hasOcrFiles)
            {
                ocrFileNumber++;
            }
            imageCounter = ocrFileNumber;

            // 5. إنشاء ملف Word جديد بإسم Google Drive
            try
            {
                string googleDriverFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Google Drive.docx";
                string googleDriverPath = Path.Combine(projectFolder, googleDriverFileName);
                using (var fs = File.Create(googleDriverPath)) { }

                File.SetCreationTime(googleDriverPath, DateTime.Now);
                File.SetLastWriteTime(googleDriverPath, DateTime.Now);

                string insertGoogleDriverQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";
                List<SQLiteParameter> googleDriverParams = new List<SQLiteParameter>
        {
            new SQLiteParameter("@project_id", projectId),
            new SQLiteParameter("@image_name", googleDriverFileName),
            new SQLiteParameter("@image_path", googleDriverPath),
            new SQLiteParameter("@attachment_type", "Google Driver")
        };

                if (db.ExecuteNonQuery(insertGoogleDriverQuery, googleDriverParams))
                {
                    imageCounter++;
                }
                else
                {
                    allSaved = false;
                    MessageBox.Show($"❌ Failed to save Google Driver file: {googleDriverFileName}");
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

                string insertTraduccionQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";
                List<SQLiteParameter> traduccionParams = new List<SQLiteParameter>
        {
            new SQLiteParameter("@project_id", projectId),
            new SQLiteParameter("@image_name", traduccionFileName),
            new SQLiteParameter("@image_path", traduccionPath),
            new SQLiteParameter("@attachment_type", "Traducción Preliminar")
        };

                if (db.ExecuteNonQuery(insertTraduccionQuery, traduccionParams))
                {
                    imageCounter++;
                }
                else
                {
                    allSaved = false;
                    MessageBox.Show($"❌ Failed to save Traducción Preliminar file: {traduccionFileName}");
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

                string insertInformeQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";
                List<SQLiteParameter> informeParams = new List<SQLiteParameter>
        {
            new SQLiteParameter("@project_id", projectId),
            new SQLiteParameter("@image_name", informeFileName),
            new SQLiteParameter("@image_path", informePath),
            new SQLiteParameter("@attachment_type", "Informe revisión")
        };

                if (db.ExecuteNonQuery(insertInformeQuery, informeParams))
                {
                    imageCounter++;
                }
                else
                {
                    allSaved = false;
                    MessageBox.Show($"❌ Failed to save Informe revisión file: {informeFileName}");
                }
            }
            catch (Exception ex)
            {
                allSaved = false;
                MessageBox.Show($"❌ Error creating Informe revisión file {imageCounter}: {ex.Message}");
            }

            // 8. إنشاء ملف Word جديد بإسم Traducción revisada
            try
            {
                string traduccionRevisadaFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Traducción revisada.docx";
                string traduccionRevisadaPath = Path.Combine(projectFolder, traduccionRevisadaFileName);
                using (var fs = File.Create(traduccionRevisadaPath)) { }

                File.SetCreationTime(traduccionRevisadaPath, DateTime.Now);
                File.SetLastWriteTime(traduccionRevisadaPath, DateTime.Now);

                string insertTraduccionRevisadaQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";
                List<SQLiteParameter> traduccionRevisadaParams = new List<SQLiteParameter>
        {
            new SQLiteParameter("@project_id", projectId),
            new SQLiteParameter("@image_name", traduccionRevisadaFileName),
            new SQLiteParameter("@image_path", traduccionRevisadaPath),
            new SQLiteParameter("@attachment_type", "Traducción revisada")
        };

                if (db.ExecuteNonQuery(insertTraduccionRevisadaQuery, traduccionRevisadaParams))
                {
                    imageCounter++;
                }
                else
                {
                    allSaved = false;
                    MessageBox.Show($"❌ Failed to save Traducción revisada file: {traduccionRevisadaFileName}");
                }
            }
            catch (Exception ex)
            {
                allSaved = false;
                MessageBox.Show($"❌ Error creating Traducción revisada file {imageCounter}: {ex.Message}");
            }

            return (allSaved, savedSourcePaths);
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
        //***********
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
            Time.KeyDown += Time_KeyDown;
            lblStatus.Visible = false;

        }

        private void btnAddWord_Click(object sender, EventArgs e)
        {
            // --- بداية التعديل ---
            // 1. التحقق أولاً إذا كان هناك ملف Word مضاف بالفعل
            if (panelDocx.Controls.Count >= 1)
            {
                MessageBox.Show("You can only add one OCR Word file.\nPlease remove the existing file if you wish to add a different one.",
                                "Limit Reached",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return; // إيقاف العملية
            }
            // --- نهاية التعديل ---

            string projectFolder = db.GetSavedPathById("archive");
            if (string.IsNullOrEmpty(projectFolder))
            {
                MessageBox.Show("Please set a save directory first.", "Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = projectFolder;
            ofd.Filter = "Word Files|*.doc;*.docx|All Files|*.*";
            ofd.Title = "Select Word File";

            // --- التعديل الثاني: منع تحديد أكثر من ملف واحد ---
            ofd.Multiselect = false; // تغيير القيمة إلى false

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                // بما أننا نسمح بملف واحد فقط، لم نعد بحاجة إلى حلقة تكرارية
                string filePath = ofd.FileName; // استخدام .FileName بدلاً من .FileNames
                string fileName = Path.GetFileName(filePath);

                // التحقق من التكرار (للاحتياط، رغم أنه لا يجب أن يحدث الآن)
                bool fileExists = panelDocx.Controls.OfType<Label>().Any(lbl => lbl.Tag?.ToString() == filePath);

                if (!fileExists)
                {
                    Label lbl = new Label();
                    lbl.Text = fileName;
                    lbl.Tag = filePath; // تخزين المسار الكامل
                    lbl.AutoSize = true;
                    lbl.Padding = new Padding(5);
                    lbl.Margin = new Padding(5);
                    lbl.BackColor = Color.SeaGreen;
                    lbl.ForeColor = Color.White;
                    lbl.Cursor = Cursors.Hand;
                    lbl.BorderStyle = BorderStyle.FixedSingle;
                    ToolTip toolTip = new ToolTip();
                    toolTip.SetToolTip(lbl, filePath);
                    lbl.DoubleClick += (s, ev) =>
                    {
                        try
                        {
                            var psi = new System.Diagnostics.ProcessStartInfo()
                            {
                                FileName = filePath,
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

            // <<-- التعديل هنا: افتح أول صورة محددة فقط باستخدام المنطق الجديد -->>
            PictureBox firstSelected = allSelectedPictures.FirstOrDefault();
            if (firstSelected != null)
            {
                OpenFileInIsolatedDirectory(firstSelected);
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
            if (sender is Control clickedControl)
            {
                clickedControl.Focus();
            }
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

       
                string tempFolder = Path.Combine(Application.StartupPath, "ExtractedFiles");
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
                Directory.CreateDirectory(tempFolder);
            }
            catch (Exception ex)
            {
                // من الجيد عرض رسالة في حال فشل التنظيف لأي سبب
                MessageBox.Show($"حدث خطأ أثناء تنظيف مساحة العمل:\n{ex.Message}", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            if (panel1.Controls.Count == 0)
            {
                Company_Client.SelectedIndex = -1;
            }

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
            var pictureBoxes = panel.Controls.OfType<PictureBox>().Where(p => p.Visible).ToList();

            if (!pictureBoxes.Any())
            {
                return 0;
            }

            for (int i = 0; i < pictureBoxes.Count; i++)
            {
                var pb = pictureBoxes[i];

                if (pb == draggedPictureBox)
                    continue;

                Rectangle bounds = pb.Bounds;

     
                if (clientPoint.Y >= bounds.Top && clientPoint.Y <= bounds.Bottom)
                {
                    
                    if (clientPoint.X < bounds.Left + (bounds.Width / 2))
                    {
                        return panel.Controls.GetChildIndex(pb);
                    }
                }
            }

          

            PictureBox lastControlInRow = null;
            foreach (var pb in pictureBoxes.OfType<PictureBox>().Reverse())
            {
                if (pb == draggedPictureBox) continue;

                Rectangle bounds = pb.Bounds;
                if (clientPoint.Y >= bounds.Top && clientPoint.Y <= bounds.Bottom)
                {
                    lastControlInRow = pb;
                    break;
                }
            }

            if (lastControlInRow != null)
            {
                // إذا وجدنا عنصراً في الصف، والمؤشر على يمينه، قم بالإدراج بعده
                return panel.Controls.GetChildIndex(lastControlInRow) + 1;
            }

            // إذا لم نجد أي موضع مناسب (مثلاً، المؤشر في منطقة فارغة تماماً)،
            // فقم بالإدراج في نهاية اللوحة
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

                // **الحالة 1: إعادة ترتيب داخل نفس اللوحة**
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

                    if (targetIndex != -1 && picturesToMove.Any())
                    {
                        var allControls = panel.Controls.OfType<PictureBox>().ToList();
                        var movedControlsIndices = picturesToMove.Select(p => allControls.IndexOf(p)).OrderBy(i => i).ToList();

                        int adjustedTargetIndex = targetIndex;
                        foreach (int index in movedControlsIndices)
                        {
                            if (index < targetIndex)
                            {
                                adjustedTargetIndex--;
                            }
                        }

                        adjustedTargetIndex = Math.Max(0, Math.Min(adjustedTargetIndex, allControls.Count - picturesToMove.Count));

                        foreach (var pic in picturesToMove.OrderByDescending(p => allControls.IndexOf(p)))
                        {
                            panel.Controls.Remove(pic);
                        }

                        for (int i = 0; i < picturesToMove.Count; i++)
                        {
                            panel.Controls.Add(picturesToMove[i]);
                            int finalIndex = Math.Min(adjustedTargetIndex + i, panel.Controls.Count - 1);
                            panel.Controls.SetChildIndex(picturesToMove[i], finalIndex);
                        }
                    }
                }
                // **الحالة 2: السحب المتعدد من panel1 مباشرة (هنا التعديل المهم)**
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

                        // *** التعديل الجوهري: إزالة العناصر من panel1 بعد نقلها ***
                        foreach (string filePath in filePaths)
                        {
                            // استدعاء الدالة التي تزيل الصورة من panel1 وتعيد ترتيبه
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
                // **الحالة 4: السحب الفردي بين اللوحات (هنا تعديل آخر مهم)**
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

                        // *** التعديل الجوهري: إزالة العنصر من مصدره الأصلي ***
                        string sourceType = DetermineSourceType(sourceCtrl);
                        // الدالة CleanupSourcePictureBox تقوم بالإزالة والتنظيف اللازم
                        CleanupSourcePictureBox(sourceCtrl, sourceType);
                    }
                }
                // **الحالة 5: إفلات من خارج البرنامج**
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

                            // التأكد من أن الصورة أو أيقونة الوورد قد تم تحميلها بنجاح قبل الإضافة
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
                case "panel1": // تعديل هنا
                    if (pb.Parent != null)
                    {
                        selectedPictureBoxes.Remove(pb); // إزالة من قائمة التحديد
                        pb.Parent.Controls.Remove(pb);
                        ReArrangeImages(); // *** إضافة مهمة: إعادة ترتيب panel1 بعد الحذف ***
                    }
                    pb.Image?.Dispose();
                    pb.Dispose();
                    break;
                default: // الحالة الافتراضية للتعامل مع أي مصدر غير معروف
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

            // *** NEW: Check if a valid destination is selected ***
            bool isValidTarget = (targetControl == panel1) ||
                                 (targetControl == flowLayoutPanel1) ||
                                 (targetControl == flowLayoutPanel2) ||
                                 (targetControl == imageApostille);

            if (!isValidTarget)
            {
                MessageBox.Show("Please click on a panel to select a destination before pasting.",
                                "No Destination Selected",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return; // Stop the paste operation, but keep items in the "cut" buffer
            }
            // *** END NEW SECTION ***


            if (targetControl == imageApostille)
            {
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
                return;
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
                    PictureBox targetPic = CreateNewPictureBoxForPanel(flowTarget);
                    SetPictureBoxContent(targetPic, item.FilePath);
                    flowTarget.Controls.Add(targetPic);
                }
                else if (targetControl == panel1)
                {
                    string ext = Path.GetExtension(item.FilePath).ToLower();
                    if (ext == ".doc" || ext == ".docx")
                    {
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
                        pbWord.Image = Properties.Resources.wordicon;

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
                        AddImageBackToPanel1(item.FilePath);
                    }
                }

                string sourceType = DetermineSourceType(item.SourceControl);
                if (sourceType == "panel1")
                {
                    wasPanel1Modified = true;
                }
                CleanupSourcePictureBox(item.SourceControl, sourceType);
            }

            if (wasPanel1Modified)
            {
                ReArrangeImages();
            }

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

        private void AddFile_Resize(object sender, EventArgs e)
        {
            ReArrangeImages();
        }
        private void Time_KeyDown(object sender, KeyEventArgs e)
        {
            MaskedTextBox mtb = sender as MaskedTextBox;
            if (mtb == null) return;

            // --- بداية التعديل ---

            // 1. التعامل مع القفز الذكي فوق النقطتين أولاً
            // إذا ضغطت السهم الأيمن والمؤشر قبل النقطتين، اقفز إلى تحديد الدقائق
            if (e.KeyCode == Keys.Right && mtb.SelectionStart == 2)
            {
                // انتظر قليلاً ثم حدد حقل الدقائق
                this.BeginInvoke((MethodInvoker)delegate {
                    mtb.Select(3, 2);
                });
                e.Handled = true; // منع المعالجة الافتراضية
                return; // تم التعامل مع هذا المفتاح، لا تكمل
            }

            // إذا ضغطت السهم الأيسر والمؤشر في بداية الدقائق، اقفز إلى تحديد الساعات
            if (e.KeyCode == Keys.Left && mtb.SelectionStart == 3)
            {
                // انتظر قليلاً ثم حدد حقل الساعات
                this.BeginInvoke((MethodInvoker)delegate {
                    mtb.Select(0, 2);
                });
                e.Handled = true; // منع المعالجة الافتراضية
                return; // تم التعامل مع هذا المفتاح، لا تكمل
            }

            // 2. إذا لم تكن حالة خاصة، اسمح لمفاتيح التنقل والحذف بالعمل بشكل طبيعي
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down ||
                e.KeyCode == Keys.Home || e.KeyCode == Keys.End || e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                return; // اسمح للسلوك الافتراضي لهذه المفاتيح
            }

            // --- نهاية التعديل ---

            // الكود الأصلي الخاص بك لإضافة "0" تلقائياً (لا يزال مفيداً)
            // التحقق من الساعات
            if (mtb.SelectionStart >= 0 && mtb.SelectionStart <= 1)
            {
                int numberPressed = -1;
                if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
                    numberPressed = e.KeyCode - Keys.D0;
                else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
                    numberPressed = e.KeyCode - Keys.NumPad0;

                // إذا كان المستخدم يكتب الرقم الأول وكان 3 أو أكبر، أضف 0 قبله
                if (mtb.SelectionStart == 0 && numberPressed >= 3 && numberPressed <= 9)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        mtb.Text = "0" + numberPressed.ToString();
                        mtb.Select(3, 0); // نقل المؤشر إلى موضع الدقائق
                    });
                }
            }
            // التحقق من الدقائق
            else if (mtb.SelectionStart >= 3 && mtb.SelectionStart <= 4)
            {
                int numberPressed = -1;
                if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
                    numberPressed = e.KeyCode - Keys.D0;
                else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
                    numberPressed = e.KeyCode - Keys.NumPad0;

                if (mtb.SelectionStart == 3 && numberPressed >= 6 && numberPressed <= 9)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        string hours = mtb.Text.Substring(0, 3); // الساعات مع النقطتين
                        mtb.Text = hours + "0" + numberPressed.ToString();
                        mtb.Select(5, 0); // نقل المؤشر إلى النهاية
                    });
                }
            }
        }
        private void OpenFileInIsolatedDirectory(PictureBox clickedPictureBox)
        {
            if (clickedPictureBox?.Tag == null) return;

            string clickedFilePath = clickedPictureBox.Tag.ToString();

            // تجاهل ملفات الوورد، افتحها مباشرة
            string ext = Path.GetExtension(clickedFilePath).ToLower();
            if (ext == ".doc" || ext == ".docx")
            {
                try
                {
                    var psi = new ProcessStartInfo()
                    {
                        FileName = clickedFilePath,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ في فتح ملف الوورد: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            List<string> sourceFilePaths = new List<string>();
            int clickedFileIndex = 0;

            // 1. تحديد اللوحة المصدر وجمع كل مسارات الملفات منها مع الترتيب الصحيح
            Control parentControl = clickedPictureBox.Parent;
            if (clickedPictureBox.Name == "imageApostille")
            {
                parentControl = clickedPictureBox;
            }

            if (parentControl == panel1)
            {
                var orderedPictureBoxes = panel1.Controls.OfType<PictureBox>()
                    .Where(pb => pb.Tag != null && !pb.Tag.ToString().EndsWith(".docx") && !pb.Tag.ToString().EndsWith(".doc"))
                    .OrderBy(p => p.Location.Y).ThenBy(p => p.Location.X) // ترتيب حسب الموقع المرئي
                    .ToList();

                sourceFilePaths = orderedPictureBoxes.Select(pb => pb.Tag.ToString()).ToList();

                // العثور على فهرس الملف المنقور عليه
                clickedFileIndex = orderedPictureBoxes.FindIndex(pb => pb.Tag.ToString() == clickedFilePath);
            }
            else if (parentControl == flowLayoutPanel1)
            {
                var orderedPictureBoxes = flowLayoutPanel1.Controls.OfType<PictureBox>()
                    .Where(pb => pb.Tag != null && !pb.Tag.ToString().EndsWith(".docx") && !pb.Tag.ToString().EndsWith(".doc"))
                    .ToList();

                sourceFilePaths = orderedPictureBoxes.Select(pb => pb.Tag.ToString()).ToList();
                clickedFileIndex = orderedPictureBoxes.FindIndex(pb => pb.Tag.ToString() == clickedFilePath);
            }
            else if (parentControl == flowLayoutPanel2)
            {
                var orderedPictureBoxes = flowLayoutPanel2.Controls.OfType<PictureBox>()
                    .Where(pb => pb.Tag != null && !pb.Tag.ToString().EndsWith(".docx") && !pb.Tag.ToString().EndsWith(".doc"))
                    .ToList();

                sourceFilePaths = orderedPictureBoxes.Select(pb => pb.Tag.ToString()).ToList();
                clickedFileIndex = orderedPictureBoxes.FindIndex(pb => pb.Tag.ToString() == clickedFilePath);
            }
            else if (parentControl == clickedPictureBox && clickedPictureBox.Name == "imageApostille")
            {
                sourceFilePaths.Add(clickedFilePath);
                clickedFileIndex = 0;
            }

            if (!sourceFilePaths.Any()) return;

            // 2. إنشاء مجلد مؤقت وفريد
            string tempDir = Path.Combine(Path.GetTempPath(), "MospukViewer_" + Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(tempDir);

                // 3. نسخ الملفات مع إعادة تسمية لضمان الترتيب الصحيح
                for (int i = 0; i < sourceFilePaths.Count; i++)
                {
                    string sourcePath = sourceFilePaths[i];
                    if (File.Exists(sourcePath))
                    {
                        // إعادة تسمية الملفات بأرقام تسلسلية لضمان الترتيب الصحيح
                        string extension = Path.GetExtension(sourcePath);
                        string newFileName = $"{i:D3}_{Path.GetFileNameWithoutExtension(sourcePath)}{extension}";
                        string destPath = Path.Combine(tempDir, newFileName);
                        File.Copy(sourcePath, destPath);

                        // تحديث مسار الملف المطلوب فتحه
                        if (i == clickedFileIndex)
                        {
                            clickedFilePath = destPath;
                        }
                    }
                }

                // 4. فتح الملف الذي تم النقر عليه من داخل المجلد المؤقت
                if (File.Exists(clickedFilePath))
                {
                    var process = new Process();
                    process.StartInfo = new ProcessStartInfo()
                    {
                        FileName = clickedFilePath,
                        UseShellExecute = true
                    };

                    // 5. إعداد الحذف التلقائي للمجلد المؤقت بعد إغلاق عارض الصور
                    process.EnableRaisingEvents = true;
                    process.Exited += (s, args) =>
                    {
                        try
                        {
                            if (Directory.Exists(tempDir))
                            {
                                Directory.Delete(tempDir, true);
                            }
                        }
                        catch (Exception ex)
                        {
                            // تجاهل الأخطاء المحتملة في الحذف
                            Console.WriteLine($"Could not delete temp directory {tempDir}: {ex.Message}");
                        }
                    };

                    process.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تهيئة عارض الصور: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // تأكد من حذف المجلد إذا فشلت العملية
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch
                {
                    // تجاهل أخطاء الحذف
                }
            }
        }
        private void HandleCopy()
        {
            // 1. Gather all selected picture boxes from every list.
            var allSelected = new List<PictureBox>();
            allSelected.AddRange(selectedPictureBoxes); // For panel1 and imageApostille
            allSelected.AddRange(selectedPictureBoxesFlow1);
            allSelected.AddRange(selectedPictureBoxesFlow2);

            // Use Distinct to ensure we don't have duplicates
            var uniqueSelected = allSelected.Distinct().ToList();

            if (!uniqueSelected.Any()) return; // Nothing selected to copy

            // 2. Get the file paths from their Tag property.
            var filePaths = new List<string>();
            foreach (var pb in uniqueSelected)
            {
                if (pb?.Tag != null)
                {
                    string path = pb.Tag.ToString();
                    if (File.Exists(path))
                    {
                        filePaths.Add(path);
                    }
                }
            }

            if (!filePaths.Any()) return; // No valid files to copy

            // 3. Add the file paths to the clipboard as a file drop list.
            var fileDropList = new System.Collections.Specialized.StringCollection();
            fileDropList.AddRange(filePaths.ToArray());

            try
            {
                Clipboard.SetFileDropList(fileDropList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not copy files to clipboard: {ex.Message}", "Copy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void HandlePaste()
        {
            // 1. Check if the clipboard contains a list of files.
            if (!Clipboard.ContainsFileDropList()) return;

            var filePathsFromClipboard = Clipboard.GetFileDropList();
            if (filePathsFromClipboard == null || filePathsFromClipboard.Count == 0) return;

            // 2. Determine the target control for the paste operation.
            Control targetControl = this.ActiveControl;
            PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;

            // *** NEW: Check if a valid destination is selected ***
            bool isValidTarget = (targetControl == panel1) ||
                                 (targetControl == flowLayoutPanel1) ||
                                 (targetControl == flowLayoutPanel2) ||
                                 (targetControl == imageApostille);

            if (!isValidTarget)
            {
                MessageBox.Show("Please click on a panel to select a destination before pasting.",
                                "No Destination Selected",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return; // Stop the paste operation
            }
            // *** END NEW SECTION ***

            // Case A: Pasting into the Apostille slot
            if (targetControl == imageApostille)
            {
                if (imageApostille.Image != null)
                {
                    MessageBox.Show("Apostille slot is already occupied. Cannot paste here.", "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (filePathsFromClipboard.Count > 1)
                {
                    MessageBox.Show("You can only paste a single image into the Apostille slot.", "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string originalFilePath = filePathsFromClipboard[0];
                string ext = Path.GetExtension(originalFilePath).ToLower();
                if (ext == ".doc" || ext == ".docx")
                {
                    MessageBox.Show("Cannot paste a Word document into the Apostille slot.", "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string newFilePath = CreateFileCopyInWorkspace(originalFilePath);
                if (newFilePath != null)
                {
                    SetPictureBoxContent(imageApostille, newFilePath);
                }
            }
            // Case B: Pasting into one of the FlowLayoutPanels
            else if (targetControl is FlowLayoutPanel flowTarget)
            {
                foreach (string path in filePathsFromClipboard)
                {
                    string newFilePath = CreateFileCopyInWorkspace(path);
                    if (newFilePath != null)
                    {
                        PictureBox newPic = CreateNewPictureBoxForPanel(flowTarget);
                        SetPictureBoxContent(newPic, newFilePath);

                        if (newPic.Image != null || Path.GetExtension(newFilePath).ToLower().Contains("doc"))
                        {
                            flowTarget.Controls.Add(newPic);
                        }
                        else
                        {
                            newPic.Dispose();
                        }
                    }
                }
            }
            // Case C: Pasting into the main panel (panel1)
            else if (targetControl == panel1)
            {
                foreach (string path in filePathsFromClipboard)
                {
                    string newFilePath = CreateFileCopyInWorkspace(path);
                    if (newFilePath != null)
                    {
                        AddImageBackToPanel1(newFilePath);
                    }
                }
                ReArrangeImages();
            }

            ClearAllSelections();
        }
        private string CreateFileCopyInWorkspace(string sourceFilePath)
        {
            // First, check if the source file still exists before trying to copy it.
            if (!File.Exists(sourceFilePath))
            {
                MessageBox.Show($"The source file could not be found and cannot be pasted:\n{Path.GetFileName(sourceFilePath)}",
                                "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            try
            {
                string directory = Path.GetDirectoryName(sourceFilePath);
                string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
                string extension = Path.GetExtension(sourceFilePath);

                // Create a new, unique filename to avoid conflicts (e.g., "MyImage_copy_a1b2c3.png")
                string newFileName = $"{fileName}_copy_{Guid.NewGuid().ToString("N").Substring(0, 6)}{extension}";
                string newFilePath = Path.Combine(directory, newFileName);

                File.Copy(sourceFilePath, newFilePath);

                return newFilePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create a copy of the file: {Path.GetFileName(sourceFilePath)}\nError: {ex.Message}",
                                "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

        }
        private void CleanUpSavedProject()
        {
            try
            {
                // 1. إفراغ اللوحات التي تحتوي على عناصر المشروع المحفوظ فقط
                // panel1 لا يتم المساس به هنا.
                ClearFlowLayoutPanel(flowLayoutPanel1);
                ClearFlowLayoutPanel(flowLayoutPanel2);
                ClearImageApostille();
                ClearPanelDocx();

                // 2. إلغاء تحديد أي صور متبقية
                ClearAllSelections();

                // 3. مسح حقول الإدخال للتحضير للمشروع التالي
                ClearFormFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تنظيف المشروع المحفوظ:\n{ex.Message}", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
    //*************************************

}

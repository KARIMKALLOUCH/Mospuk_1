using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Mospuk_1
{

    public partial class AddFile : Form
    {
        private DragDropHandler _dragDropHandler; // **الكائن الجديد لإدارة السحب والإفلات**
        private DataLoaderAddfille _dataLoader; // **إضافة كائن لتحميل البيانات**
        private WorkspaceCleaner _workspaceCleaner; // **الكائن الجديد لتنظيف مساحة العمل**
        private KeyboardActionHandler _keyboardActionHandler; // **إضافة الكائن الجديد**
        private List<StagedProject> _stagedProjects = new List<StagedProject>();

        SQLiteDatabase db;

       
        private enum ResizeDirection { None, Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight }
        // إضافة هذه الخصائص العامة
        public FlowLayoutPanel FlowLayoutPanel1 => flowLayoutPanel1;
        public FlowLayoutPanel FlowLayoutPanel2 => flowLayoutPanel2;
        public Panel PanelDocx => panelDocx;
        public string NotesText => txtnotes.Text;
        public AddFile(SQLiteDatabase database)
        {
            InitializeComponent();
            db = database;
            _dataLoader = new DataLoaderAddfille(db); // **إنشاء كائن DataLoader هنا**

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
            PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            _dragDropHandler = new DragDropHandler(this, panel1, flowLayoutPanel1, flowLayoutPanel2, imageApostille);
            _dragDropHandler.Initialize();
            _workspaceCleaner = new WorkspaceCleaner(this, _dragDropHandler);
            _keyboardActionHandler = new KeyboardActionHandler(this, _dragDropHandler);
            this.KeyPreview = true;
            this.KeyDown += AddFile_KeyDown;
            this.AcceptButton = null;
            panel1.KeyDown += HandleSelectAll;
            flowLayoutPanel1.KeyDown += HandleSelectAll;
            flowLayoutPanel2.KeyDown += HandleSelectAll;
            panel1.AutoScroll = true;
            panel1.Padding = new Padding(10);
            flowLayoutPanel1.MouseDown += (s, e) => flowLayoutPanel1.Focus();
            flowLayoutPanel2.MouseDown += (s, e) => flowLayoutPanel2.Focus();
            this.Click += EmptySpace_Click;
            panel1.Click += EmptySpace_Click;
            flowLayoutPanel1.Click += EmptySpace_Click;
            flowLayoutPanel2.Click += EmptySpace_Click;
            panelDocx.Click += EmptySpace_Click;
        }
        private void AddFile_FormClosing(object sender, FormClosingEventArgs e)
        {
            _workspaceCleaner.AddFile_FormClosing();
            // تنظيف جميع المجلدات المؤقتة
            try
            {
                string tempFolder = Path.Combine(Application.StartupPath, "ExtractedFiles");
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
            catch (Exception ex)
            {
                // يمكنك تسجيل الخطأ إذا لزم الأمر
            }

        }
        private void AddFile_KeyDown(object sender, KeyEventArgs e)
        {
            _keyboardActionHandler.HandleKeyDown(e);

        }
        public void OpenImage_DoubleClick(object sender, EventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb != null)
            {
                OpenFileInIsolatedDirectory(pb);
            }
        }
        private void btnUplaod_Click(object sender, EventArgs e)
        {
            string downloadsPath = db.GetSavedPathById("downloads");
            if (string.IsNullOrEmpty(downloadsPath) || !Directory.Exists(downloadsPath))
            {
                MessageBox.Show("Please set a downloads directory first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All Supported Files|*.rar;*.zip;*.jpg;*.jpeg;*.png;*.pdf;*.docx;*.xlsx|RAR Files|*.rar|ZIP Files|*.zip|Images|*.jpg;*.jpeg;*.png|PDF Files|*.pdf|All Files|*.*";
            ofd.Multiselect = true;
            ofd.InitialDirectory = downloadsPath;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                // إنشاء مجلد مؤقت فريد لهذه العملية
                string uniqueOutputFolder = Path.Combine(Application.StartupPath, "ExtractedFiles", Guid.NewGuid().ToString());
                Directory.CreateDirectory(uniqueOutputFolder);

                foreach (string path in ofd.FileNames)
                {
                    string extension = Path.GetExtension(path).ToLower();
                    if (extension == ".rar")
                    {
                        _dataLoader.ExtractRAR(path, uniqueOutputFolder);
                    }
                    else if (extension == ".zip")
                    {
                        _dataLoader.ExtractZIP(path, uniqueOutputFolder);
                    }
                    else
                    {
                        File.Copy(path, Path.Combine(uniqueOutputFolder, Path.GetFileName(path)), true);
                    }
                }

                DisplayFilesFromTempFolder(uniqueOutputFolder);
            }
        }
        public void DisplayFiles(string directory)
        {
            // إنشاء مجلد مؤقت فريد لكل عملية رفع
            string uniqueTempFolder = Path.Combine(Application.StartupPath, "ExtractedFiles", Guid.NewGuid().ToString());
            Directory.CreateDirectory(uniqueTempFolder);

            // نسخ الملفات المستخرجة إلى المجلد المؤقت الجديد
            foreach (string file in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                string destFile = Path.Combine(uniqueTempFolder, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            // الآن استخدم المجلد المؤقت الجديد للعرض
            DisplayFilesFromTempFolder(uniqueTempFolder);
        }
        private void DisplayFilesFromTempFolder(string tempDirectory)
        {
            var allPictureBoxes = panel1.Controls.OfType<PictureBox>()
                .Concat(flowLayoutPanel1.Controls.OfType<PictureBox>())
                .Concat(flowLayoutPanel2.Controls.OfType<PictureBox>());

            var existingFilePaths = new HashSet<string>(allPictureBoxes.Where(pb => pb != null && pb.Tag != null).Select(pb => pb.Tag.ToString()));

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

            string[] allFilesInDirectory = Directory.GetFiles(tempDirectory, "*.*", SearchOption.AllDirectories);

            foreach (string file in allFilesInDirectory)
            {
                if (existingFilePaths.Contains(file)) continue;

                string ext = Path.GetExtension(file).ToLower();
                PictureBox pb = new PictureBox
                {
                    Width = maxWidth,
                    Height = maxHeight,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.None,
                    BackColor = Color.Transparent,
                    Tag = file, // سيحتوي الآن على المسار المؤقت الفريد
                    Location = new Point(x, y)
                };

                if (ext == ".pdf")
                {
                    pb.Image = Properties.Resources.pdficon;
                }
                else if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                {
                    try
                    {
                        using (var imgTemp = Image.FromFile(file)) pb.Image = new Bitmap(imgTemp);
                    }
                    catch { pb.BackColor = Color.White; }
                }
                else if (ext == ".docx" || ext == ".doc")
                {
                    pb.Image = Properties.Resources.wordicon;
                }
                else
                {
                    pb.BackColor = Color.LightGray;
                    pb.Paint += (s, e_paint) => { e_paint.Graphics.DrawString(Path.GetFileName(file), new Font("Arial", 8), Brushes.Black, new PointF(5, 40)); };
                }

                // **استخدام معالجات الأحداث من الكلاس الجديد**
                pb.DoubleClick += OpenImage_DoubleClick;
                pb.MouseDown += _dragDropHandler.Pb_MouseDown_Panel1;
                pb.MouseMove += _dragDropHandler.Pb_MouseMove_Panel1;
                pb.MouseUp += _dragDropHandler.Pb_MouseUp_Panel1;
                pb.Click += _dragDropHandler.Pb_Click_Panel1;
                pb.Paint += _dragDropHandler.PictureBox_Paint_Selection;
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
        public void ReArrangeImages()
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
        private void savebtn_Click(object sender, EventArgs e)
        {
            this.ActiveControl = null;

            // This is now the "Staging" button
            try
            {
                // Validate inputs (same validation as before)
                if (cmbUser.SelectedItem == null)
                {
                    MessageBox.Show("Please select a user.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbUser.Focus();
                    return;
                }

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

                // Generate project order
                int newOrder;
                if (numProjectOrder.Value > 0)
                {
                    int manualOrder = (int)numProjectOrder.Value;

                    // Check if order already exists in staged projects
                    bool orderExistsInStaged = _stagedProjects.Any(sp =>
                        sp.ReceptionDate.Date == receptionDate.Date && sp.ProjectOrder == manualOrder);

                    if (orderExistsInStaged)
                    {
                        MessageBox.Show($"Project order {manualOrder} already exists in staged projects for date {receptionDate:yyyy-MM-dd}. Please choose a different number.",
                            "Duplicate Order", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        numProjectOrder.Focus();
                        return;
                    }

                    // Check database
                    string checkOrderQuery = "SELECT COUNT(*) FROM projects WHERE reception_date = @date AND project_order = @order";
                    object existsResult = db.ExecuteScalar(checkOrderQuery, new List<SQLiteParameter>
                {
                    new SQLiteParameter("@date", receptionDate.ToString("yyyy-MM-dd")),
                    new SQLiteParameter("@order", manualOrder)
                });

                    int existingCount = Convert.ToInt32(existsResult);
                    if (existingCount > 0)
                    {
                        MessageBox.Show($"Project order {manualOrder} already exists for date {receptionDate:yyyy-MM-dd}. Please choose a different number.",
                            "Duplicate Order", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        numProjectOrder.Focus();
                        return;
                    }

                    newOrder = manualOrder;
                }
                else
                {
                    // Get highest order from database
                    string orderQuery = "SELECT IFNULL(MAX(project_order), 0) FROM projects WHERE reception_date = @date";
                    object result = db.ExecuteScalar(orderQuery, new List<SQLiteParameter>
                {
                    new SQLiteParameter("@date", receptionDate.ToString("yyyy-MM-dd"))
                });
                    int lastOrder = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);

                    // Also check staged projects for same date
                    int maxStagedOrder = _stagedProjects
                        .Where(sp => sp.ReceptionDate.Date == receptionDate.Date)
                        .Select(sp => sp.ProjectOrder)
                        .DefaultIfEmpty(0)
                        .Max();

                    newOrder = Math.Max(lastOrder, maxStagedOrder) + 1;
                }

                string selectedUser = cmbUser.Text;
                string documentType = selectedDocTypePair.Value;
                string translationType = selectedTranslationPair.Value;

                // Create staged project
                var stagedProject = new StagedProject
                {
                    CompanyClient = companyClient,
                    ReceptionDate = receptionDate,
                    ReceptionTime = receptionTime,
                    DeliveryDays = deliveryDays,
                    DeliveryDate = deliveryDate,
                    ProjectOrder = newOrder,
                    Note = txtnotes.Text,
                    DocumentType = documentType,
                    TranslationType = translationType,
                    SelectedUser = selectedUser
                };

                // Generate folder name and filenames
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
                    folderName += $"-------------{sanitizedNote}-------------{selectedUser}";
                }
                else
                {
                    folderName += $"--------------------------{selectedUser}";
                }

                stagedProject.FolderName = folderName;

                // Generate all filenames and store control copies
                GenerateFilenamesForStagedProject(stagedProject, deliveryDateStr, receptionDateStr,
                    projectOrderStr, receptionTimeStr, companyClient, translationType, documentType, selectedUser);

                // Add to staged projects list
                _stagedProjects.Add(stagedProject);

                // Display filenames in DataGridView
                PopulateDataGridViewWithFiles(stagedProject.GeneratedFileNames);

                // Clear the form for next project
                ClearFormForNextProject();

                MessageBox.Show($"Project staged successfully! Generated {stagedProject.GeneratedFileNames.Count} filenames.",
                    "Project Staged", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error staging project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void RemoveImageFromPanel1(string filePath)
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
                pbToRemove.Image?.Dispose();
                pbToRemove.Image = null;
                panel1.Controls.Remove(pbToRemove);
                pbToRemove.Dispose();
                ReArrangeImages();
            }
        }
        public void AddImageBackToPanel1(string filePath)
        {
            int padding = 10;
            int maxWidth = 120;
            int maxHeight = 120;
            int itemsPerRow = Math.Max(1, (panel1.ClientSize.Width - padding) / (maxWidth + padding));
            int count = panel1.Controls.Count;
            int x = padding + (count % itemsPerRow) * (maxWidth + padding);
            int y = padding + (count / itemsPerRow) * (maxHeight + padding);

            PictureBox pb = new PictureBox
            {
                Width = maxWidth,
                Height = maxHeight,
                BorderStyle = BorderStyle.None,
                SizeMode = PictureBoxSizeMode.Zoom, // <--- هذا هو التغيير: تم إضافة هذه الخاصية
                Tag = filePath,
                Location = new Point(x, y)
            };

            SetPictureBoxContent(pb, filePath);

            // **استخدام معالجات الأحداث من الكلاس الجديد**
            pb.DoubleClick += OpenImage_DoubleClick;
            pb.MouseDown += _dragDropHandler.Pb_MouseDown_Panel1;
            pb.MouseMove += _dragDropHandler.Pb_MouseMove_Panel1;
            pb.MouseUp += _dragDropHandler.Pb_MouseUp_Panel1;
            pb.Click += _dragDropHandler.Pb_Click_Panel1;
            pb.Paint += _dragDropHandler.PictureBox_Paint_Selection;
            panel1.Controls.Add(pb);
        }
        private void AddFile_Load(object sender, EventArgs e)
        {
            Delivery_Date.Items.Add(new KeyValuePair<string, int>("3 days", 3));
            Delivery_Date.Items.Add(new KeyValuePair<string, int>("Urgent (1 day)", 1));
            Delivery_Date.DisplayMember = "Key";
            Delivery_Date.ValueMember = "Value";
            Delivery_Date.SelectedIndex = 0;
            Reception_Date.Value = DateTime.Now;
            Time.Text = DateTime.Now.ToString("HH:mm");
          
            Time.KeyUp += Time_KeyUp_AutoJump;
            Time.TextChanged += Time_TextChanged_Validate;
            Time.KeyDown += Time_KeyDown;
            lblStatus.Visible = false;
            _dataLoader.LoadClientsAndCompanies(Company_Client);
            _dataLoader.LoadDocumentTypesToComboBox(comboDocumentType);
            _dataLoader.LoadLanguagePairsToComboBox(comboTranslation);
            _dataLoader.LoadUsersToCombo(cmbUser);
        }
        private void btnAddWord_Click(object sender, EventArgs e)
        {
            this.ActiveControl = null;

            if (panelDocx.Controls.Count >= 1)
            {
                MessageBox.Show("You can only add one OCR Word file.\nPlease remove the existing file if you wish to add a different one.", "Limit Reached", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string projectFolder = db.GetSavedPathById("archive");
            if (string.IsNullOrEmpty(projectFolder))
            {
                MessageBox.Show("Please set a save directory first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            OpenFileDialog ofd = new OpenFileDialog
            {
                InitialDirectory = projectFolder,
                Filter = "Word Files|*.doc;*.docx|All Files|*.*",
                Title = "Select Word File",
                Multiselect = false
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string filePath = ofd.FileName;
                string fileName = Path.GetFileName(filePath);
                bool fileExists = panelDocx.Controls.OfType<Label>().Any(lbl => lbl.Tag?.ToString() == filePath);
                if (!fileExists)
                {
                    Label lbl = new Label
                    {
                        Text = fileName,
                        Tag = filePath,
                        AutoSize = true,
                        Padding = new Padding(5),
                        Margin = new Padding(5),
                        BackColor = Color.SeaGreen,
                        ForeColor = Color.White,
                        Cursor = Cursors.Hand,
                        BorderStyle = BorderStyle.FixedSingle
                    };
                    ToolTip toolTip = new ToolTip();
                    toolTip.SetToolTip(lbl, filePath);
                    lbl.DoubleClick += (s, ev) =>
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo() { FileName = filePath, UseShellExecute = true });
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
                        if (MessageBox.Show($"Are you sure you want to remove '{fileName}' from the list?", "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
        private void EmptySpace_Click(object sender, EventArgs e)
        {
            _dragDropHandler.ClearAllSelections();
            panel1.Invalidate();
            flowLayoutPanel1.Invalidate();
            flowLayoutPanel2.Invalidate();
            if (sender is Control clickedControl)
            {
                clickedControl.Focus();
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
                    _dragDropHandler.ClearAllSelections();
                    foreach (PictureBox pb in panel1.Controls.OfType<PictureBox>())
                    {
                        _dragDropHandler.selectedPictureBoxes.Add(pb);
                        pb.Invalidate();
                    }
                    panel1.Invalidate();
                }
                else if (focusedControl == flowLayoutPanel1)
                {
                    _dragDropHandler.ClearAllSelections();
                    foreach (PictureBox pb in flowLayoutPanel1.Controls.OfType<PictureBox>().Where(p => p.Image != null))
                    {
                        _dragDropHandler.selectedPictureBoxesFlow1.Add(pb);
                        pb.Invalidate();
                    }
                }
                else if (focusedControl == flowLayoutPanel2)
                {
                    _dragDropHandler.ClearAllSelections();
                    foreach (PictureBox pb in flowLayoutPanel2.Controls.OfType<PictureBox>().Where(p => p.Image != null))
                    {
                        _dragDropHandler.selectedPictureBoxesFlow2.Add(pb);
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
                _dragDropHandler.ClearAllSelections();
                foreach (PictureBox pb in panel1.Controls.OfType<PictureBox>())
                {
                    _dragDropHandler.selectedPictureBoxes.Add(pb);
                    pb.Invalidate();
                }
                foreach (PictureBox pb in flowLayoutPanel1.Controls.OfType<PictureBox>().Where(p => p.Image != null))
                {
                    _dragDropHandler.selectedPictureBoxesFlow1.Add(pb);
                    pb.Invalidate();
                }
                foreach (PictureBox pb in flowLayoutPanel2.Controls.OfType<PictureBox>().Where(p => p.Image != null))
                {
                    _dragDropHandler.selectedPictureBoxesFlow2.Add(pb);
                    pb.Invalidate();
                }
                PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
                if (imageApostille?.Image != null)
                {
                    _dragDropHandler.selectedPictureBoxes.Add(imageApostille);
                    imageApostille.Invalidate();
                }
            }
        }
        public class PictureBoxDragInfo
        {
            public PictureBox PictureBox { get; set; }
            public string SourceType { get; set; }
            public string FilePath { get; set; }
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
            if (mtb?.SelectionStart == 2)
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
                if (int.TryParse(mtb.Text.Substring(0, 2), out int hour) && hour > 23)
                {
                    int currentSelection = mtb.SelectionStart;
                    mtb.Text = "23" + mtb.Text.Substring(2);
                    mtb.SelectionStart = currentSelection;
                }
            }
            if (mtb.MaskCompleted)
            {
                if (int.TryParse(mtb.Text.Substring(3, 2), out int minute) && minute > 59)
                {
                    int currentSelection = mtb.SelectionStart;
                    mtb.Text = mtb.Text.Substring(0, 3) + "59";
                    mtb.SelectionStart = currentSelection;
                }
            }
            mtb.TextChanged += Time_TextChanged_Validate;
        }
        public class KeyboardDragItem
        {
            public string FilePath { get; set; }
            public PictureBox SourceControl { get; set; }
        }
        public void SetPictureBoxContent(PictureBox targetPic, string filePath)
        {
            if (targetPic == null || string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            string ext = Path.GetExtension(filePath).ToLower();
            targetPic.Tag = filePath;
            targetPic.Image?.Dispose(); // التخلص من الصورة الحالية لتجنب مشاكل الذاكرة

            if (ext == ".pdf")
            {
                targetPic.Image = Properties.Resources.pdficon; // عرض أيقونة PDF لملفات PDF
            }
            else if (ext == ".docx" || ext == ".doc")
            {
                targetPic.Image = Properties.Resources.wordicon; // عرض أيقونة Word لملفات Word
            }
            else
            {
                // لمحاولة تحميل الصورة الحقيقية للملفات الأخرى (مثل JPG, PNG)
                try
                {
                    using (var imgTemp = Image.FromFile(filePath))
                    {
                        targetPic.Image = new Bitmap(imgTemp);
                    }
                }
                catch
                {
                    // في حالة فشل تحميل الصورة، عرض خلفية حمراء
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
            if (e.KeyCode == Keys.Right && mtb.SelectionStart == 2)
            {
                this.BeginInvoke((MethodInvoker)delegate { mtb.Select(3, 2); });
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.Left && mtb.SelectionStart == 3)
            {
                this.BeginInvoke((MethodInvoker)delegate { mtb.Select(0, 2); });
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Home || e.KeyCode == Keys.End || e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                return;
            }
            if (mtb.SelectionStart >= 0 && mtb.SelectionStart <= 1)
            {
                int numberPressed = (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) ? e.KeyCode - Keys.D0 : (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ? e.KeyCode - Keys.NumPad0 : -1;
                if (mtb.SelectionStart == 0 && numberPressed >= 3 && numberPressed <= 9)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        mtb.Text = "0" + numberPressed.ToString();
                        mtb.Select(3, 0);
                    });
                }
            }
            else if (mtb.SelectionStart >= 3 && mtb.SelectionStart <= 4)
            {
                int numberPressed = (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) ? e.KeyCode - Keys.D0 : (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ? e.KeyCode - Keys.NumPad0 : -1;
                if (mtb.SelectionStart == 3 && numberPressed >= 6 && numberPressed <= 9)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        string hours = mtb.Text.Substring(0, 3);
                        mtb.Text = hours + "0" + numberPressed.ToString();
                        mtb.Select(5, 0);
                    });
                }
            }
        }
        private void OpenFileInIsolatedDirectory(PictureBox clickedPictureBox)
        {
            if (clickedPictureBox?.Tag == null) return;
            string clickedFilePath = clickedPictureBox.Tag.ToString();
            string ext = Path.GetExtension(clickedFilePath).ToLower();
            if (ext == ".doc" || ext == ".docx")
            {
                try { Process.Start(new ProcessStartInfo() { FileName = clickedFilePath, UseShellExecute = true }); }
                catch (Exception ex) { MessageBox.Show("خطأ في فتح ملف الوورد: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                return;
            }

            List<string> sourceFilePaths = new List<string>();
            int clickedFileIndex = 0;
            Control parentControl = clickedPictureBox.Parent;
            if (clickedPictureBox.Name == "imageApostille") parentControl = clickedPictureBox;

            if (parentControl == panel1)
            {
                var orderedPictureBoxes = panel1.Controls.OfType<PictureBox>().Where(pb => pb.Tag != null && !pb.Tag.ToString().EndsWith(".docx") && !pb.Tag.ToString().EndsWith(".doc")).OrderBy(p => p.Location.Y).ThenBy(p => p.Location.X).ToList();
                sourceFilePaths = orderedPictureBoxes.Select(pb => pb.Tag.ToString()).ToList();
                clickedFileIndex = orderedPictureBoxes.FindIndex(pb => pb.Tag.ToString() == clickedFilePath);
            }
            else if (parentControl == flowLayoutPanel1)
            {
                var orderedPictureBoxes = flowLayoutPanel1.Controls.OfType<PictureBox>().Where(pb => pb.Tag != null && !pb.Tag.ToString().EndsWith(".docx") && !pb.Tag.ToString().EndsWith(".doc")).ToList();
                sourceFilePaths = orderedPictureBoxes.Select(pb => pb.Tag.ToString()).ToList();
                clickedFileIndex = orderedPictureBoxes.FindIndex(pb => pb.Tag.ToString() == clickedFilePath);
            }
            else if (parentControl == flowLayoutPanel2)
            {
                var orderedPictureBoxes = flowLayoutPanel2.Controls.OfType<PictureBox>().Where(pb => pb.Tag != null && !pb.Tag.ToString().EndsWith(".docx") && !pb.Tag.ToString().EndsWith(".doc")).ToList();
                sourceFilePaths = orderedPictureBoxes.Select(pb => pb.Tag.ToString()).ToList();
                clickedFileIndex = orderedPictureBoxes.FindIndex(pb => pb.Tag.ToString() == clickedFilePath);
            }
            else if (parentControl == clickedPictureBox && clickedPictureBox.Name == "imageApostille")
            {
                sourceFilePaths.Add(clickedFilePath);
                clickedFileIndex = 0;
            }

            if (!sourceFilePaths.Any()) return;
            string tempDir = Path.Combine(Path.GetTempPath(), "MospukViewer_" + Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(tempDir);
                for (int i = 0; i < sourceFilePaths.Count; i++)
                {
                    string sourcePath = sourceFilePaths[i];
                    if (File.Exists(sourcePath))
                    {
                        string extension = Path.GetExtension(sourcePath);
                        string newFileName = $"{i:D3}_{Path.GetFileNameWithoutExtension(sourcePath)}{extension}";
                        string destPath = Path.Combine(tempDir, newFileName);
                        File.Copy(sourcePath, destPath);
                        if (i == clickedFileIndex) clickedFilePath = destPath;
                    }
                }

                if (File.Exists(clickedFilePath))
                {
                    var process = new Process { StartInfo = new ProcessStartInfo() { FileName = clickedFilePath, UseShellExecute = true }, EnableRaisingEvents = true };
                    process.Exited += (s, args) =>
                    {
                        try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); }
                        catch { }
                    };
                    process.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تهيئة عارض الصور: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }
        private void Delivery_Date_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Delivery_Date.SelectedItem == null) return;
            var selectedItem = (KeyValuePair<string, int>)Delivery_Date.SelectedItem;
            string selectedText = selectedItem.Key;
            if (selectedText.Contains("Urgent"))
            {
                if (string.IsNullOrWhiteSpace(txtnotes.Text) || txtnotes.Text.Trim().Equals("Urgent", StringComparison.OrdinalIgnoreCase))
                {
                    txtnotes.Text = "URGENT";
                }
                else if (!txtnotes.Text.Trim().StartsWith("Urgent", StringComparison.OrdinalIgnoreCase))
                {
                    txtnotes.Text = "URGENT " + txtnotes.Text;
                }
            }
            else
            {
                if (txtnotes.Text.Trim().Equals("Urgent", StringComparison.OrdinalIgnoreCase))
                {
                    txtnotes.Clear();
                }
            }
        }

        private void PopulateDataGridViewWithFiles(List<string> fileNames)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => PopulateDataGridViewWithFiles(fileNames)));
                return;
            }
            if (fileNames != null && fileNames.Any())
            {
                foreach (string fileName in fileNames)
                {
                    guna2DataGridView1.Rows.Add(fileName);
                }
            }
        }

        private void btnsaveall_Click(object sender, EventArgs e)
        {

            this.ActiveControl = null;
            // This is now the "Final Save" button
            if (!_stagedProjects.Any())
            {
                MessageBox.Show("No staged projects to save.", "No Staged Projects", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            lblStatus.Text = "Saving all staged projects, please wait...";
            lblStatus.Visible = true;
            this.Enabled = false;
            Application.DoEvents();

            try
            {
                int successCount = 0;
                int totalCount = _stagedProjects.Count;

                foreach (var stagedProject in _stagedProjects)
                {
                    try
                    {
                        if (SaveStagedProject(stagedProject))
                        {
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving project {stagedProject.FolderName}: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                // Clear staged projects and DataGridView
                _stagedProjects.Clear();
                guna2DataGridView1.Rows.Clear();

                MessageBox.Show($"Saved {successCount} of {totalCount} projects successfully.", "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                lblStatus.Visible = false;
                this.Enabled = true;
            }
        }
        private void GenerateFilenamesForStagedProject(StagedProject stagedProject, string deliveryDateStr,
        string receptionDateStr, string projectOrderStr, string receptionTimeStr, string companyClient,
        string translationType, string documentType, string selectedUser)
        {
            int imageCounter = 1;

            // Store copies of controls for later processing
            foreach (Control control in flowLayoutPanel1.Controls)
            {
                if (control is PictureBox pb && pb.Tag != null)
                {
                    // Create a copy of the PictureBox
                    PictureBox pbCopy = new PictureBox
                    {
                        Tag = pb.Tag,
                        Image = pb.Image?.Clone() as Image
                    };
                    stagedProject.MainImages.Add(pbCopy);

                    // Generate filename
                    string originalPath = pb.Tag.ToString();
                    string extension = Path.GetExtension(originalPath).ToLower();
                    string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}";

                    if (imageCounter == 1)
                    {
                        imageName = AddNoteToFileName(imageName, stagedProject.Note, selectedUser);
                    }

                    string fullItemName = imageName + extension;
                    stagedProject.GeneratedFileNames.Add(fullItemName);
                    imageCounter++;
                }
            }

            // Handle Apostille image
            PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            if (imageApostille != null && imageApostille.Image != null && imageApostille.Tag != null)
            {
                // Create copy
                PictureBox apostilleCopy = new PictureBox
                {
                    Tag = imageApostille.Tag,
                    Image = imageApostille.Image?.Clone() as Image
                };
                stagedProject.ApostilleImage = apostilleCopy;

                string originalPath = imageApostille.Tag.ToString();
                string extension = Path.GetExtension(originalPath).ToLower();
                string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Apostilla";
                string fullImageName = imageName + extension;
                stagedProject.GeneratedFileNames.Add(fullImageName);
                imageCounter++;
            }

            // Handle attachments
            string attachmentType = "A";
            foreach (Control control in flowLayoutPanel2.Controls)
            {
                if (control is PictureBox pb && pb.Tag != null)
                {
                    // Create copy
                    PictureBox pbCopy = new PictureBox
                    {
                        Tag = pb.Tag,
                        Image = pb.Image?.Clone() as Image
                    };
                    stagedProject.Attachments.Add(pbCopy);

                    string originalPath = pb.Tag.ToString();
                    string extension = Path.GetExtension(originalPath).ToLower();
                    string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_{attachmentType}";
                    string fullItemName = imageName + extension;
                    stagedProject.GeneratedFileNames.Add(fullItemName);
                    imageCounter++;
                }
            }

            // Handle OCR files
            int ocrFileNumber = imageCounter;
            bool hasOcrFiles = false;
            foreach (Control control in panelDocx.Controls)
            {
                if (control is Label lbl && lbl.Tag != null)
                {
                    hasOcrFiles = true;
                    // Create copy
                    Label lblCopy = new Label
                    {
                        Tag = lbl.Tag,
                        Text = lbl.Text
                    };
                    stagedProject.OcrFiles.Add(lblCopy);

                    string originalPath = lbl.Tag.ToString();
                    string extension = Path.GetExtension(originalPath);
                    string wordFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{ocrFileNumber}_OCR";
                    string fullWordFileName = wordFileName + extension;
                    stagedProject.GeneratedFileNames.Add(fullWordFileName);
                    ocrFileNumber++;
                }
            }

            if (!hasOcrFiles) ocrFileNumber++;

            // Generate empty Word files
            string[] emptyFileNames = {
            "Google Drive.docx",
            "Traducción Preliminar.docx",
            "Informe revisión.docx",
            "Traducción revisada.docx"
        };

            for (int i = 0; i < emptyFileNames.Length; i++)
            {
                string fileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{ocrFileNumber}_{emptyFileNames[i]}";
                stagedProject.GeneratedFileNames.Add(fileName);
                ocrFileNumber++;
            }
        }

        private void ClearFormForNextProject()
        {
            // Clear all form fields
            Reception_Date.Value = DateTime.Now;
            Time.Text = DateTime.Now.ToString("HH:mm");
            Delivery_Date.SelectedIndex = 0;
            txtnotes.Clear();
            comboDocumentType.SelectedIndex = -1;
            comboTranslation.SelectedIndex = -1;
            numProjectOrder.Value = 0;

            // Clear all images and files
            _workspaceCleaner.CleanUpSavedProject();
        }
        private bool SaveStagedProject(StagedProject stagedProject)
        {
            // Insert project into database
            string insertQuery = @"INSERT INTO projects (company_client, reception_date, reception_time, delivery_days, delivery_date, hours_spent, project_order, folder_name, note, document_type, translation_type, registration_date, last_update_date) 
                              VALUES (@company_client, @reception_date, @reception_time, @delivery_days, @delivery_date, @hours_spent, @project_order, @folder_name, @note, @document_type, @translation_type, CURRENT_DATE, CURRENT_TIMESTAMP)";

            List<SQLiteParameter> parameters = new List<SQLiteParameter>
        {
            new SQLiteParameter("@company_client", stagedProject.CompanyClient),
            new SQLiteParameter("@reception_date", stagedProject.ReceptionDate.ToString("yyyy-MM-dd")),
            new SQLiteParameter("@reception_time", stagedProject.ReceptionTime),
            new SQLiteParameter("@delivery_days", stagedProject.DeliveryDays),
            new SQLiteParameter("@delivery_date", stagedProject.DeliveryDate.ToString("yyyy-MM-dd")),
            new SQLiteParameter("@hours_spent", 24),
            new SQLiteParameter("@project_order", stagedProject.ProjectOrder),
            new SQLiteParameter("@folder_name", stagedProject.FolderName),
            new SQLiteParameter("@note", string.IsNullOrWhiteSpace(stagedProject.Note) ? DBNull.Value : (object)stagedProject.Note),
            new SQLiteParameter("@document_type", stagedProject.DocumentType),
            new SQLiteParameter("@translation_type", stagedProject.TranslationType)
        };

            bool success = db.ExecuteNonQuery(insertQuery, parameters);

            if (success)
            {
                string getLastIdQuery = "SELECT last_insert_rowid()";
                object lastIdResult = db.ExecuteScalar(getLastIdQuery, null);
                int projectId = Convert.ToInt32(lastIdResult);

                // Save files using the staged project data
                return SaveStagedProjectFiles(projectId, stagedProject);
            }

            return false;
        }

        private bool SaveStagedProjectFiles(int projectId, StagedProject stagedProject)
        {
            string projectFolder = db.GetSavedPathById("save");
            if (string.IsNullOrEmpty(projectFolder) || !Directory.Exists(projectFolder))
            {
                Directory.CreateDirectory(projectFolder);
            }

            // Use similar logic to ProjectImageSaver but with staged data
            var imageSaver = new StagedProjectImageSaver(db, stagedProject);
            return imageSaver.SaveAllStagedFiles(projectId, projectFolder);
        }

        private string AddNoteToFileName(string baseName, string note, string selectedUser)
        {
            if (!string.IsNullOrWhiteSpace(note))
            {
                string rawNote = note.Trim();
                string sanitizedNote = new string(rawNote.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '+' || c == '-').ToArray()).Replace(" ", "_");
                if (sanitizedNote.Length > 30)
                    sanitizedNote = sanitizedNote.Substring(0, 30);
                return baseName + $"------------------------{sanitizedNote}----------------------{selectedUser}";
            }
            else
            {
                return baseName + $"-------------------------------------------------------------{selectedUser}";
            }
        }

    }    //*************************************
    public class StagedProject
    {
        public string CompanyClient { get; set; }
        public DateTime ReceptionDate { get; set; }
        public string ReceptionTime { get; set; }
        public int DeliveryDays { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int ProjectOrder { get; set; }
        public string FolderName { get; set; }
        public string Note { get; set; }
        public string DocumentType { get; set; }
        public string TranslationType { get; set; }
        public string SelectedUser { get; set; }
        public List<string> GeneratedFileNames { get; set; }

        // Store actual control data for later processing
        public List<PictureBox> MainImages { get; set; }
        public PictureBox ApostilleImage { get; set; }
        public List<PictureBox> Attachments { get; set; }
        public List<Label> OcrFiles { get; set; }

        public StagedProject()
        {
            GeneratedFileNames = new List<string>();
            MainImages = new List<PictureBox>();
            Attachments = new List<PictureBox>();
            OcrFiles = new List<Label>();
        }
    }

}
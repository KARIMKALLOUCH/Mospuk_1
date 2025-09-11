using DevExpress.Utils.Design.ImagePickerAddon;
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

        SQLiteDatabase db;

        private bool isResizingPanel = false;
        private Point lastMousePosition;
        private ResizeDirection resizeDirection;
        private enum ResizeDirection { None, Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight }
        private const int resizeBorderWidth = 10;
        // إضافة هذه الخصائص العامة
        public FlowLayoutPanel FlowLayoutPanel1 => flowLayoutPanel1;
        public FlowLayoutPanel FlowLayoutPanel2 => flowLayoutPanel2;
        public Panel PanelDocx => panelDocx;
        public string NotesText => txtnotes.Text;
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

            // **إنشاء وتهيئة معالج السحب والإفلات**
            PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            _dragDropHandler = new DragDropHandler(this, panel1, flowLayoutPanel1, flowLayoutPanel2, imageApostille);
            _dragDropHandler.Initialize();

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
            try
            {
                guna2DataGridView1.Rows.Clear();
                _dragDropHandler.ResetDragState();

                string tempFolder = Path.Combine(Application.StartupPath, "ExtractedFiles");
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
            catch { }
        }

        private void AddFile_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.X)
            {
                _dragDropHandler.HandleKeyboardDragStart();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                if (_dragDropHandler._keyboardDragItems.Any())
                {
                    _dragDropHandler.HandleKeyboardDrop();
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
                HandleCopy();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                List<PictureBox> allSelectedPictures = new List<PictureBox>();
                allSelectedPictures.AddRange(_dragDropHandler.selectedPictureBoxesFlow1);
                allSelectedPictures.AddRange(_dragDropHandler.selectedPictureBoxesFlow2);
                allSelectedPictures.AddRange(_dragDropHandler.selectedPictureBoxes);

                if (allSelectedPictures.Count > 0)
                {
                    var confirmResult = MessageBox.Show($"Are you sure you want to delete {allSelectedPictures.Count} selected items?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (confirmResult == DialogResult.Yes)
                    {
                        bool panel1NeedsRearrange = false;
                        foreach (var pb in allSelectedPictures.ToList())
                        {
                            string filePathToDelete = pb.Tag?.ToString();

                            if (pb.Parent == flowLayoutPanel1)
                            {
                                flowLayoutPanel1.Controls.Remove(pb);
                                _dragDropHandler.selectedPictureBoxesFlow1.Remove(pb);
                            }
                            else if (pb.Parent == flowLayoutPanel2)
                            {
                                flowLayoutPanel2.Controls.Remove(pb);
                                _dragDropHandler.selectedPictureBoxesFlow2.Remove(pb);
                            }
                            else if (pb.Parent == panel1)
                            {
                                panel1NeedsRearrange = true;
                                panel1.Controls.Remove(pb);
                                _dragDropHandler.selectedPictureBoxes.Remove(pb);
                            }
                            else if (pb.Name == "imageApostille")
                            {
                                pb.Image = null;
                                pb.Tag = null;
                            }

                            pb.Image?.Dispose();
                            pb.Dispose();

                            if (!string.IsNullOrEmpty(filePathToDelete) && File.Exists(filePathToDelete))
                            {
                                try
                                {
                                    File.Delete(filePathToDelete);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Could not delete the file: {Path.GetFileName(filePathToDelete)}\nError: {ex.Message}", "File Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }

                        _dragDropHandler.selectedPictureBoxesFlow1.Clear();
                        _dragDropHandler.selectedPictureBoxesFlow2.Clear();
                        _dragDropHandler.selectedPictureBoxes.Clear();

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
                    entry.WriteToDirectory(outputDirectory, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                }
            }
        }

        public void ExtractZIP(string zipPath, string outputDirectory)
        {
            using (var archive = ArchiveFactory.Open(zipPath))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory(outputDirectory, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                }
            }
        }

        public void DisplayFiles(string directory)
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

            string[] allFilesInDirectory = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
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
                    Tag = file,
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
            lblStatus.Text = "Saving, please wait...";
            lblStatus.Visible = true;
            this.Enabled = false;
            Application.DoEvents();

            try
            {
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

                // تحديد project_order
                int newOrder;
                if (numProjectOrder.Value > 0)
                {
                    int manualOrder = (int)numProjectOrder.Value;

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
                    string orderQuery = "SELECT IFNULL(MAX(project_order), 0) FROM projects WHERE reception_date = @date";
                    object result = db.ExecuteScalar(orderQuery, new List<SQLiteParameter>
            {
                new SQLiteParameter("@date", receptionDate.ToString("yyyy-MM-dd"))
            });
                    int lastOrder = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
                    newOrder = lastOrder + 1;
                }

                string selectedUser = cmbUser.Text;
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
                    folderName += $"-------------{sanitizedNote}-------------{selectedUser}";
                }
                else
                {
                    folderName += $"--------------------------{selectedUser}";
                }

                string insertQuery = @"INSERT INTO projects (company_client, reception_date, reception_time, delivery_days, delivery_date, hours_spent, project_order, folder_name, note, document_type, translation_type, registration_date, last_update_date) 
                              VALUES (@company_client, @reception_date, @reception_time, @delivery_days, @delivery_date, @hours_spent, @project_order, @folder_name, @note, @document_type, @translation_type, CURRENT_DATE, CURRENT_TIMESTAMP)";

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
                    object lastIdResult = db.ExecuteScalar(getLastIdQuery, null);
                    int projectId = Convert.ToInt32(lastIdResult);

                    // استدعاء دالة الحفظ من الكلاس الجديد
                    var imageSaver = new ProjectImageSaver(db, this);
                    var (allImagesSaved, savedSourcePaths, savedFileNames) = imageSaver.SaveProjectImages(
                        projectId, folderName, deliveryDateStr, receptionDateStr,
                        projectOrderStr, receptionTimeStr, companyClient, translationType,
                        documentType, selectedUser);

                    if (allImagesSaved)
                    {
                        PopulateDataGridViewWithFiles(savedFileNames);

                        // تنظيف الواجهة الرسومية
                        CleanUpSavedProject();
                        numProjectOrder.Value = 0;

                        // حذف الملفات المصدر من المجلد المؤقت
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
                                // يمكنك تسجيل الخطأ هنا إذا أردت
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
            LoadClientsAndCompanies();
            Time.Text = DateTime.Now.ToString("HH:mm");
            LoadDocumentTypesToComboBox();
            LoadLanguagePairsToComboBox();
            LoadUsersToCombo();
            Time.KeyUp += Time_KeyUp_AutoJump;
            Time.TextChanged += Time_TextChanged_Validate;
            Time.KeyDown += Time_KeyDown;
            lblStatus.Visible = false;
        }

        private void btnAddWord_Click(object sender, EventArgs e)
        {
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

        private void OpenSelectedImages()
        {
            List<PictureBox> allSelectedPictures = new List<PictureBox>();
            allSelectedPictures.AddRange(_dragDropHandler.selectedPictureBoxesFlow1);
            allSelectedPictures.AddRange(_dragDropHandler.selectedPictureBoxesFlow2);
            allSelectedPictures.AddRange(_dragDropHandler.selectedPictureBoxes);

            PictureBox firstSelected = allSelectedPictures.FirstOrDefault();
            if (firstSelected != null)
            {
                OpenFileInIsolatedDirectory(firstSelected);
            }
        }

        private bool AreAnyImagesSelected()
        {
            return _dragDropHandler.selectedPictureBoxes.Any() ||
                   _dragDropHandler.selectedPictureBoxesFlow1.Any() ||
                   _dragDropHandler.selectedPictureBoxesFlow2.Any();
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

        private void CleanWorkspace()
        {
            try
            {
                ClearFlowLayoutPanel(flowLayoutPanel1);
                ClearFlowLayoutPanel(flowLayoutPanel2);
                ClearImageApostille();
                ClearPanelDocx();
                _dragDropHandler.ClearAllSelections();
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
                MessageBox.Show($"حدث خطأ أثناء تنظيف مساحة العمل:\n{ex.Message}", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ClearFlowLayoutPanel(FlowLayoutPanel panel)
        {
            List<Control> controlsToRemove = new List<Control>();
            foreach (Control control in panel.Controls)
            {
                if (control is PictureBox) controlsToRemove.Add(control);
            }
            foreach (Control control in controlsToRemove)
            {
                PictureBox pb = control as PictureBox;
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
                if (control is Label) controlsToRemove.Add(control);
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
            comboDocumentType.SelectedIndex = -1;
            comboTranslation.SelectedIndex = -1;
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
                string query = @"SELECT client_id AS id, client_code AS code, 'Client' AS type FROM clients
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ في تحميل العملاء والشركات:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDocumentTypesToComboBox()
        {
            try
            {
                string query = "SELECT id, name AS code, 'Document Type' AS type FROM document_types ORDER BY type, code";
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading document types:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLanguagePairsToComboBox()
        {
            try
            {
                string query = "SELECT id, name AS code, 'Language Pair' AS type FROM language_pairs ORDER BY type, code";
                DataTable languagePairsData = db.ExecuteQuery(query, null);
                comboTranslation.Items.Clear();
                foreach (DataRow row in languagePairsData.Rows)
                {
                    int languagePairId = Convert.ToInt32(row["id"]);
                    string languagePairName = row["code"].ToString();
                    comboTranslation.Items.Add(new KeyValuePair<int, string>(languagePairId, languagePairName));
                }
                comboTranslation.DisplayMember = "Value";
                comboTranslation.ValueMember = "Key";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading language pairs:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void HandleCopy()
        {
            var allSelected = new List<PictureBox>();
            allSelected.AddRange(_dragDropHandler.selectedPictureBoxes);
            allSelected.AddRange(_dragDropHandler.selectedPictureBoxesFlow1);
            allSelected.AddRange(_dragDropHandler.selectedPictureBoxesFlow2);
            var uniqueSelected = allSelected.Distinct().ToList();
            if (!uniqueSelected.Any()) return;

            var filePaths = new List<string>();
            foreach (var pb in uniqueSelected)
            {
                if (pb?.Tag != null)
                {
                    string path = pb.Tag.ToString();
                    if (File.Exists(path)) filePaths.Add(path);
                }
            }
            if (!filePaths.Any()) return;
            var fileDropList = new System.Collections.Specialized.StringCollection();
            fileDropList.AddRange(filePaths.ToArray());
            try { Clipboard.SetFileDropList(fileDropList); }
            catch (Exception ex) { MessageBox.Show($"Could not copy files to clipboard: {ex.Message}", "Copy Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void HandlePaste()
        {
            if (!Clipboard.ContainsFileDropList()) return;
            var filePathsFromClipboard = Clipboard.GetFileDropList();
            if (filePathsFromClipboard == null || filePathsFromClipboard.Count == 0) return;

            Control targetControl = this.ActiveControl;
            PictureBox imageApostille = this.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            bool isValidTarget = (targetControl == panel1) || (targetControl == flowLayoutPanel1) || (targetControl == flowLayoutPanel2) || (targetControl == imageApostille);
            if (!isValidTarget)
            {
                MessageBox.Show("Please click on a panel to select a destination before pasting.", "No Destination Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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
            else if (targetControl is FlowLayoutPanel flowTarget)
            {
                foreach (string path in filePathsFromClipboard)
                {
                    string newFilePath = CreateFileCopyInWorkspace(path);
                    if (newFilePath != null)
                    {
                        var pic = new Guna.UI2.WinForms.Guna2PictureBox
                        {
                            Width = 130,
                            Height = 157,
                            BorderStyle = BorderStyle.None,
                            SizeMode = PictureBoxSizeMode.StretchImage,
                            BackColor = Color.Transparent,
                            Margin = new Padding(5),
                            AllowDrop = true
                        };
                        pic.Click += (s, e) => { /* Placeholder for generic pic click if needed */ };
                        pic.Paint += _dragDropHandler.PictureBox_Paint_Selection;

                        SetPictureBoxContent(pic, newFilePath);
                        if (pic.Image != null || Path.GetExtension(newFilePath).ToLower().Contains("doc"))
                        {
                            flowTarget.Controls.Add(pic);
                        }
                        else
                        {
                            pic.Dispose();
                        }
                    }
                }
            }
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
            _dragDropHandler.ClearAllSelections();
        }

        private string CreateFileCopyInWorkspace(string sourceFilePath)
        {
            if (!File.Exists(sourceFilePath))
            {
                MessageBox.Show($"The source file could not be found and cannot be pasted:\n{Path.GetFileName(sourceFilePath)}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            try
            {
                string directory = Path.Combine(Application.StartupPath, "ExtractedFiles");
                string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
                string extension = Path.GetExtension(sourceFilePath);
                string newFileName = $"{fileName}_copy_{Guid.NewGuid().ToString("N").Substring(0, 6)}{extension}";
                string newFilePath = Path.Combine(directory, newFileName);
                File.Copy(sourceFilePath, newFilePath, true);
                return newFilePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create a copy of the file: {Path.GetFileName(sourceFilePath)}\nError: {ex.Message}", "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private void CleanUpSavedProject()
        {
            try
            {
                ClearFlowLayoutPanel(flowLayoutPanel1);
                ClearFlowLayoutPanel(flowLayoutPanel2);
                ClearImageApostille();
                ClearPanelDocx();
                _dragDropHandler.ClearAllSelections();
                ClearFormFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تنظيف المشروع المحفوظ:\n{ex.Message}", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private System.Drawing.Imaging.ImageFormat GetImageFormat(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".jpg": case ".jpeg": return System.Drawing.Imaging.ImageFormat.Jpeg;
                case ".png": return System.Drawing.Imaging.ImageFormat.Png;
                case ".gif": return System.Drawing.Imaging.ImageFormat.Gif;
                case ".bmp": return System.Drawing.Imaging.ImageFormat.Bmp;
                case ".tiff": case ".tif": return System.Drawing.Imaging.ImageFormat.Tiff;
                default: return System.Drawing.Imaging.ImageFormat.Jpeg;
            }
        }

        private void LoadUsersToCombo()
        {
            try
            {
                string sql = @"SELECT user_id, user_code FROM users ORDER BY user_code";
                DataTable dt = db.ExecuteQuery(sql, null);
                cmbUser.DataSource = dt;
                cmbUser.DisplayMember = "user_code";
                cmbUser.ValueMember = "user_id";
                cmbUser.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading users: " + ex.Message);
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
    }    //*************************************

}
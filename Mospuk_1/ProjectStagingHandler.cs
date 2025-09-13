using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Mospuk_1
{

    public class ProjectStagingHandler
    {
        private readonly SQLiteDatabase _db;
        private readonly List<StagedProject> _stagedProjects = new List<StagedProject>();

        public IReadOnlyList<StagedProject> StagedProjects => _stagedProjects.AsReadOnly();
        public int StagedProjectCount => _stagedProjects.Count;

        public ProjectStagingHandler(SQLiteDatabase db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public StagedProject StageNewProject(
            string companyClient,
            DateTime receptionDate,
            string receptionTime,
            int deliveryDays,
            int manualProjectOrder,
            string note,
            string documentType,
            string translationType,
            string selectedUser,
            Control.ControlCollection mainImageControls,
            PictureBox apostilleImage,
            Control.ControlCollection attachmentControls,
            Control.ControlCollection ocrFileControls)
        {
            // 1. تحديد رقم ترتيب المشروع
            int newOrder = DetermineProjectOrder(receptionDate, manualProjectOrder);

            // 2. إنشاء كائن StagedProject
            DateTime deliveryDate = receptionDate.AddDays(deliveryDays);
            var stagedProject = new StagedProject
            {
                CompanyClient = companyClient,
                ReceptionDate = receptionDate,
                ReceptionTime = receptionTime,
                DeliveryDays = deliveryDays,
                DeliveryDate = deliveryDate,
                ProjectOrder = newOrder,
                Note = note,
                DocumentType = documentType,
                TranslationType = translationType,
                SelectedUser = selectedUser
            };

            // 3. توليد اسم المجلد وأسماء الملفات
            GenerateFolderAndFileNames(stagedProject, mainImageControls, apostilleImage, attachmentControls, ocrFileControls);

            // 4. إضافة المشروع إلى قائمة المشاريع المجهزة
            _stagedProjects.Add(stagedProject);

            return stagedProject;
        }

        /// <summary>
        /// يحفظ جميع المشاريع المجهزة في قاعدة البيانات والملفات النهائية.
        /// </summary>
        /// <returns>Tuple containing success count and total count.</returns>
        public (int SuccessCount, int TotalCount) SaveAllStagedProjects()
        {
            int successCount = 0;
            int totalCount = _stagedProjects.Count;

            foreach (var stagedProject in _stagedProjects)
            {
                try
                {
                    if (SaveSingleStagedProject(stagedProject))
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving project {stagedProject.FolderName}: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // مسح القائمة بعد محاولة حفظ الجميع
            _stagedProjects.Clear();

            return (successCount, totalCount);
        }

        #region Private Helper Methods

        private int DetermineProjectOrder(DateTime receptionDate, int manualOrder)
        {
            if (manualOrder > 0)
            {
                // التحقق من وجود الترتيب في المشاريع المجهزة حالياً لنفس التاريخ
                if (_stagedProjects.Any(sp => sp.ReceptionDate.Date == receptionDate.Date && sp.ProjectOrder == manualOrder))
                {
                    throw new InvalidOperationException($"Project order {manualOrder} already exists in staged projects for date {receptionDate:yyyy-MM-dd}. Please choose a different number.");
                }

                // التحقق في قاعدة البيانات
                string checkOrderQuery = "SELECT COUNT(*) FROM projects WHERE reception_date = @date AND project_order = @order";
                object existsResult = _db.ExecuteScalar(checkOrderQuery, new List<SQLiteParameter>
                {
                    new SQLiteParameter("@date", receptionDate.ToString("yyyy-MM-dd")),
                    new SQLiteParameter("@order", manualOrder)
                });

                if (Convert.ToInt32(existsResult) > 0)
                {
                    throw new InvalidOperationException($"Project order {manualOrder} already exists for date {receptionDate:yyyy-MM-dd}. Please choose a different number.");
                }
                return manualOrder;
            }
            else
            {
                // توليد الترتيب تلقائياً
                string orderQuery = "SELECT IFNULL(MAX(project_order), 0) FROM projects WHERE reception_date = @date";
                object result = _db.ExecuteScalar(orderQuery, new List<SQLiteParameter> { new SQLiteParameter("@date", receptionDate.ToString("yyyy-MM-dd")) });
                int lastDbOrder = (result == DBNull.Value) ? 0 : Convert.ToInt32(result);

                int maxStagedOrder = _stagedProjects
                    .Where(sp => sp.ReceptionDate.Date == receptionDate.Date)
                    .Select(sp => sp.ProjectOrder)
                    .DefaultIfEmpty(0)
                    .Max();

                return Math.Max(lastDbOrder, maxStagedOrder) + 1;
            }
        }

        private void GenerateFolderAndFileNames(StagedProject stagedProject, Control.ControlCollection mainImageControls, PictureBox apostilleImage, Control.ControlCollection attachmentControls, Control.ControlCollection ocrFileControls)
        {
            string deliveryDateStr = stagedProject.DeliveryDate.ToString("yyyyMMdd");
            string receptionDateStr = stagedProject.ReceptionDate.ToString("yyMMdd");
            string projectOrderStr = stagedProject.ProjectOrder.ToString("D2");
            string receptionTimeStr = stagedProject.ReceptionTime.Replace(":", "");
            string hoursSpent = "24";

            if (stagedProject.DeliveryDays == 1) deliveryDateStr = "0" + deliveryDateStr.Substring(1);

            string folderName = $"{deliveryDateStr}{hoursSpent}_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{stagedProject.CompanyClient}_{stagedProject.TranslationType}_{stagedProject.DocumentType}";

            if (!string.IsNullOrWhiteSpace(stagedProject.Note))
            {
                string rawNote = stagedProject.Note.Trim();
                string sanitizedNote = new string(rawNote.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '+' || c == '-').ToArray()).Replace(" ", "_");
                if (sanitizedNote.Length > 30) sanitizedNote = sanitizedNote.Substring(0, 30);
                folderName += $"-------------{sanitizedNote}-------------{stagedProject.SelectedUser}";
            }
            else
            {
                folderName += $"--------------------------{stagedProject.SelectedUser}";
            }
            stagedProject.FolderName = folderName;

            GenerateFilenamesForStagedProject(stagedProject, deliveryDateStr, receptionDateStr, projectOrderStr, receptionTimeStr, mainImageControls, apostilleImage, attachmentControls, ocrFileControls);
        }

        private void GenerateFilenamesForStagedProject(StagedProject stagedProject, string deliveryDateStr,
            string receptionDateStr, string projectOrderStr, string receptionTimeStr,
            Control.ControlCollection mainImageControls, PictureBox apostilleImage,
            Control.ControlCollection attachmentControls, Control.ControlCollection ocrFileControls)
        {
            // هذا الكود هو نفس الكود الأصلي لكن تم نقله إلى هنا
            // ... (The full logic from the original GenerateFilenamesForStagedProject method goes here)
            int imageCounter = 1;

            // Store copies of controls for later processing
            foreach (Control control in mainImageControls)
            {
                if (control is PictureBox pb && pb.Tag != null)
                {
                    stagedProject.MainImages.Add(new PictureBox { Tag = pb.Tag, Image = pb.Image?.Clone() as System.Drawing.Image });
                    string originalPath = pb.Tag.ToString();
                    string extension = Path.GetExtension(originalPath).ToLower();
                    string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{stagedProject.CompanyClient}_{stagedProject.TranslationType}_{stagedProject.DocumentType}_{imageCounter}";

                    if (imageCounter == 1)
                    {
                        imageName = AddNoteToFileName(imageName, stagedProject.Note, stagedProject.SelectedUser);
                    }

                    string fullItemName = imageName + extension;
                    stagedProject.GeneratedFileNames.Add(fullItemName);
                    imageCounter++;
                }
            }

            // Handle Apostille image
            if (apostilleImage != null && apostilleImage.Image != null && apostilleImage.Tag != null)
            {
                stagedProject.ApostilleImage = new PictureBox { Tag = apostilleImage.Tag, Image = apostilleImage.Image?.Clone() as System.Drawing.Image };
                string originalPath = apostilleImage.Tag.ToString();
                string extension = Path.GetExtension(originalPath).ToLower();
                string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{stagedProject.CompanyClient}_{stagedProject.TranslationType}_{stagedProject.DocumentType}_{imageCounter}_Apostilla";
                string fullImageName = imageName + extension;
                stagedProject.GeneratedFileNames.Add(fullImageName);
                imageCounter++;
            }

            // Handle attachments
            string attachmentType = "A";
            foreach (Control control in attachmentControls)
            {
                if (control is PictureBox pb && pb.Tag != null)
                {
                    stagedProject.Attachments.Add(new PictureBox { Tag = pb.Tag, Image = pb.Image?.Clone() as System.Drawing.Image });
                    string originalPath = pb.Tag.ToString();
                    string extension = Path.GetExtension(originalPath).ToLower();
                    string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{stagedProject.CompanyClient}_{stagedProject.TranslationType}_{stagedProject.DocumentType}_{imageCounter}_{attachmentType}";
                    string fullItemName = imageName + extension;
                    stagedProject.GeneratedFileNames.Add(fullItemName);
                    imageCounter++;
                }
            }

            // Handle OCR files
            int ocrFileNumber = imageCounter;
            bool hasOcrFiles = false;
            foreach (Control control in ocrFileControls)
            {
                if (control is Label lbl && lbl.Tag != null)
                {
                    hasOcrFiles = true;
                    stagedProject.OcrFiles.Add(new Label { Tag = lbl.Tag, Text = lbl.Text });
                    string originalPath = lbl.Tag.ToString();
                    string extension = Path.GetExtension(originalPath);
                    string wordFileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{stagedProject.CompanyClient}_{stagedProject.TranslationType}_{stagedProject.DocumentType}_{ocrFileNumber}_OCR";
                    string fullWordFileName = wordFileName + extension;
                    stagedProject.GeneratedFileNames.Add(fullWordFileName);
                    ocrFileNumber++;
                }
            }

            if (!hasOcrFiles) ocrFileNumber++;

            // Generate empty Word files
            string[] emptyFileNames = { "Google Drive.docx", "Traducción Preliminar.docx", "Informe revisión.docx", "Traducción revisada.docx" };
            for (int i = 0; i < emptyFileNames.Length; i++)
            {
                string fileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{stagedProject.CompanyClient}_{stagedProject.TranslationType}_{stagedProject.DocumentType}_{ocrFileNumber}_{emptyFileNames[i]}";
                stagedProject.GeneratedFileNames.Add(fileName);
                ocrFileNumber++;
            }
        }

        private string AddNoteToFileName(string baseName, string note, string selectedUser)
        {
            // نفس الكود الأصلي
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

        private bool SaveSingleStagedProject(StagedProject stagedProject)
        {
            // نفس الكود الأصلي
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

            if (_db.ExecuteNonQuery(insertQuery, parameters))
            {
                object lastIdResult = _db.ExecuteScalar("SELECT last_insert_rowid()", null);
                int projectId = Convert.ToInt32(lastIdResult);
                return SaveStagedProjectFiles(projectId, stagedProject);
            }
            return false;
        }

        private bool SaveStagedProjectFiles(int projectId, StagedProject stagedProject)
        {
            // نفس الكود الأصلي
            string projectFolder = _db.GetSavedPathById("save");
            if (string.IsNullOrEmpty(projectFolder) || !Directory.Exists(projectFolder))
            {
                Directory.CreateDirectory(projectFolder);
            }

            var imageSaver = new StagedProjectImageSaver(_db, stagedProject);
            return imageSaver.SaveAllStagedFiles(projectId, projectFolder);
        }

        #endregion
    }
}
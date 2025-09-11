using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Mospuk_1
{
    public class ProjectImageSaver
    {
        private SQLiteDatabase _db;
        private AddFile _addFileForm;

        public ProjectImageSaver(SQLiteDatabase db, AddFile addFileForm)
        {
            _db = db;
            _addFileForm = addFileForm;
        }

        public (bool success, List<string> savedSourcePaths, List<string> savedFileNames)
            SaveProjectImages(int projectId, string folderName, string deliveryDateStr,
            string receptionDateStr, string projectOrderStr, string receptionTimeStr,
            string companyClient, string translationType, string documentType, string selectedUser)
        {
            bool allSaved = true;
            int imageCounter = 1;
            var savedSourcePaths = new List<string>();
            var savedFileNames = new List<string>();

            string projectFolder = _db.GetSavedPathById("save");
            if (string.IsNullOrEmpty(projectFolder))
            {
                MessageBox.Show("Please set a save directory first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (false, savedSourcePaths, savedFileNames);
            }

            if (!Directory.Exists(projectFolder))
            {
                Directory.CreateDirectory(projectFolder);
            }

            // حفظ الصور الأساسية
            allSaved = SaveMainImages(projectId, deliveryDateStr, receptionDateStr, projectOrderStr,
                receptionTimeStr, companyClient, translationType, documentType, selectedUser,
                ref imageCounter, savedSourcePaths, savedFileNames, projectFolder, allSaved);

            // حفظ صورة Apostille
            allSaved = SaveApostilleImage(projectId, deliveryDateStr, receptionDateStr, projectOrderStr,
                receptionTimeStr, companyClient, translationType, documentType, selectedUser,
                ref imageCounter, savedSourcePaths, savedFileNames, projectFolder, allSaved);

            // حفظ المرفقات
            allSaved = SaveAttachments(projectId, deliveryDateStr, receptionDateStr, projectOrderStr,
                receptionTimeStr, companyClient, translationType, documentType, selectedUser,
                ref imageCounter, savedSourcePaths, savedFileNames, projectFolder, allSaved);

            // حفظ ملفات OCR Word
            allSaved = SaveOcrFiles(projectId, deliveryDateStr, receptionDateStr, projectOrderStr,
                receptionTimeStr, companyClient, translationType, documentType, selectedUser,
                ref imageCounter, savedSourcePaths, savedFileNames, projectFolder, allSaved);

            // إنشاء ملفات Word الفارغة
            allSaved = CreateEmptyWordFiles(projectId, deliveryDateStr, receptionDateStr, projectOrderStr,
                receptionTimeStr, companyClient, translationType, documentType, selectedUser,
                ref imageCounter, savedSourcePaths, savedFileNames, projectFolder, allSaved);

            return (allSaved, savedSourcePaths, savedFileNames);
        }

        private bool SaveMainImages(int projectId, string deliveryDateStr, string receptionDateStr,
            string projectOrderStr, string receptionTimeStr, string companyClient, string translationType,
            string documentType, string selectedUser, ref int imageCounter, List<string> savedSourcePaths,
            List<string> savedFileNames, string projectFolder, bool allSaved)
        {
            bool result = allSaved;

            foreach (Control control in _addFileForm.FlowLayoutPanel1.Controls)
            {
                if (control is PictureBox pb && pb.Tag != null)
                {
                    try
                    {
                        string originalPath = pb.Tag.ToString();
                        string extension = Path.GetExtension(originalPath).ToLower();
                        string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}";

                        if (imageCounter == 1)
                        {
                            imageName = AddNoteToFileName(imageName, _addFileForm.NotesText, selectedUser);
                        }

                        string fullItemName = imageName + extension;
                        string destinationPath = Path.Combine(projectFolder, fullItemName);

                        if (IsDocumentFile(extension) || extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            File.Copy(originalPath, destinationPath, true);
                        }
                        else if (pb.Image != null)
                        {
                            ImageFormat format = GetImageFormat(originalPath);
                            pb.Image.Save(destinationPath, format);
                        }
                        else
                        {
                            result = false;
                            MessageBox.Show($"Could not save item (no image found): {originalPath}");
                            continue;
                        }

                        SetFileTimestamps(destinationPath);

                        if (SaveImageToDatabase(projectId, fullItemName, destinationPath, null))
                        {
                            imageCounter++;
                            savedSourcePaths.Add(originalPath);
                            savedFileNames.Add(fullItemName);
                        }
                        else
                        {
                            result = false;
                            MessageBox.Show($"Failed to save database record for: {fullItemName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result = false;
                        MessageBox.Show($"Error saving item {imageCounter}: {ex.Message}");
                    }
                }
            }

            return result;
        }

        private bool SaveApostilleImage(int projectId, string deliveryDateStr, string receptionDateStr,
            string projectOrderStr, string receptionTimeStr, string companyClient, string translationType,
            string documentType, string selectedUser, ref int imageCounter, List<string> savedSourcePaths,
            List<string> savedFileNames, string projectFolder, bool allSaved)
        {
            bool result = allSaved;

            PictureBox imageApostille = _addFileForm.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            if (imageApostille != null && imageApostille.Image != null && imageApostille.Tag != null)
            {
                try
                {
                    string originalPath = imageApostille.Tag.ToString();
                    string extension = Path.GetExtension(originalPath).ToLower();
                    string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_Apostille";
                    string fullImageName = imageName + extension;
                    string imagePath = Path.Combine(projectFolder, fullImageName);

                    if (IsImageFile(extension) || extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(originalPath, imagePath, true);
                    }
                    else
                    {
                        ImageFormat format = GetImageFormat(originalPath);
                        imageApostille.Image.Save(imagePath, format);
                    }

                    SetFileTimestamps(imagePath);

                    if (SaveImageToDatabase(projectId, fullImageName, imagePath, "Apostille"))
                    {
                        imageCounter++;
                        savedSourcePaths.Add(originalPath);
                        savedFileNames.Add(fullImageName);
                    }
                    else
                    {
                        result = false;
                        MessageBox.Show($"Failed to save Apostille image: {fullImageName}");
                    }
                }
                catch (Exception ex)
                {
                    result = false;
                    MessageBox.Show($"Error saving Apostille image: {ex.Message}");
                }
            }

            return result;
        }

        private bool SaveAttachments(int projectId, string deliveryDateStr, string receptionDateStr,
            string projectOrderStr, string receptionTimeStr, string companyClient, string translationType,
            string documentType, string selectedUser, ref int imageCounter, List<string> savedSourcePaths,
            List<string> savedFileNames, string projectFolder, bool allSaved)
        {
            bool result = allSaved;
            string attachmentType = "A";

            foreach (Control control in _addFileForm.FlowLayoutPanel2.Controls)
            {
                if (control is PictureBox pb && pb.Tag != null)
                {
                    try
                    {
                        string originalPath = pb.Tag.ToString();
                        string extension = Path.GetExtension(originalPath).ToLower();
                        string imageName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_{attachmentType}";
                        string fullItemName = imageName + extension;
                        string destinationPath = Path.Combine(projectFolder, fullItemName);

                        if (IsDocumentFile(extension) || extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            File.Copy(originalPath, destinationPath, true);
                        }
                        else if (pb.Image != null)
                        {
                            ImageFormat format = GetImageFormat(originalPath);
                            pb.Image.Save(destinationPath, format);
                        }
                        else
                        {
                            result = false;
                            MessageBox.Show($"Could not save attachment (no image found): {originalPath}");
                            continue;
                        }

                        SetFileTimestamps(destinationPath);

                        if (SaveImageToDatabase(projectId, fullItemName, destinationPath, attachmentType))
                        {
                            imageCounter++;
                            savedSourcePaths.Add(originalPath);
                            savedFileNames.Add(fullItemName);
                        }
                        else
                        {
                            result = false;
                            MessageBox.Show($"Failed to save attachment record: {fullItemName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result = false;
                        MessageBox.Show($"Error saving attachment {imageCounter}: {ex.Message}");
                    }
                }
            }

            return result;
        }

        private bool SaveOcrFiles(int projectId, string deliveryDateStr, string receptionDateStr,
            string projectOrderStr, string receptionTimeStr, string companyClient, string translationType,
            string documentType, string selectedUser, ref int imageCounter, List<string> savedSourcePaths,
            List<string> savedFileNames, string projectFolder, bool allSaved)
        {
            bool result = allSaved;
            int ocrFileNumber = imageCounter;
            bool hasOcrFiles = false;

            foreach (Control control in _addFileForm.PanelDocx.Controls)
            {
                if (control is Label lbl && lbl.Tag != null)
                {
                    hasOcrFiles = true;
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
                            SetFileTimestamps(wordPath);

                            if (SaveImageToDatabase(projectId, fullWordFileName, wordPath, "WORD"))
                            {
                                ocrFileNumber++;
                                savedFileNames.Add(fullWordFileName);
                            }
                            else
                            {
                                result = false;
                                MessageBox.Show($"Failed to save Word file record: {fullWordFileName}");
                            }
                        }
                        else
                        {
                            result = false;
                            MessageBox.Show($"Word file not found: {originalPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result = false;
                        MessageBox.Show($"Error saving Word file {ocrFileNumber}: {ex.Message}");
                    }
                }
            }

            if (!hasOcrFiles) ocrFileNumber++;
            imageCounter = ocrFileNumber;

            return result;
        }

        private bool CreateEmptyWordFiles(int projectId, string deliveryDateStr, string receptionDateStr,
            string projectOrderStr, string receptionTimeStr, string companyClient, string translationType,
            string documentType, string selectedUser, ref int imageCounter, List<string> savedSourcePaths,
            List<string> savedFileNames, string projectFolder, bool allSaved)
        {
            bool result = allSaved;

            // إنشاء ملفات Word الفارغة
            string[] emptyFileNames = {
                "Google Drive.docx",
                "Traducción Preliminar.docx",
                "Informe revisión.docx",
                "Traducción revisada.docx"
            };

            string[] attachmentTypes = {
                "Google Driver",
                "Traducción Preliminar",
                "Informe revisión",
                "Traducción revisada"
            };

            for (int i = 0; i < emptyFileNames.Length; i++)
            {
                try
                {
                    string fileName = $"{deliveryDateStr}24_{receptionDateStr}{projectOrderStr}_{receptionTimeStr}_{companyClient}_{translationType}_{documentType}_{imageCounter}_{emptyFileNames[i]}";
                    string filePath = Path.Combine(projectFolder, fileName);

                    using (var fs = File.Create(filePath)) { }
                    SetFileTimestamps(filePath);

                    if (SaveImageToDatabase(projectId, fileName, filePath, attachmentTypes[i]))
                    {
                        imageCounter++;
                        savedFileNames.Add(fileName);
                    }
                    else
                    {
                        result = false;
                        MessageBox.Show($"❌ Failed to save {emptyFileNames[i]}: {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    result = false;
                    MessageBox.Show($"❌ Error creating {emptyFileNames[i]} {imageCounter}: {ex.Message}");
                }
            }

            return result;
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

        private bool IsDocumentFile(string extension)
        {
            return extension.Equals(".docx", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".doc", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsImageFile(string extension)
        {
            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".png", StringComparison.OrdinalIgnoreCase);
        }

        private ImageFormat GetImageFormat(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".jpg": case ".jpeg": return ImageFormat.Jpeg;
                case ".png": return ImageFormat.Png;
                case ".gif": return ImageFormat.Gif;
                case ".bmp": return ImageFormat.Bmp;
                case ".tiff": case ".tif": return ImageFormat.Tiff;
                default: return ImageFormat.Jpeg;
            }
        }

        private void SetFileTimestamps(string filePath)
        {
            File.SetCreationTime(filePath, DateTime.Now);
            File.SetLastWriteTime(filePath, DateTime.Now);
        }

        private bool SaveImageToDatabase(int projectId, string imageName, string imagePath, string attachmentType)
        {
            string insertImageQuery = @"INSERT INTO items (project_id, image_name, image_path, attachment_type, registration_date, last_update_date) 
                                       VALUES (@project_id, @image_name, @image_path, @attachment_type, CURRENT_DATE, CURRENT_TIMESTAMP)";

            List<SQLiteParameter> imageParameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@project_id", projectId),
                new SQLiteParameter("@image_name", imageName),
                new SQLiteParameter("@image_path", imagePath),
                new SQLiteParameter("@attachment_type", attachmentType ?? (object)DBNull.Value)
            };

            return _db.ExecuteNonQuery(insertImageQuery, imageParameters);
        }
    }
}
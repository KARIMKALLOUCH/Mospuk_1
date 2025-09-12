using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mospuk_1
{
    public class StagedProjectImageSaver
    {
        private SQLiteDatabase _db;
        private StagedProject _stagedProject;

        public StagedProjectImageSaver(SQLiteDatabase db, StagedProject stagedProject)
        {
            _db = db;
            _stagedProject = stagedProject;
        }

        public bool SaveAllStagedFiles(int projectId, string projectFolder)
        {
            bool allSaved = true;
            int filenameIndex = 0;

            // Save main images
            foreach (var pb in _stagedProject.MainImages)
            {
                if (filenameIndex < _stagedProject.GeneratedFileNames.Count)
                {
                    string filename = _stagedProject.GeneratedFileNames[filenameIndex];
                    string destinationPath = Path.Combine(projectFolder, filename);

                    if (SaveImageFile(pb, destinationPath))
                    {
                        allSaved &= SaveImageToDatabase(projectId, filename, destinationPath, null);
                    }
                    else
                    {
                        allSaved = false;
                    }

                    filenameIndex++;
                }
            }

            // Save apostille image
            if (_stagedProject.ApostilleImage != null && filenameIndex < _stagedProject.GeneratedFileNames.Count)
            {
                string filename = _stagedProject.GeneratedFileNames[filenameIndex];
                string destinationPath = Path.Combine(projectFolder, filename);

                if (SaveImageFile(_stagedProject.ApostilleImage, destinationPath))
                {
                    allSaved &= SaveImageToDatabase(projectId, filename, destinationPath, "Apostille");
                }
                else
                {
                    allSaved = false;
                }

                filenameIndex++;
            }

            // Save attachments
            foreach (var pb in _stagedProject.Attachments)
            {
                if (filenameIndex < _stagedProject.GeneratedFileNames.Count)
                {
                    string filename = _stagedProject.GeneratedFileNames[filenameIndex];
                    string destinationPath = Path.Combine(projectFolder, filename);

                    if (SaveImageFile(pb, destinationPath))
                    {
                        allSaved &= SaveImageToDatabase(projectId, filename, destinationPath, "A");
                    }
                    else
                    {
                        allSaved = false;
                    }

                    filenameIndex++;
                }
            }

            // Save OCR files
            foreach (var lbl in _stagedProject.OcrFiles)
            {
                if (filenameIndex < _stagedProject.GeneratedFileNames.Count)
                {
                    string filename = _stagedProject.GeneratedFileNames[filenameIndex];
                    string destinationPath = Path.Combine(projectFolder, filename);
                    string sourcePath = lbl.Tag.ToString();

                    try
                    {
                        if (File.Exists(sourcePath))
                        {
                            File.Copy(sourcePath, destinationPath, true);
                            SetFileTimestamps(destinationPath);
                            allSaved &= SaveImageToDatabase(projectId, filename, destinationPath, "WORD");
                        }
                        else
                        {
                            allSaved = false;
                        }
                    }
                    catch
                    {
                        allSaved = false;
                    }

                    filenameIndex++;
                }
            }

            // Create empty Word files (remaining filenames)
            string[] attachmentTypes = { "Google Driver", "Traducción Preliminar", "Informe revisión", "Traducción revisada" };
            int typeIndex = 0;

            while (filenameIndex < _stagedProject.GeneratedFileNames.Count)
            {
                string filename = _stagedProject.GeneratedFileNames[filenameIndex];
                string destinationPath = Path.Combine(projectFolder, filename);

                try
                {
                    using (var fs = File.Create(destinationPath)) { }
                    SetFileTimestamps(destinationPath);

                    string attachmentType = typeIndex < attachmentTypes.Length ? attachmentTypes[typeIndex] : "Other";
                    allSaved &= SaveImageToDatabase(projectId, filename, destinationPath, attachmentType);
                    typeIndex++;
                }
                catch
                {
                    allSaved = false;
                }

                filenameIndex++;
            }

            return allSaved;
        }

        private bool SaveImageFile(PictureBox pb, string destinationPath)
        {
            try
            {
                string sourcePath = pb.Tag.ToString();
                string extension = Path.GetExtension(sourcePath).ToLower();

                if (IsDocumentFile(extension) || extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(sourcePath, destinationPath, true);
                }
                else if (pb.Image != null)
                {
                    ImageFormat format = GetImageFormat(sourcePath);
                    pb.Image.Save(destinationPath, format);
                }
                else
                {
                    return false;
                }

                SetFileTimestamps(destinationPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsDocumentFile(string extension)
        {
            return extension.Equals(".docx", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".doc", StringComparison.OrdinalIgnoreCase);
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

using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Mospuk_1
{
    public class ProjectFileHandler
    {
        private readonly ProjectDatabaseService _dbService;
        private readonly string _tempExtractionFolder;

        public ProjectFileHandler(ProjectDatabaseService dbService, string appStartupPath)
        {
            _dbService = dbService;
            _tempExtractionFolder = Path.Combine(appStartupPath, "ExtractedFiles");
            InitializeTempFolder();
        }

        private void InitializeTempFolder()
        {
            try
            {
                if (Directory.Exists(_tempExtractionFolder))
                {
                    Directory.Delete(_tempExtractionFolder, true);
                }
                Directory.CreateDirectory(_tempExtractionFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing temp folder: {ex.Message}", "File System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string GetTempExtractionFolder() => _tempExtractionFolder;

        public void CleanTempExtractionFolder()
        {
            try
            {
                if (Directory.Exists(_tempExtractionFolder))
                {
                    Directory.Delete(_tempExtractionFolder, true);
                    Directory.CreateDirectory(_tempExtractionFolder); // Recreate for future use
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cleaning temp folder: {ex.Message}", "File System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ExtractArchive(string archivePath)
        {
            string extension = Path.GetExtension(archivePath).ToLower();
            try
            {
                if (extension == ".rar")
                {
                    using (var archive = ArchiveFactory.Open(archivePath))
                    {
                        foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                        {
                            entry.WriteToDirectory(_tempExtractionFolder, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                    }
                }
                else if (extension == ".zip")
                {
                    using (var archive = ArchiveFactory.Open(archivePath))
                    {
                        foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                        {
                            entry.WriteToDirectory(_tempExtractionFolder, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                    }
                }
                else
                {
                    // For non-archive files, just copy them to the temp folder
                    File.Copy(archivePath, Path.Combine(_tempExtractionFolder, Path.GetFileName(archivePath)), true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing archive '{Path.GetFileName(archivePath)}':\n{ex.Message}",
                                "Archive Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string CopyFileToTemp(string filePath)
        {
            try
            {
                string destPath = Path.Combine(_tempExtractionFolder, Path.GetFileName(filePath));
                File.Copy(filePath, destPath, true);
                return destPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying file to temp folder: {ex.Message}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public (bool success, List<string> savedSourcePaths, List<string> savedFileNames) SaveProjectItems(
            int projectId,
            List<string> mainFilePaths,
            string apostilleFilePath,
            List<string> attachmentFilePaths,
            List<string> ocrFilePaths,
            ProjectDatabaseService.ProjectData projectData,
            string selectedUser)
        {
            bool allSaved = true;
            int imageCounter = 1;
            var savedSourcePaths = new List<string>();
            var savedFileNames = new List<string>();

            string projectFolder = _dbService.GetSavedPathById("save");
            if (string.IsNullOrEmpty(projectFolder) || !Directory.Exists(projectFolder))
            {
                MessageBox.Show("Please set a valid save directory first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (false, savedSourcePaths, savedFileNames);
            }

            // Ensure the project folder exists
            if (!Directory.Exists(projectFolder))
            {
                Directory.CreateDirectory(projectFolder);
            }

            Func<string, string, string> generateFileName = (originalPath, attachmentSuffix) =>
            {
                string extension = Path.GetExtension(originalPath);
                string baseName = $"{projectData.DeliveryDate.ToString("yyyyMMdd")}24_{projectData.ReceptionDate.ToString("yyMMdd")}{projectData.ProjectOrder:D2}_{projectData.ReceptionTime.Replace(":", "")}_{projectData.CompanyClient}_{projectData.TranslationType}_{projectData.DocumentType}_{imageCounter}";

                if (imageCounter == 1 && string.IsNullOrEmpty(attachmentSuffix)) // Only for the first main document
                {
                    if (!string.IsNullOrWhiteSpace(projectData.Note))
                    {
                        string rawNote = projectData.Note.Trim();
                        string sanitizedNote = new string(rawNote.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '+' || c == '-').ToArray()).Replace(" ", "_");
                        if (sanitizedNote.Length > 30)
                            sanitizedNote = sanitizedNote.Substring(0, 30);
                        baseName += $"-------------{sanitizedNote}-------------{selectedUser}";
                    }
                    else
                    {
                        baseName += $"--------------------------{selectedUser}";
                    }
                }
                else if (!string.IsNullOrEmpty(attachmentSuffix))
                {
                    baseName += $"_{attachmentSuffix}";
                }

                return baseName + extension;
            };

            // 1. Save main documents (from flowLayoutPanel1)
            foreach (string originalPath in mainFilePaths)
            {
                string fullItemName = generateFileName(originalPath, null);
                string destinationPath = Path.Combine(projectFolder, fullItemName);

                try
                {
                    File.Copy(originalPath, destinationPath, true);
                    File.SetCreationTime(destinationPath, DateTime.Now);
                    File.SetLastWriteTime(destinationPath, DateTime.Now);

                    if (_dbService.InsertItem(projectId, fullItemName, destinationPath, "Main"))
                    {
                        imageCounter++;
                        savedSourcePaths.Add(originalPath);
                        savedFileNames.Add(fullItemName);
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
                    MessageBox.Show($"Error saving main document: {Path.GetFileName(originalPath)}\n{ex.Message}");
                }
            }

            // 2. Save Apostille image
            if (!string.IsNullOrEmpty(apostilleFilePath))
            {
                string fullItemName = generateFileName(apostilleFilePath, "Apostille");
                string destinationPath = Path.Combine(projectFolder, fullItemName);

                try
                {
                    File.Copy(apostilleFilePath, destinationPath, true);
                    File.SetCreationTime(destinationPath, DateTime.Now);
                    File.SetLastWriteTime(destinationPath, DateTime.Now);

                    if (_dbService.InsertItem(projectId, fullItemName, destinationPath, "Apostille"))
                    {
                        imageCounter++;
                        savedSourcePaths.Add(apostilleFilePath);
                        savedFileNames.Add(fullItemName);
                    }
                    else
                    {
                        allSaved = false;
                        MessageBox.Show($"Failed to save database record for Apostille: {fullItemName}");
                    }
                }
                catch (Exception ex)
                {
                    allSaved = false;
                    MessageBox.Show($"Error saving Apostille image: {Path.GetFileName(apostilleFilePath)}\n{ex.Message}");
                }
            }

            // 3. Save attachments (from flowLayoutPanel2)
            foreach (string originalPath in attachmentFilePaths)
            {
                string fullItemName = generateFileName(originalPath, "A");
                string destinationPath = Path.Combine(projectFolder, fullItemName);

                try
                {
                    File.Copy(originalPath, destinationPath, true);
                    File.SetCreationTime(destinationPath, DateTime.Now);
                    File.SetLastWriteTime(destinationPath, DateTime.Now);

                    if (_dbService.InsertItem(projectId, fullItemName, destinationPath, "Attachment"))
                    {
                        imageCounter++;
                        savedSourcePaths.Add(originalPath);
                        savedFileNames.Add(fullItemName);
                    }
                    else
                    {
                        allSaved = false;
                        MessageBox.Show($"Failed to save database record for attachment: {fullItemName}");
                    }
                }
                catch (Exception ex)
                {
                    allSaved = false;
                    MessageBox.Show($"Error saving attachment: {Path.GetFileName(originalPath)}\n{ex.Message}");
                }
            }

            // 4. Save OCR Word files (from panelDocx)
            foreach (string originalPath in ocrFilePaths)
            {
                string fullItemName = generateFileName(originalPath, "OCR");
                string destinationPath = Path.Combine(projectFolder, fullItemName);

                try
                {
                    File.Copy(originalPath, destinationPath, true);
                    File.SetCreationTime(destinationPath, DateTime.Now);
                    File.SetLastWriteTime(destinationPath, DateTime.Now);

                    if (_dbService.InsertItem(projectId, fullItemName, destinationPath, "WORD"))
                    {
                        imageCounter++;
                        savedSourcePaths.Add(originalPath);
                        savedFileNames.Add(fullItemName);
                    }
                    else
                    {
                        allSaved = false;
                        MessageBox.Show($"Failed to save database record for OCR file: {fullItemName}");
                    }
                }
                catch (Exception ex)
                {
                    allSaved = false;
                    MessageBox.Show($"Error saving OCR file: {Path.GetFileName(originalPath)}\n{ex.Message}");
                }
            }

            // 5. Create predefined Word files
            var predefinedFiles = new List<(string suffix, string attachmentType)>();
            predefinedFiles.Add(("_Google Drive", "Google Drive"));
            predefinedFiles.Add(("_Traducción Preliminar", "Traducción Preliminar"));
            predefinedFiles.Add(("_Informe revisión", "Informe revisión"));
            predefinedFiles.Add(("_Traducción revisada", "Traducción revisada"));

            foreach (var (suffix, type) in predefinedFiles)
            {
                string fullItemName = $"{projectData.DeliveryDate.ToString("yyyyMMdd")}24_{projectData.ReceptionDate.ToString("yyMMdd")}{projectData.ProjectOrder:D2}_{projectData.ReceptionTime.Replace(":", "")}_{projectData.CompanyClient}_{projectData.TranslationType}_{projectData.DocumentType}_{imageCounter}{suffix}.docx";
                string destinationPath = Path.Combine(projectFolder, fullItemName);

                try
                {
                    File.Create(destinationPath).Dispose(); // Create an empty file
                    File.SetCreationTime(destinationPath, DateTime.Now);
                    File.SetLastWriteTime(destinationPath, DateTime.Now);

                    if (_dbService.InsertItem(projectId, fullItemName, destinationPath, type))
                    {
                        imageCounter++;
                        savedFileNames.Add(fullItemName);
                    }
                    else
                    {
                        allSaved = false;
                        MessageBox.Show($"Failed to save database record for '{type}' file: {fullItemName}");
                    }
                }
                catch (Exception ex)
                {
                    allSaved = false;
                    MessageBox.Show($"Error creating '{type}' file: {fullItemName}\n{ex.Message}");
                }
            }

            return (allSaved, savedSourcePaths, savedFileNames);
        }

        public void DeleteFiles(List<string> filePaths)
        {
            foreach (string path in filePaths)
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
                    MessageBox.Show($"Could not delete file '{Path.GetFileName(path)}':\n{ex.Message}",
                                    "File Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        public void OpenFileInIsolatedDirectory(string filePath, IEnumerable<string> siblingFilePaths)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            string ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".doc" || ext == ".docx")
            {
                try
                {
                    var psi = new ProcessStartInfo() { FileName = filePath, UseShellExecute = true };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening Word file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "MospukViewer_" + Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(tempDir);

                List<string> orderedSourceFilePaths = siblingFilePaths
                    .Where(p => File.Exists(p) && !p.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) && !p.EndsWith(".doc", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                int clickedFileIndex = orderedSourceFilePaths.IndexOf(filePath);
                string tempClickedFilePath = null;

                for (int i = 0; i < orderedSourceFilePaths.Count; i++)
                {
                    string sourcePath = orderedSourceFilePaths[i];
                    string extension = Path.GetExtension(sourcePath);
                    string newFileName = $"{i:D3}_{Path.GetFileNameWithoutExtension(sourcePath)}{extension}";
                    string destPath = Path.Combine(tempDir, newFileName);
                    File.Copy(sourcePath, destPath);

                    if (i == clickedFileIndex)
                    {
                        tempClickedFilePath = destPath;
                    }
                }

                if (!string.IsNullOrEmpty(tempClickedFilePath) && File.Exists(tempClickedFilePath))
                {
                    var process = new Process();
                    process.StartInfo = new ProcessStartInfo() { FileName = tempClickedFilePath, UseShellExecute = true };
                    process.EnableRaisingEvents = true;
                    process.Exited += (s, args) =>
                    {
                        try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); }
                        catch (Exception ex) { Console.WriteLine($"Could not delete temp directory {tempDir}: {ex.Message}"); }
                    };
                    process.Start();
                }
            }
            catch (Exception ex)
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); }
                catch { }
            }
        }

        public System.Drawing.Imaging.ImageFormat GetImageFormat(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".jpg": case ".jpeg": return System.Drawing.Imaging.ImageFormat.Jpeg;
                case ".png": return System.Drawing.Imaging.ImageFormat.Png;
                case ".gif": return System.Drawing.Imaging.ImageFormat.Gif;
                case ".bmp": return System.Drawing.Imaging.ImageFormat.Bmp;
                case ".tiff": case ".tif": return System.Drawing.Imaging.ImageFormat.Tiff;
                default: return System.Drawing.Imaging.ImageFormat.Jpeg; // Default to JPEG if unknown
            }
        }
    }
}
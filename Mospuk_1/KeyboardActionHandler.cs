using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Mospuk_1
{
    public class KeyboardActionHandler
    {
        private readonly AddFile _form;
        private readonly DragDropHandler _dragDropHandler;

        public KeyboardActionHandler(AddFile form, DragDropHandler dragDropHandler)
        {
            _form = form;
            _dragDropHandler = dragDropHandler;
        }

        public void HandleKeyDown(KeyEventArgs e)
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
                HandleDelete();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                HandleEnter(e);
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

            Control targetControl = _form.ActiveControl;
            PictureBox imageApostille = _form.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
            bool isValidTarget = (targetControl is Panel || targetControl is FlowLayoutPanel || targetControl == imageApostille);
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
                    _form.SetPictureBoxContent(imageApostille, newFilePath);
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
                            BackColor = System.Drawing.Color.Transparent,
                            Margin = new Padding(5),
                            AllowDrop = true
                        };
                        pic.Click += (s, e) => { /* Placeholder for generic pic click if needed */ };
                        pic.Paint += _dragDropHandler.PictureBox_Paint_Selection;

                        _form.SetPictureBoxContent(pic, newFilePath);
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
            else if (targetControl is Panel panelTarget && panelTarget.Name == "panel1")
            {
                foreach (string path in filePathsFromClipboard)
                {
                    string newFilePath = CreateFileCopyInWorkspace(path);
                    if (newFilePath != null)
                    {
                        _form.AddImageBackToPanel1(newFilePath);
                    }
                }
                _form.ReArrangeImages();
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

        private void HandleDelete()
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

                        if (pb.Parent == _form.FlowLayoutPanel1)
                        {
                            _form.FlowLayoutPanel1.Controls.Remove(pb);
                            _dragDropHandler.selectedPictureBoxesFlow1.Remove(pb);
                        }
                        else if (pb.Parent == _form.FlowLayoutPanel2)
                        {
                            _form.FlowLayoutPanel2.Controls.Remove(pb);
                            _dragDropHandler.selectedPictureBoxesFlow2.Remove(pb);
                        }
                        else if (pb.Parent.Name == "panel1")
                        {
                            panel1NeedsRearrange = true;
                            pb.Parent.Controls.Remove(pb);
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
                        _form.ReArrangeImages();
                    }
                }
            }
        }

        private void HandleEnter(KeyEventArgs e)
        {
            if (AreAnyImagesSelected())
            {
                OpenSelectedImages();
            }
            else
            {
                // للوصول إلى الزر, يجب أن يكون إما public أو internal في AddFile.cs
                // على سبيل المثال: public Button UploadButton => btnUplaod;
                if (_form.Controls.Find("btnUplaod", true).FirstOrDefault() is Button btn)
                {
                    btn.PerformClick();
                }
            }
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private bool AreAnyImagesSelected()
        {
            return _dragDropHandler.selectedPictureBoxes.Any() ||
                   _dragDropHandler.selectedPictureBoxesFlow1.Any() ||
                   _dragDropHandler.selectedPictureBoxesFlow2.Any();
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
                // يجب أن تكون الدالة OpenFileInIsolatedDirectory عامة (public) في AddFile.cs
                // لتتمكن من استدعائها من هنا
                // _form.OpenFileInIsolatedDirectory(firstSelected); 

                // أو نقل منطق الدالة إلى هنا
                if (firstSelected?.Tag == null) return;
                _form.OpenImage_DoubleClick(firstSelected, EventArgs.Empty);
            }
        }
    }
}
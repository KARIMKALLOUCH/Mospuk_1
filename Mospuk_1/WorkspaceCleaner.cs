using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Mospuk_1
{
    public class WorkspaceCleaner
    {
        private readonly AddFile _formInstance;
        private readonly DragDropHandler _dragDropHandler;

        public WorkspaceCleaner(AddFile formInstance, DragDropHandler dragDropHandler)
        {
            _formInstance = formInstance;
            _dragDropHandler = dragDropHandler;
        }

        /// <summary>
        /// تُستدعى عند إغلاق النافذة لتنظيف الموارد والمجلد المؤقت.
        /// (نفس الكود الأصلي من AddFile_FormClosing)
        /// </summary>
        public void AddFile_FormClosing()
        {
            try
            {
                // للوصول إلى Guna DataGridView، يجب البحث عنه داخل عناصر التحكم للفورم
                var dgv = _formInstance.Controls.Find("guna2DataGridView1", true).FirstOrDefault() as DataGridView;
                if (dgv != null)
                {
                    dgv.Rows.Clear();
                }
                _dragDropHandler.ResetDragState();

                string tempFolder = Path.Combine(Application.StartupPath, "ExtractedFiles");
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
            catch { }
        }

        /// <summary>
        /// تُستدعى بعد حفظ المشروع بنجاح لمسح محتوى اللوحات والحقول.
        /// (نفس الكود الأصلي من CleanUpSavedProject)
        /// </summary>
        public void CleanUpSavedProject()
        {
            try
            {
                ClearFlowLayoutPanel(_formInstance.FlowLayoutPanel1);
                ClearFlowLayoutPanel(_formInstance.FlowLayoutPanel2);
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

        /// <summary>
        /// دالة شاملة لتنظيف مساحة العمل بالكامل.
        /// (نفس الكود الأصلي من CleanWorkspace)
        /// </summary>
        public void CleanWorkspace()
        {
            try
            {
                ClearFlowLayoutPanel(_formInstance.FlowLayoutPanel1);
                ClearFlowLayoutPanel(_formInstance.FlowLayoutPanel2);
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

        /// <summary>
        /// دالة مساعدة تقوم بإفراغ محتوى لوحة FlowLayoutPanel.
        /// (نفس الكود الأصلي من ClearFlowLayoutPanel)
        /// </summary>
        public void ClearFlowLayoutPanel(FlowLayoutPanel panel)
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

        /// <summary>
        /// دالة مساعدة تمسح الصورة والبيانات من PictureBox المخصص للختم.
        /// (نفس الكود الأصلي من ClearImageApostille)
        /// </summary>
        public void ClearImageApostille()
        {
            PictureBox imageApostille = _formInstance.Controls.Find("imageApostille", true).FirstOrDefault() as PictureBox;
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

        /// <summary>
        /// دالة مساعدة تمسح الملفات المضافة إلى لوحة panelDocx.
        /// (نفس الكود الأصلي من ClearPanelDocx)
        /// </summary>
        public void ClearPanelDocx()
        {
            List<Control> controlsToRemove = new List<Control>();
            foreach (Control control in _formInstance.PanelDocx.Controls)
            {
                if (control is Label) controlsToRemove.Add(control);
            }
            foreach (Control control in controlsToRemove)
            {
                _formInstance.PanelDocx.Controls.Remove(control);
                control.Dispose();
            }
        }

        /// <summary>
        /// دالة مساعدة تُعيد تعيين الحقول النصية والقوائم المنسدلة.
        /// (نفس الكود الأصلي من ClearFormFields)
        /// </summary>
        public void ClearFormFields()
        {
            var txtnotes = _formInstance.Controls.Find("txtnotes", true).FirstOrDefault() as TextBox;
            var comboDocumentType = _formInstance.Controls.Find("comboDocumentType", true).FirstOrDefault() as ComboBox;
            var comboTranslation = _formInstance.Controls.Find("comboTranslation", true).FirstOrDefault() as ComboBox;
            var receptionDate = _formInstance.Controls.Find("Reception_Date", true).FirstOrDefault() as DateTimePicker;
            var companyClient = _formInstance.Controls.Find("Company_Client", true).FirstOrDefault() as ComboBox;
            var panel1 = _formInstance.Controls.Find("panel1", true).FirstOrDefault() as Panel;

            if (txtnotes != null) txtnotes.Clear();
            if (comboDocumentType != null) comboDocumentType.SelectedIndex = -1;
            if (comboTranslation != null) comboTranslation.SelectedIndex = -1;
            if (receptionDate != null) receptionDate.Value = DateTime.Now;

            if (panel1 != null && companyClient != null && panel1.Controls.Count == 0)
            {
                companyClient.SelectedIndex = -1;
            }
        }
    }
}
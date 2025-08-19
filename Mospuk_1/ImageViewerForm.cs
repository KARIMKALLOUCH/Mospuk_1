using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mospuk_1
{
    public partial class ImageViewerForm : Form
    {
        private List<string> currentImagePaths;
        private int currentImageIndex;
        private PictureBox mainPictureBox;
        private Label imageInfoLabel;
        private string sourcePanel; // لتتبع المصدر (PANEL1, FAMELAYOUTPANEL1, FAMELAYOUTPANEL2)

        public ImageViewerForm()
        {
            InitializeComponent();
            InitializeViewer();
        }

        public ImageViewerForm(List<string> imagePaths, int startIndex, string panelSource)
        {
            InitializeComponent();
            InitializeViewer();

            currentImagePaths = imagePaths ?? new List<string>();
            currentImageIndex = Math.Max(0, Math.Min(startIndex, currentImagePaths.Count - 1));
            sourcePanel = panelSource;

            if (currentImagePaths.Count > 0)
            {
                LoadCurrentImage();
            }
        }

        private void InitializeViewer()
        {
            this.Text = "عارض الصور";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.WindowState = FormWindowState.Normal;
            this.KeyPreview = true;
            this.BackColor = Color.Black;

            // إنشاء PictureBox الرئيسي
            mainPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                Cursor = Cursors.Hand
            };

            // إنشاء Label لمعلومات الصورة
            imageInfoLabel = new Label
            {
                Text = "",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(128, 0, 0, 0), // شفاف جزئياً
                AutoSize = false,
                Size = new Size(300, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            // وضع Label في أسفل يسار الشاشة
            imageInfoLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            imageInfoLabel.Location = new Point(10, this.ClientSize.Height - 70);

            // إضافة الأحداث
            this.KeyDown += ImageViewerForm_KeyDown;
            mainPictureBox.Click += MainPictureBox_Click;
            this.Resize += ImageViewerForm_Resize;

            // إضافة العناصر للنافذة
            this.Controls.Add(mainPictureBox);
            this.Controls.Add(imageInfoLabel);

            // جعل Label يظهر فوق PictureBox
            imageInfoLabel.BringToFront();
        }

        private void ImageViewerForm_Resize(object sender, EventArgs e)
        {
            // إعادة وضع Label عند تغيير حجم النافذة
            if (imageInfoLabel != null)
            {
                imageInfoLabel.Location = new Point(10, this.ClientSize.Height - 70);
            }
        }

        private void LoadCurrentImage()
        {
            if (currentImagePaths == null || currentImagePaths.Count == 0 ||
                currentImageIndex < 0 || currentImageIndex >= currentImagePaths.Count)
            {
                mainPictureBox.Image = null;
                imageInfoLabel.Text = "لا توجد صور للعرض";
                this.Text = "عارض الصور";
                return;
            }

            string imagePath = currentImagePaths[currentImageIndex];

            try
            {
                if (File.Exists(imagePath))
                {
                    // تحرير الصورة السابقة من الذاكرة
                    if (mainPictureBox.Image != null)
                    {
                        mainPictureBox.Image.Dispose();
                    }

                    // تحميل الصورة الجديدة
                    using (var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        mainPictureBox.Image = Image.FromStream(fs);
                    }

                    // تحديث معلومات الصورة
                    string fileName = Path.GetFileName(imagePath);
                    string imageInfo = $"الصورة {currentImageIndex + 1} من {currentImagePaths.Count}\n{fileName}";
                    imageInfoLabel.Text = imageInfo;

                    // تحديث عنوان النافذة
                    this.Text = $"عارض الصور - {fileName} - ({sourcePanel})";
                }
                else
                {
                    mainPictureBox.Image = null;
                    imageInfoLabel.Text = "الملف غير موجود";
                    this.Text = "عارض الصور - ملف غير موجود";
                }
            }
            catch (Exception ex)
            {
                mainPictureBox.Image = null;
                imageInfoLabel.Text = $"خطأ في تحميل الصورة:\n{ex.Message}";
                this.Text = "عارض الصور - خطأ";
                MessageBox.Show($"خطأ في تحميل الصورة: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImageViewerForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Right:
                case Keys.D:
                    NextImage();
                    e.Handled = true;
                    break;

                case Keys.Left:
                case Keys.A:
                    PreviousImage();
                    e.Handled = true;
                    break;

                case Keys.Escape:
                    this.Close();
                    e.Handled = true;
                    break;

                case Keys.F11:
                    ToggleFullScreen();
                    e.Handled = true;
                    break;

                case Keys.Home:
                    FirstImage();
                    e.Handled = true;
                    break;

                case Keys.End:
                    LastImage();
                    e.Handled = true;
                    break;
            }
        }

        private void MainPictureBox_Click(object sender, EventArgs e)
        {
            // يمكن إضافة وظائف أخرى هنا مثل التبديل بين الصور بالنقر
            NextImage();
        }

        private void NextImage()
        {
            if (currentImagePaths != null && currentImagePaths.Count > 1)
            {
                currentImageIndex = (currentImageIndex + 1) % currentImagePaths.Count;
                LoadCurrentImage();
            }
        }

        private void PreviousImage()
        {
            if (currentImagePaths != null && currentImagePaths.Count > 1)
            {
                currentImageIndex = (currentImageIndex - 1 + currentImagePaths.Count) % currentImagePaths.Count;
                LoadCurrentImage();
            }
        }

        private void FirstImage()
        {
            if (currentImagePaths != null && currentImagePaths.Count > 0)
            {
                currentImageIndex = 0;
                LoadCurrentImage();
            }
        }

        private void LastImage()
        {
            if (currentImagePaths != null && currentImagePaths.Count > 0)
            {
                currentImageIndex = currentImagePaths.Count - 1;
                LoadCurrentImage();
            }
        }

        private void ToggleFullScreen()
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.Sizable;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // تحرير موارد الصورة عند إغلاق النافذة
            if (mainPictureBox?.Image != null)
            {
                mainPictureBox.Image.Dispose();
            }
            base.OnFormClosed(e);
        }

        // إضافة وظائف مساعدة للحصول على معلومات العرض الحالي
        public int CurrentImageIndex => currentImageIndex;
        public int TotalImages => currentImagePaths?.Count ?? 0;
        public string CurrentImagePath => currentImagePaths != null && currentImageIndex >= 0 && currentImageIndex < currentImagePaths.Count
            ? currentImagePaths[currentImageIndex] : null;
    }
}
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Mospuk_1
{
    public partial class LoadingForm : Form
    {
        // *** إضافة ***: متغير لتخزين الفورم الأب الذي سيتم التوسيط عليه
        private Form _parentForm;

        public LoadingForm()
        {
            InitializeComponent();
            // *** إضافة ***: بعض الخصائص لجعل الفورم يبدو كنافذة تحميل حقيقية
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;
        }

        // *** تعديل ***: تعديل دالة الإظهار لتتعامل مع تغييرات الحجم والموقع
        public void ShowLoading(Form parentForm)
        {
            if (parentForm == null || parentForm.IsDisposed)
                return;

            _parentForm = parentForm;

            // ربط الأحداث: عندما يتحرك الفورم الأب أو يتغير حجمه، قم بتحديث موقع نافذة التحميل
            _parentForm.LocationChanged += ParentForm_LocationOrSizeChanged;
            _parentForm.Resize += ParentForm_LocationOrSizeChanged;

            // قم بالتوسيط في المرة الأولى
            CenterToParent();

            this.Show(parentForm); // استخدام Show(owner) لمنع المستخدم من التفاعل مع الفورم الأب
            this.BringToFront();
        }

        // *** إضافة ***: دالة جديدة لحساب وتطبيق التوسيط
        private void CenterToParent()
        {
            if (_parentForm == null || _parentForm.IsDisposed)
                return;

            // حساب النقطة المركزية بالنسبة للشاشة
            Point center = new Point(
                _parentForm.Left + (_parentForm.Width - this.Width) / 2,
                _parentForm.Top + (_parentForm.Height - this.Height) / 2
            );

            this.Location = center;
        }

        // *** إضافة ***: هذا هو معالج الأحداث الذي سيتم استدعاؤه عند تحريك أو تغيير حجم الفورم الأب
        private void ParentForm_LocationOrSizeChanged(object sender, EventArgs e)
        {
            CenterToParent();
        }


        public new void Hide()
        {
            if (!this.IsDisposed)
            {
                base.Hide();

                // *** إضافة ***: إلغاء الاشتراك في الأحداث عند الإخفاء لمنع تسرب الذاكرة
                if (_parentForm != null)
                {
                    _parentForm.LocationChanged -= ParentForm_LocationOrSizeChanged;
                    _parentForm.Resize -= ParentForm_LocationOrSizeChanged;
                    _parentForm = null; // تنظيف المرجع
                }
            }
        }
    }
}
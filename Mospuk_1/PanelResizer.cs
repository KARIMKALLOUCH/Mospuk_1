using System;
using System.Drawing;
using System.Windows.Forms;

public class PanelResizer
{
    private const int GripSize = 8; // حجم المنطقة التي يمكن للمستخدم السحب منها لتغيير الحجم
    private Control _control;      // العنصر الذي يتم تغيير حجمه
    private bool _isResizing;
    private ResizeDirection _resizeDirection;
    private Point _startMousePoint; // الموقع المطلق للماوس عند بدء السحب
    private Rectangle _originalBounds; // احتفظ بالحدود الأصلية للعنصر عند بدء تغيير الحجم

    // تحديد اتجاهات تغيير الحجم الممكنة
    private enum ResizeDirection
    {
        None, Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight
    }

    public PanelResizer(Control control)
    {
        _control = control;
    }

    public void Attach()
    {
        _control.MouseDown += Control_MouseDown;
        _control.MouseMove += Control_MouseMove;
        _control.MouseUp += Control_MouseUp;
        _control.MouseLeave += Control_MouseLeave;
    }

    public void Detach()
    {
        _control.MouseDown -= Control_MouseDown;
        _control.MouseMove -= Control_MouseMove;
        _control.MouseUp -= Control_MouseUp;
        _control.MouseLeave -= Control_MouseLeave;
        _control.Cursor = Cursors.Default;
    }

    private void Control_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _resizeDirection = GetResizeDirection(e.Location);
            if (_resizeDirection != ResizeDirection.None)
            {
                _isResizing = true;
                // تحويل موقع الماوس إلى إحداثيات الشاشة المطلقة
                _startMousePoint = _control.PointToScreen(e.Location);
                _originalBounds = _control.Bounds; // التقاط حدود العنصر عند بدء تغيير الحجم
                _control.Focus();
            }
        }
    }

    private void Control_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizing)
        {
            // حساب الفرق بين الموقع الحالي والموقع الأصلي للماوس
            Point currentMousePoint = _control.PointToScreen(e.Location);
            int deltaX = currentMousePoint.X - _startMousePoint.X;
            int deltaY = currentMousePoint.Y - _startMousePoint.Y;

            // نبدأ بالحدود الأصلية للعنصر كمرجع لحساب الحدود الجديدة
            int proposedX = _originalBounds.X;
            int proposedY = _originalBounds.Y;
            int proposedWidth = _originalBounds.Width;
            int proposedHeight = _originalBounds.Height;

            int minWidth = 50; // الحد الأدنى لعرض العنصر
            int minHeight = 50; // الحد الأدنى لارتفاع العنصر

            // 1. حساب الحدود المقترحة بناءً على اتجاه تغيير الحجم والدلتا
            switch (_resizeDirection)
            {
                case ResizeDirection.Top:
                    proposedY = _originalBounds.Y + deltaY;
                    proposedHeight = _originalBounds.Height - deltaY;
                    break;
                case ResizeDirection.Bottom:
                    proposedHeight = _originalBounds.Height + deltaY;
                    break;
                case ResizeDirection.Left:
                    proposedX = _originalBounds.X + deltaX;
                    proposedWidth = _originalBounds.Width - deltaX;
                    break;
                case ResizeDirection.Right:
                    proposedWidth = _originalBounds.Width + deltaX;
                    break;
                case ResizeDirection.TopLeft:
                    proposedX = _originalBounds.X + deltaX;
                    proposedY = _originalBounds.Y + deltaY;
                    proposedWidth = _originalBounds.Width - deltaX;
                    proposedHeight = _originalBounds.Height - deltaY;
                    break;
                case ResizeDirection.TopRight:
                    proposedY = _originalBounds.Y + deltaY;
                    proposedWidth = _originalBounds.Width + deltaX;
                    proposedHeight = _originalBounds.Height - deltaY;
                    break;
                case ResizeDirection.BottomLeft:
                    proposedX = _originalBounds.X + deltaX;
                    proposedWidth = _originalBounds.Width - deltaX;
                    proposedHeight = _originalBounds.Height + deltaY;
                    break;
                case ResizeDirection.BottomRight:
                    proposedWidth = _originalBounds.Width + deltaX;
                    proposedHeight = _originalBounds.Height + deltaY;
                    break;
            }

            // 2. تطبيق قيود الحد الأدنى للعرض والارتفاع
            if (proposedWidth < minWidth)
            {
                // إذا كان تغيير الحجم من اليسار، اضبط X للحفاظ على الحد الأدنى للعرض
                if (_resizeDirection == ResizeDirection.Left ||
                    _resizeDirection == ResizeDirection.TopLeft ||
                    _resizeDirection == ResizeDirection.BottomLeft)
                {
                    proposedX = _originalBounds.Right - minWidth;
                }
                proposedWidth = minWidth;
            }
            if (proposedHeight < minHeight)
            {
                // إذا كان تغيير الحجم من الأعلى، اضبط Y للحفاظ على الحد الأدنى للارتفاع
                if (_resizeDirection == ResizeDirection.Top ||
                    _resizeDirection == ResizeDirection.TopLeft ||
                    _resizeDirection == ResizeDirection.TopRight)
                {
                    proposedY = _originalBounds.Bottom - minHeight;
                }
                proposedHeight = minHeight;
            }

            // 3. تطبيق قيود حدود العنصر الأب (Parent)
            if (_control.Parent != null)
            {
                Rectangle parentClientBounds = _control.Parent.ClientRectangle; // منطقة العميل الأبوية

                // قم بتقييد X (الحافة اليسرى)
                if (proposedX < parentClientBounds.Left)
                {
                    proposedX = parentClientBounds.Left;
                }
                // قم بتقييد اليمين (الحافة اليمنى)
                if (proposedX + proposedWidth > parentClientBounds.Right)
                {
                    proposedWidth = parentClientBounds.Right - proposedX;
                }
                // قم بتقييد Y (الحافة العلوية)
                if (proposedY < parentClientBounds.Top)
                {
                    proposedY = parentClientBounds.Top;
                }
                // قم بتقييد الأسفل (الحافة السفلية)
                if (proposedY + proposedHeight > parentClientBounds.Bottom)
                {
                    proposedHeight = parentClientBounds.Bottom - proposedY;
                }
            }

            // التأكد مرة أخرى من الحد الأدنى للحجم بعد قيود الأب
            if (proposedWidth < minWidth) proposedWidth = minWidth;
            if (proposedHeight < minHeight) proposedHeight = minHeight;

            // 4. تطبيق قيود تداخل العناصر الشقيقة (Siblings)
            Rectangle finalBounds = new Rectangle(proposedX, proposedY, proposedWidth, proposedHeight);

            if (_control.Parent != null && _control.Parent.Controls.Count > 1)
            {
                foreach (Control sibling in _control.Parent.Controls)
                {
                    // تخطي العنصر نفسه، والعناصر غير المرئية، أو المعطلة
                    if (sibling == _control || !sibling.Visible || !sibling.Enabled)
                    {
                        continue;
                    }

                    Rectangle siblingBounds = sibling.Bounds;

                    // يهدف هذا المنطق إلى منع العنصر الذي يتم تغيير حجمه من التداخل مع الأشقاء
                    switch (_resizeDirection)
                    {
                        case ResizeDirection.Top:
                        case ResizeDirection.TopLeft:
                        case ResizeDirection.TopRight:
                            // إذا كان تغيير الحجم للأعلى، وهناك شقيق في الأعلى، توقف عند حافته السفلية
                            if (finalBounds.IntersectsWith(siblingBounds) &&
                                finalBounds.Top < _originalBounds.Y &&
                                siblingBounds.Bottom <= _originalBounds.Y &&
                                finalBounds.Right > siblingBounds.Left && finalBounds.Left < siblingBounds.Right)
                            {
                                int newY = siblingBounds.Bottom;
                                int newHeight = _originalBounds.Bottom - newY;
                                if (newHeight >= minHeight)
                                {
                                    finalBounds.Y = newY;
                                    finalBounds.Height = newHeight;
                                }
                            }
                            break;

                        case ResizeDirection.Bottom:
                        case ResizeDirection.BottomLeft:
                        case ResizeDirection.BottomRight:
                            // إذا كان تغيير الحجم للأسفل، وهناك شقيق في الأسفل، توقف عند حافته العلوية
                            if (finalBounds.IntersectsWith(siblingBounds) &&
                                finalBounds.Bottom > _originalBounds.Bottom &&
                                siblingBounds.Top >= _originalBounds.Bottom &&
                                finalBounds.Right > siblingBounds.Left && finalBounds.Left < siblingBounds.Right)
                            {
                                int newHeight = siblingBounds.Top - finalBounds.Y;
                                if (newHeight >= minHeight)
                                {
                                    finalBounds.Height = newHeight;
                                }
                            }
                            break;

                        case ResizeDirection.Left:

                            // إذا كان تغيير الحجم لليسار، وهناك شقيق على اليسار، توقف عند حافته اليمنى
                            if (finalBounds.IntersectsWith(siblingBounds) &&
                                finalBounds.Left < _originalBounds.X &&
                                siblingBounds.Right <= _originalBounds.X &&
                                finalBounds.Bottom > siblingBounds.Top && finalBounds.Top < siblingBounds.Bottom)
                            {
                                int newX = siblingBounds.Right;
                                int newWidth = _originalBounds.Right - newX;
                                if (newWidth >= minWidth)
                                {
                                    finalBounds.X = newX;
                                    finalBounds.Width = newWidth;
                                }
                            }
                            break;

                        case ResizeDirection.Right:

                            // إذا كان تغيير الحجم لليمين، وهناك شقيق على اليمين، توقف عند حافته اليسرى
                            if (finalBounds.IntersectsWith(siblingBounds) &&
                                finalBounds.Right > _originalBounds.Right &&
                                siblingBounds.Left >= _originalBounds.Right &&
                                finalBounds.Bottom > siblingBounds.Top && finalBounds.Top < siblingBounds.Bottom)
                            {
                                int newWidth = siblingBounds.Left - finalBounds.X;
                                if (newWidth >= minWidth)
                                {
                                    finalBounds.Width = newWidth;
                                }
                            }
                            break;
                    }
                }
            }

            // تحقق نهائي من الحد الأدنى للأبعاد بعد جميع تعديلات التصادم
            if (finalBounds.Width < minWidth) finalBounds.Width = minWidth;
            if (finalBounds.Height < minHeight) finalBounds.Height = minHeight;

            // تطبيق الحدود النهائية المحسوبة
            _control.Bounds = finalBounds;
        }
        else
        {
            // تغيير مؤشر الماوس لإظهار إمكانية تغيير الحجم
            _control.Cursor = GetCursor(e.Location);
        }
    }
    private void Control_MouseUp(object sender, MouseEventArgs e)
    {
        _isResizing = false;
        _resizeDirection = ResizeDirection.None;
        _control.Cursor = GetCursor(e.Location); // استعادة المؤشر المناسب أو الافتراضي
    }

    private void Control_MouseLeave(object sender, EventArgs e)
    {
        if (!_isResizing)
        {
            _control.Cursor = Cursors.Default;
        }
    }

    // تحديد اتجاه تغيير الحجم بناءً على موقع الماوس
    private ResizeDirection GetResizeDirection(Point p)
    {
        // حساب مساحة شريط التمرير (افتراضيًا حوالي 17 بيكسل في Windows)
        int scrollBarWidth = SystemInformation.VerticalScrollBarWidth;

        bool onLeft = p.X < GripSize;
        bool onRight = p.X > _control.Width - GripSize - scrollBarWidth; // طرح عرض شريط التمرير
        bool onTop = p.Y < GripSize;
        bool onBottom = p.Y > _control.Height - GripSize;

        if (onLeft && onTop) return ResizeDirection.TopLeft;
        if (onRight && onTop) return ResizeDirection.TopRight;
        if (onLeft && onBottom) return ResizeDirection.BottomLeft;
        if (onRight && onBottom) return ResizeDirection.BottomRight;
        if (onTop) return ResizeDirection.Top;
        if (onBottom) return ResizeDirection.Bottom;
        if (onLeft) return ResizeDirection.Left;
        if (onRight) return ResizeDirection.Right;

        return ResizeDirection.None;
    }

    // تحديد مؤشر الماوس بناءً على اتجاه تغيير الحجم
    private Cursor GetCursor(Point p)
    {
        ResizeDirection dir = GetResizeDirection(p);
        switch (dir)
        {
            case ResizeDirection.Top:
            case ResizeDirection.Bottom:
                return Cursors.SizeNS;
            case ResizeDirection.Left:
            case ResizeDirection.Right:
                return Cursors.SizeWE;
            case ResizeDirection.TopLeft:
            case ResizeDirection.BottomRight:
                return Cursors.SizeNWSE;
            case ResizeDirection.TopRight:
            case ResizeDirection.BottomLeft:
                return Cursors.SizeNESW;
            default:
                return Cursors.Default;
        }//***************
    }
}
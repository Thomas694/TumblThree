using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace TumblThree.Presentation.Controls
{
    public class ToggleSwitch : CheckBox
    {
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }

    public class ToggleSwitchSlider : Border
    {
        private bool isDragging;
        private Point mousePosition;

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool), typeof(ToggleSwitchSlider), new FrameworkPropertyMetadata(false));

        public bool IsChecked {
            get { return (bool)GetValue(IsCheckedProperty); }
            set
            {
                SetValue(IsCheckedProperty, value);
                //if (!value) this.RenderTransform = null;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            isDragging = true;
            mousePosition = e.GetPosition(Parent as UIElement);
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            isDragging = false;
            var transform = (this.RenderTransform as TranslateTransform);
            this.ReleaseMouseCapture();
            Point relativePoint = this.TransformToAncestor((Parent as UIElement)).Transform(new Point(0, 0));
            if (relativePoint.X + 1 == Math.Round(((FrameworkElement)Parent).RenderSize.Width / 2, 0))
            {
                IsChecked = true;
                System.Threading.Thread.Sleep(250);
                this.RenderTransform = null;
                IsChecked = false;
            }
            else if (transform != null)
            {
                transform.X = 0;
            }
            e.Handled = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (!isDragging) return;
            isDragging = false;
            this.RenderTransform = null;
            this.ReleaseMouseCapture();
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var ToggleSwitchSlider = this;
            if (isDragging && ToggleSwitchSlider != null)
            {
                var currentPosition = e.GetPosition(Parent as UIElement);
                var transform = (ToggleSwitchSlider.RenderTransform as TranslateTransform);
                if (transform == null)
                {
                    transform = new TranslateTransform();
                    ToggleSwitchSlider.RenderTransform = transform;
                }
                if (currentPosition.X - mousePosition.X > 0 && Math.Round(currentPosition.X - mousePosition.X, 0) <= ((FrameworkElement)Parent).RenderSize.Width / 2)
                    transform.X = (currentPosition.X - mousePosition.X);
            }
        }
    }
}

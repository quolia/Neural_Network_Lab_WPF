using Qualia.Tools;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public class CanvasControl : Panel
    {
        private readonly VisualCollection _visuals;

        private Func<double, double> _scaleFunc = RenderSettings.Scale;

        public CanvasControl()
        {
            _visuals = new(this);
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
        }

        public void Clear()
        {
            _visuals.Clear();
        }

        private DrawingContext G()
        {
            DrawingVisual dv = new();
            _visuals.Add(dv);

            return dv.RenderOpen();
        }

        public void AddVisual(Visual visual)
        {
            _visuals.Add(visual);
        }

        protected override int VisualChildrenCount => _visuals.Count;

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _visuals.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _visuals[index];
        }

        public void DrawRectangle(Brush brush, Pen pen, ref Rect rect)
        {
            using var g = G();

            rect.X = _scaleFunc(rect.X);
            rect.Y = _scaleFunc(rect.Y);
            rect.Width = _scaleFunc(rect.Width);
            rect.Height = _scaleFunc(rect.Height);

            g.DrawRectangle(brush, pen, rect);
        }

        public void DrawText(FormattedText text, ref Point point, double angle = 0)
        {
            using var g = G();

            point.X = _scaleFunc(point.X);
            point.Y = _scaleFunc(point.Y);

            if (angle != 0)
            {
                RotateTransform rt = new()
                {
                    Angle = angle
                };
                g.PushTransform(rt);
            }

            g.DrawText(text, point);

            if (angle != 0)
            {
                g.Pop();
            }
        }

        public void DrawLine(Pen pen, ref Point point1, ref Point point2)
        {
            using var g = G();

            point1.X = _scaleFunc(point1.X);
            point1.Y = _scaleFunc(point1.Y);
            point2.X = _scaleFunc(point2.X);
            point2.Y = _scaleFunc(point2.Y);

            g.DrawLine(pen, point1, point2);
        }

        public void DrawEllipse(Brush brush, Pen pen, ref Point center, double radiusX, double radiusY)
        {
            using var g = G();

            center.X = _scaleFunc(center.X);
            center.Y = _scaleFunc(center.Y);
            radiusX = _scaleFunc(radiusX);
            radiusY = _scaleFunc(radiusY);

            g.DrawEllipse(brush, pen, center, radiusX, radiusY);
        }
    }
}

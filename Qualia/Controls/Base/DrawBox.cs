using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public class DrawBox : Panel
    {
        private VisualCollection Visuals; 

        public DrawBox()
        {
            Visuals = new VisualCollection(this);
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
        }

        public void Clear()
        {
            Visuals.Clear();
        }

        private DrawingContext G()
        {
            var dv = new DrawingVisual();
            Visuals.Add(dv);
            return dv.RenderOpen();
        }

        protected override int VisualChildrenCount
        {
            get { return Visuals.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= Visuals.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            return Visuals[index];
        }

        public void Update()
        {
            UpdateLayout();
        }

        public void DrawRectangle(Brush brush, Pen pen, Rect rect)
        {
            using (var g = G())
            {
                rect.X = Render.Scale(rect.X);
                rect.Y = Render.Scale(rect.Y);
                rect.Width = Render.Scale(rect.Width);
                rect.Height = Render.Scale(rect.Height);
                

                g.DrawRectangle(brush, pen, rect);
            }
        }

        public void DrawText(FormattedText text, Point point)
        {
            using (var g = G())
            {  
                point.X = Render.Scale(point.X);
                point.Y = Render.Scale(point.Y);
                g.DrawText(text, point);
            }
        }

        public void DrawLine(Pen pen, Point point0, Point point1)
        {
            using (var g = G())
            {
                point0.X = Render.Scale(point0.X);
                point0.Y = Render.Scale(point0.Y);
                point1.X = Render.Scale(point1.X);
                point1.Y = Render.Scale(point1.Y);

                g.DrawLine(pen, point0, point1);
            }
        }

        public void DrawEllipse(Brush brush, Pen pen, Point center, double radiusX, double radiusY)
        {
            using (var g = G())
            {
                center.X = Render.Scale(center.X);
                center.Y = Render.Scale(center.Y);
                radiusX = Render.Scale(radiusX);
                radiusY = Render.Scale(radiusY);

                g.DrawEllipse(brush, pen, center, radiusX, radiusY);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tools;

namespace Qualia.Controls
{
    public class QPresenter : Panel
    {
        private VisualCollection Visuals;

        public Func<double, double> Scale = Render.Scale;

        public QPresenter()
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

        public void AddVisual(Visual visual)
        {
            Visuals.Add(visual);
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
            //UpdateLayout();
        }

        public void DrawRectangle(Brush brush, Pen pen, Rect rect)
        {
            using (var g = G())
            {
                rect.X = Scale(rect.X);
                rect.Y = Scale(rect.Y);
                rect.Width = Scale(rect.Width);
                rect.Height = Scale(rect.Height);
                
                g.DrawRectangle(brush, pen, rect);
            }
        }

        public void DrawText(FormattedText text, Point point, double angle = 0)
        {
            using (var g = G())
            {
                point.X = Scale(point.X);
                point.Y = Scale(point.Y);

                if (angle != 0)
                {
                    RotateTransform rt = new RotateTransform();
                    rt.Angle = angle;
                    g.PushTransform(rt);
                }

                g.DrawText(text, point);

                if (angle != 0)
                {
                    g.Pop();
                }
            }
        }

        public void DrawLine(Pen pen, Point point0, Point point1)
        {
            using (var g = G())
            {
                point0.X = Scale(point0.X);
                point0.Y = Scale(point0.Y);
                point1.X = Scale(point1.X);
                point1.Y = Scale(point1.Y);

                g.DrawLine(pen, point0, point1);
            }
        }

        public void DrawEllipse(Brush brush, Pen pen, Point center, double radiusX, double radiusY)
        {
            using (var g = G())
            {
                center.X = Scale(center.X);
                center.Y = Scale(center.Y);
                radiusX = Scale(radiusX);
                radiusY = Scale(radiusY);

                g.DrawEllipse(brush, pen, center, radiusX, radiusY);
            }
        }

        public Image GetImage(double width = 0, double height = 0)
        {
            double w = width == 0 ? SystemParameters.PrimaryScreenWidth : width ;
            double h = height == 0 ? SystemParameters.PrimaryScreenHeight : height ;

            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)w, (int)h, Render.Dpi, Render.Dpi, PixelFormats.Pbgra32);

           Measure(RenderSize);
           Arrange(new Rect(RenderSize)); 

            foreach (var visual in Visuals)
            {               
                bitmap.Render(visual);
            }
            bitmap.Freeze();

            var image = new Image();
            image.Source = bitmap;
            return image;
        }
    }
}

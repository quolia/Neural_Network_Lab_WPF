using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public class DrawBox : FrameworkElement
    {
        private VisualCollection _children; 

        public DrawBox()
        {
            _children = new VisualCollection(this);
            /*
            DrawingVisual drawingVisual = new DrawingVisual();

            // Retrieve the DrawingContext in order to create new drawing content.
            DrawingContext drawingContext = drawingVisual.RenderOpen();

            // Create a rectangle and draw it in the DrawingContext.
            Rect rect = new Rect(new System.Windows.Point(160, 100), new System.Windows.Size(320, 80));
            drawingContext.DrawRectangle(System.Windows.Media.Brushes.LightBlue, (System.Windows.Media.Pen)null, rect);

            // Persist the drawing content.
            drawingContext.Close();
            */
            //return drawingVisual;
        }

        public DrawingContext G
        {
            get
            {
                var drawingVisual = new DrawingVisual();
                _children.Add(drawingVisual);
                var drawingContext = drawingVisual.RenderOpen();

                Extension.Dispatch(this, () => drawingContext.Close());
                return drawingContext;
            }
        }

        // Provide a required override for the VisualChildrenCount property.
        protected override int VisualChildrenCount
        {
            get { return _children.Count; }
        }

        // Provide a required override for the GetVisualChild method.
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _children.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            return _children[index];
        }

    }
}

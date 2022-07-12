using Qualia.Tools;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    public partial class SelectableFunctionControl : UserControl, ISelectableItem
    {
        const double AXIS_OFFSET = 6;

        private readonly Typeface _font = new(new("Tahoma"),
                                              FontStyles.Normal,
                                              FontWeights.Bold,
                                              FontStretches.Normal);

        private readonly Pen _penBlack = Draw.GetPen(in ColorsX.Black);
        private readonly Pen _penLightGray = Draw.GetPen(in ColorsX.LightGray);

        public string Text { get; set; }
        public string Value { get; set; }

        public Control Control => this;

        //private Func<object, double> value;
        //private Color _color;

        public SelectableFunctionControl()
        {
            InitializeComponent();
        }

        public SelectableFunctionControl(string name)
        {
            InitializeComponent();

            Text = name;
            Value = name;
        }

        public SelectableFunctionControl(Func<object, double> value, in Color red)
        {
            //this.value = value;
            //this.red = red;
        }

        public void DrawFunction(Func<double, double> func, in Color color)
        {
            double step = 40;
            double yc = CtlCanvas.Height / 2;
            double xc = CtlCanvas.Width / 2;

            for (double x = -2.5; x <= 2.5; x += 0.1)
            {
                double y = func(x);
                CtlCanvas.DrawEllipse(Draw.GetBrush(in color),
                                      Draw.GetPen(in color),
                                      ref Points.Get(xc + x * step, yc - y * step), 1,
                                      1);
            }
        }

        public void DrawBase()
        {
            CtlCanvas.Clear();

            double step = 20;
            double yc = CtlCanvas.Height / 2;
            double xc = CtlCanvas.Width / 2;

            const int SIZE = 10;

            for (int n = -SIZE / 2; n <= SIZE / 2; ++n)
            {
                // Vertical primary.
                CtlCanvas.DrawLineVertical(_penLightGray,
                                           ref Points.Get(xc + step * n, yc - SIZE / 2 * step),
                                           SIZE * step);

                // Vertical secondary.
                if (n % 2 == 0)
                {
                    CtlCanvas.DrawLineVertical(_penBlack,
                                               ref Points.Get(xc + step * n, yc - AXIS_OFFSET / 2),
                                               AXIS_OFFSET);
                }

                // Horizontal primary.
                CtlCanvas.DrawLineHorizontal(_penLightGray,
                                             ref Points.Get(xc - SIZE / 2 * step, yc + step * n),
                                             SIZE * step);

                // Horizontal secondary.
                if (n % 2 == 0)
                {
                    CtlCanvas.DrawLineHorizontal(_penBlack,
                                                ref Points.Get(xc - AXIS_OFFSET / 2, yc + step * n),
                                                AXIS_OFFSET);
                }
            }

            // Center.

            CtlCanvas.DrawLineVertical(_penBlack,
                                       ref Points.Get(xc, yc - SIZE / 2 * step),
                                       SIZE * step);

            CtlCanvas.DrawLineHorizontal(_penBlack,
                                         ref Points.Get(xc - SIZE / 2 * step, yc),
                                         SIZE * step);
        }
    }
}

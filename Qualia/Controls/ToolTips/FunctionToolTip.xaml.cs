using Qualia.Tools;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    public partial class FunctionPresenter : UserControl, ISelectableItem
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

        public FunctionPresenter()
        {
            InitializeComponent();
        }

        public FunctionPresenter(string name)
        {
            InitializeComponent();

            Text = name;
            Value = name;
        }

        public void DrawFunction(Func<double, double> func, in Color color)
        {
            double step = 40;
            double yc = CtlCanvas.Height / 2;
            double xc = CtlCanvas.Width / 2;

            for (double x = -2.5; x <= 2.5; x += 0.08)
            {
                double y = func(x);

                var render_x = xc + x * step;
                var render_y = yc - y * step;

                if (render_y < 0 || render_y > CtlCanvas.Height)
                {
                    continue;
                }

                CtlCanvas.DrawEllipse(Draw.GetBrush(in color),
                                      Draw.GetPen(in color),
                                      ref Points.Get(render_x, render_y), 1,
                                      1);
            }
        }

        public void DrawBase()
        {
            const int SIZE_X = 10;
            const int SIZE_Y = 6;

            const double STEP = 20;

            CtlCanvas.Clear();
            CtlCanvas.Height = SIZE_Y * STEP;

            double yc = CtlCanvas.Height / 2;
            double xc = CtlCanvas.Width / 2;

            for (int yn = -SIZE_Y / 2; yn <= SIZE_Y / 2; ++yn)
            {
                // Horizontal primary.
                CtlCanvas.DrawLineHorizontal(_penLightGray,
                                             ref Points.Get(xc - SIZE_X / 2 * STEP, yc + STEP * yn),
                                             SIZE_X * STEP);

                // Horizontal secondary.
                if (yn % 2 == 0)
                {
                    CtlCanvas.DrawLineHorizontal(_penBlack,
                                                ref Points.Get(xc - AXIS_OFFSET / 2, yc + STEP * yn),
                                                AXIS_OFFSET);
                }
            }

            for (int xn = -SIZE_X / 2; xn <= SIZE_X / 2; ++xn)
            {
                // Vertical primary.
                CtlCanvas.DrawLineVertical(_penLightGray,
                                           ref Points.Get(xc + STEP * xn, yc - SIZE_Y / 2 * STEP),
                                           SIZE_Y * STEP);

                // Vertical secondary.
                if (xn % 2 == 0)
                {
                    CtlCanvas.DrawLineVertical(_penBlack,
                                               ref Points.Get(xc + STEP * xn, yc - AXIS_OFFSET / 2),
                                               AXIS_OFFSET);
                }
            }

            // Center.

            CtlCanvas.DrawLineVertical(_penBlack,
                                       ref Points.Get(xc, yc - SIZE_Y / 2 * STEP),
                                       SIZE_Y * STEP);

            CtlCanvas.DrawLineHorizontal(_penBlack,
                                         ref Points.Get(xc - SIZE_X / 2 * STEP, yc),
                                         SIZE_X * STEP);
        }
    }
}

using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Qualia.Controls
{
    public partial class FunctionToolTip : UserControl
    {
        const double AXIS_OFFSET = 6;

        private readonly Typeface _font = new(new("Tahoma"),
                                              FontStyles.Normal,
                                              FontWeights.Bold,
                                              FontStretches.Normal);

        private readonly Pen _penBlack = Draw.GetPen(in ColorsX.Black);
        private readonly Pen _penLightGray = Draw.GetPen(in ColorsX.LightGray);

        public FunctionToolTip()
        {
            InitializeComponent();

            Loaded += FunctionToolTip_OnLoaded;
        }

        private void FunctionToolTip_OnLoaded(object sender, RoutedEventArgs e)
        {
            DrawBase();
        }

        public void DrawBase()
        {
            CtlCanvas.Clear();

            double step = (ActualWidth - AXIS_OFFSET) / 10;
            double y = ActualHeight - AXIS_OFFSET - AXIS_OFFSET / 2;
            double x;

            for (x = 1; x < 10; ++x)
            {
                CtlCanvas.DrawLine(_penLightGray,
                                        ref Points.Get((float)(AXIS_OFFSET + step * x),
                                                       (float)y),
                                        ref Points.Get((float)(AXIS_OFFSET + step * x),
                                                       0));

                CtlCanvas.DrawLine(_penBlack,
                                        ref Points.Get((float)(AXIS_OFFSET + step * x),
                                                       (float)(y - ActualHeight / 2 + AXIS_OFFSET / 2)),
                                        ref Points.Get((float)(AXIS_OFFSET + step * x),
                                                       (float)(y - ActualHeight / 2 + AXIS_OFFSET * 1.5)));
            }

            step = (ActualHeight - AXIS_OFFSET) / 10;
            x = AXIS_OFFSET / 2;

            for (y = 1; y < 10; ++y)
            {
                CtlCanvas.DrawLine(_penLightGray,
                                       ref Points.Get((float)x,
                                                      (float)(ActualHeight - AXIS_OFFSET - step * y)),
                                       ref Points.Get(ActualWidth,
                                                      (float)(ActualHeight - AXIS_OFFSET - step * y)));

                CtlCanvas.DrawLine(_penBlack,
                                       ref Points.Get((float)x + ActualWidth / 2 - AXIS_OFFSET / 2,
                                                      (float)(ActualHeight - AXIS_OFFSET - step * y)),
                                       ref Points.Get((float)(x + AXIS_OFFSET / 2 + ActualWidth / 2),
                                                      (float)(ActualHeight - AXIS_OFFSET - step * y)));
            }

            CtlCanvas.DrawLine(_penBlack,
                                   ref Points.Get(AXIS_OFFSET / 2 + ActualWidth / 2, 0),
                                   ref Points.Get(AXIS_OFFSET / 2 + ActualWidth / 2, ActualHeight - AXIS_OFFSET * 1.5));

            CtlCanvas.DrawLine(_penBlack,
                                   ref Points.Get(0, ActualHeight / 2 - AXIS_OFFSET / 2),
                                   ref Points.Get(ActualWidth, ActualHeight / 2 - AXIS_OFFSET / 2));
        }
    }
}

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
using Tools;

namespace Qualia.Controls
{
    public partial class MatrixPresenter : UserControl
    {
        Typeface Font;

        public MatrixPresenter()
        {
            InitializeComponent();
            Font = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        }

        public void Draw(List<NetworkDataModel> models, NetworkDataModel selectedModel)
        {
            int size = 9;

            CtlPresenter.Clear();

            long goodMax = 1;
            long badMax = 1;
            long axisOffset = 12;
            long bound = 60;

            var matrix = selectedModel.ErrorMatrix;

            for (int y = 0; y < matrix.Output.Length; ++y)
            {
                for (int x = 0; x < matrix.Input.Length; ++x)
                {
                    if (x == y)
                    {
                        goodMax = Math.Max(goodMax, matrix.Matrix[x, y]);
                    }
                    else
                    {
                        badMax = Math.Max(badMax, matrix.Matrix[x, y]);
                    }
                }
            }

            for (int y = 0; y < matrix.Output.Length; ++y)
            {
                for (int x = 0; x < matrix.Input.Length; ++x)
                {
                    var value = (double)matrix.Matrix[y, x] / (double)(x == y ? goodMax : badMax);
                    var color = Tools.Draw.GetColorDradient(Colors.LightGray, x == y ? Colors.Green : Colors.Red, 255, value);
                    var brush = Tools.Draw.GetBrush(color);
                    var pen = Tools.Draw.GetPen(Colors.Silver);
                    CtlPresenter.DrawRectangle(brush, pen, new Rect(axisOffset + x * size, axisOffset + y * size, size, size));
                }
            }

            long outputMax = Math.Max(matrix.Output.Max(), 1);
            for (int x = 0; x < matrix.Output.Length; ++x)
            {
                var color = Tools.Draw.GetColorDradient(Colors.White, matrix.Output[x] > matrix.Input[x] ? Colors.Red : matrix.Output[x] < matrix.Input[x] ? Colors.Blue : Colors.Green, 100, (double)matrix.Output[x] / (double)outputMax);
                var brush = Tools.Draw.GetBrush(color);
                var pen = Tools.Draw.GetPen(Colors.Silver);

                CtlPresenter.DrawRectangle(brush, pen, new Rect(axisOffset + x * size, 2 + axisOffset + matrix.Input.Length * size, size, (int)(bound * (double)matrix.Output[x] / (double)outputMax)));
            }

            long inputMax = Math.Max(matrix.Input.Max(), 1);
            for (int y = 0; y < matrix.Input.Length; ++y)
            {
                var color = Tools.Draw.GetColorDradient(Colors.White, Colors.Green, 100, (double)matrix.Input[y] / (double)inputMax);
                var brush = Tools.Draw.GetBrush(color);
                var pen = Tools.Draw.GetPen(Colors.Silver);
                CtlPresenter.DrawRectangle(brush, pen, new Rect(2 + axisOffset + matrix.Output.Length * size, axisOffset + y * size, (int)(bound * (double)matrix.Input[y] / (double)inputMax), size));
            }

            var text = new FormattedText("Output", Culture.Current, FlowDirection.LeftToRight, Font, 10, Brushes.Black, Render.PixelsPerDip);
            CtlPresenter.DrawText(text, new Point(axisOffset + (matrix.Output.Length * size - text.Width) / 2, axisOffset - text.Height - 1));
            text = new FormattedText("Input", Culture.Current, FlowDirection.LeftToRight, Font, 10, Brushes.Black, Render.PixelsPerDip);
            CtlPresenter.DrawText(text, new Point(-axisOffset - (matrix.Input.Length * size - text.Width) / 1, axisOffset - text.Height - 1), -90);

            CtlPresenter.Update();
        }
    }
}

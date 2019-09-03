using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        static Typeface Font = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        FormattedText FmtOutput = new FormattedText("Output", Culture.Current, FlowDirection.LeftToRight, Font, 10, Brushes.Black, Render.PixelsPerDip);
        FormattedText FmtInput = new FormattedText("Input", Culture.Current, FlowDirection.LeftToRight, Font, 10, Brushes.Black, Render.PixelsPerDip);

        Dictionary<string, FormattedText> ClassesFmtText = new Dictionary<string, FormattedText>();

        Pen PenSilver = Tools.Draw.GetPen(Colors.Silver);

        public MatrixPresenter()
        {
            InitializeComponent();
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
        }

        List<string> Classes;

        bool IsClassesChanged(List<string> classes)
        {
            if (Classes == null || Classes.Count != classes.Count)
            {
                return true;
            }

            for (int c = 0; c < classes.Count; ++c)
            {
                if (classes[c] != Classes[c])
                {
                    return true; ;
                }
            }

            return false;
        }

        void InitClassesFmtText(List<string> classes)
        {
            ClassesFmtText.Clear();
            for (int i = 0; i < classes.Count; ++i)
            {
                ClassesFmtText[classes[i]] = new FormattedText(classes[i], Culture.Current, FlowDirection.LeftToRight, Font, 7, Brushes.Black, Render.PixelsPerDip);
            }
        }

        public void DrawBase(ErrorMatrix matrix)
        {
            CtlBase.Clear();

            int size = 9;
            long axisOffset = 12;
 
            for (int y = 0; y < matrix.Output.Length; ++y)
            {
                for (int x = 0; x < matrix.Input.Length; ++x)
                {
                    CtlBase.DrawRectangle(null, PenSilver, Rects.Get(axisOffset + x * size, axisOffset + y * size, size, size));
                }
            }

            for (int x = 0; x < matrix.Output.Length; ++x)
            {
                var text = ClassesFmtText[matrix.Classes[x]];
                CtlBase.DrawText(text, Points.Get(axisOffset + x * size + (size - text.Width) / 2, 1 + axisOffset + matrix.Input.Length * size));
            }

            for (int y = 0; y < matrix.Input.Length; ++y)
            {
                var text = ClassesFmtText[matrix.Classes[y]];
                CtlBase.DrawText(text, Points.Get(1 + axisOffset + matrix.Output.Length * size + (size - text.Width) / 2, axisOffset + y * size));
            }
        
            CtlBase.DrawText(FmtOutput, Points.Get(axisOffset + (matrix.Output.Length * size - FmtOutput.Width) / 2, axisOffset - FmtOutput.Height - 1));            
            CtlBase.DrawText(FmtInput, Points.Get(-axisOffset - (matrix.Input.Length * size - FmtInput.Width) / 1, axisOffset - FmtInput.Height - 1), -90);

            CtlBase.Update();
        }

        public void Draw(ErrorMatrix matrix)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (IsClassesChanged(matrix.Classes))
            {
                InitClassesFmtText(matrix.Classes);
                DrawBase(matrix);
            }

            Classes = matrix.Classes;
            CtlPresenter.Clear();
            
            int size = 9;
            long goodMax = 1;
            long badMax = 1;
            long axisOffset = 12;
            long bound = 30;

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
                    if (matrix.Matrix[y, x] > 0)
                    {
                        var value = (double)matrix.Matrix[y, x] / (double)(x == y ? goodMax : badMax);
                        var color = Tools.Draw.GetColorDradient(Colors.LightGray, x == y ? Colors.Green : Colors.Red, 255, value);
                        var brush = Tools.Draw.GetBrush(color);
                        CtlPresenter.DrawRectangle(brush, PenSilver, Rects.Get(axisOffset + x * size, axisOffset + y * size, size, size));
                    }
                }
            }
        
            long outputMax = Math.Max(matrix.Output.Max(), 1);
            for (int x = 0; x < matrix.Output.Length; ++x)
            {
                var color = Tools.Draw.GetColorDradient(Colors.White, matrix.Output[x] > matrix.Input[x] ? Colors.Red : matrix.Output[x] < matrix.Input[x] ? Colors.Blue : Colors.Green, 100, (double)matrix.Output[x] / (double)outputMax);
                var brush = Tools.Draw.GetBrush(color);
                CtlPresenter.DrawRectangle(brush, PenSilver, Rects.Get(axisOffset + x * size, 10 + axisOffset + matrix.Input.Length * size, size, (int)(bound * (double)matrix.Output[x] / (double)outputMax)));
            }

            long inputMax = Math.Max(matrix.Input.Max(), 1);
            for (int y = 0; y < matrix.Input.Length; ++y)
            {
                var color = Tools.Draw.GetColorDradient(Colors.White, Colors.Green, 100, (double)matrix.Input[y] / (double)inputMax);
                var brush = Tools.Draw.GetBrush(color);
                CtlPresenter.DrawRectangle(brush, PenSilver, Rects.Get(11 + axisOffset + matrix.Output.Length * size, axisOffset + y * size, (int)(bound * (double)matrix.Input[y] / (double)inputMax), size));
            }

            CtlPresenter.Update();
        }

        public void Clear()
        {
            CtlPresenter.Clear();
        }
    }

    public class ErrorMatrix : ListNode<ErrorMatrix>
    {
        public long[] Input;
        public long[] Output;
        public long[,] Matrix;

        public List<string> Classes ;

        public ErrorMatrix(List<string> classes)
        {
            Classes = classes;

            var c = Classes.Count;

            Input = new long[c];
            Output = new long[c];
            Matrix = new long[c, c];
        }

        public long Count { get; private set; }

        public void AddData(int input, int output)
        {
            ++Input[input];
            ++Output[output];
            ++Matrix[input, output];
            ++Count;
        }

        public void ClearData()
        {
            Array.Clear(Input, 0, Input.Length);
            Array.Clear(Output, 0, Output.Length);
            
            for (int y = 0; y < Input.Length; ++y)
            {
                for (int x = 0; x < Output.Length; ++x)
                {
                    Matrix[x, y] = 0;
                }
            }
            Count = 0;
        }
    }
}

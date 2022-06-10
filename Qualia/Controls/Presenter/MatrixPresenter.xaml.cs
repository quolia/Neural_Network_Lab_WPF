using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    sealed public partial class MatrixPresenter : UserControl
    {
        private static readonly Typeface s_font = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private readonly FormattedText _textOutput = new FormattedText("Output", Culture.Current, FlowDirection.LeftToRight, s_font, 10, Brushes.Black, Render.PixelsPerDip);
        private readonly FormattedText _textInput = new FormattedText("Input", Culture.Current, FlowDirection.LeftToRight, s_font, 10, Brushes.Black, Render.PixelsPerDip);

        private readonly Dictionary<string, FormattedText> _classesFormatText = new Dictionary<string, FormattedText>();

        private readonly Pen _penSilver = Tools.Draw.GetPen(Colors.Silver);

        private List<string> _classes;

        public MatrixPresenter()
        {
            InitializeComponent();

            _penSilver.Freeze();

            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
        }

        bool IsClassesChanged(List<string> classes)
        {
            if (_classes == null || _classes.Count != classes.Count)
            {
                return true;
            }

            for (int i = 0; i < classes.Count; ++i)
            {
                if (classes[i] != _classes[i])
                {
                    return true;
                }
            }

            return false;
        }

        void InitClassesFormatText(List<string> classes)
        {
            _classesFormatText.Clear();

            for (int i = 0; i < classes.Count; ++i)
            {
                _classesFormatText[classes[i]] = new FormattedText(classes[i],
                                                                   Culture.Current,
                                                                   FlowDirection.LeftToRight,
                                                                   s_font,
                                                                   7,
                                                                   Brushes.Black,
                                                                   Render.PixelsPerDip);
            }
        }

        public void DrawBase(ErrorMatrix matrix)
        {
            if (matrix is null)
            {
                throw new ArgumentNullException(nameof(matrix));
            }

            CtlBase.Clear();

            int size = 9;
            long axisOffset = 12;
 
            for (int y = 0; y < matrix.Output.Length; ++y)
            {
                for (int x = 0; x < matrix.Input.Length; ++x)
                {
                    CtlBase.DrawRectangle(null, _penSilver, ref Rects.Get(axisOffset + x * size, axisOffset + y * size, size, size));
                }
            }

            for (int x = 0; x < matrix.Output.Length; ++x)
            {
                var text = _classesFormatText[matrix.Classes[x]];
                CtlBase.DrawText(text, ref Points.Get(axisOffset + x * size + (size - text.Width) / 2, 1 + axisOffset + matrix.Input.Length * size));
            }

            for (int y = 0; y < matrix.Input.Length; ++y)
            {
                var text = _classesFormatText[matrix.Classes[y]];
                CtlBase.DrawText(text, ref Points.Get(1 + axisOffset + matrix.Output.Length * size + (size - text.Width) / 2, axisOffset + y * size));
            }
        
            CtlBase.DrawText(_textOutput, ref Points.Get(axisOffset + (matrix.Output.Length * size - _textOutput.Width) / 2, axisOffset - _textOutput.Height - 1));
            CtlBase.DrawText(_textInput, ref Points.Get(-axisOffset - (matrix.Input.Length * size + _textInput.Width) / 2, axisOffset - _textInput.Height - 1), -90);

            CtlBase.Update();
        }

        public void Draw(ErrorMatrix matrix)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (matrix is null)
            {
                throw new ArgumentNullException(nameof(matrix));
            }

            if (IsClassesChanged(matrix.Classes))
            {
                InitClassesFormatText(matrix.Classes);
                DrawBase(matrix);
            }

            _classes = matrix.Classes;
            CtlPresenter.Clear();
            
            int size = 9;
            long goodMax = 1;
            long badMax = 1;
            long axisOffset = 12;
            long bound = 30;

            
            for (int x = 0; x < matrix.Input.Length; ++x)
            {
                for (int y = 0; y < matrix.Output.Length; ++y)
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
                        var brush = Tools.Draw.GetBrush(in color);

                        CtlPresenter.DrawRectangle(brush, _penSilver, ref Rects.Get(axisOffset + x * size, axisOffset + y * size, size, size));
                    }
                }
            }
        
            long outputMax = matrix.MaxOutput();
            for (int x = 0; x < matrix.Output.Length; ++x)
            {
                var color = Tools.Draw.GetColorDradient(Colors.White, matrix.Output[x] > matrix.Input[x] ? Colors.Red : matrix.Output[x] < matrix.Input[x] ? Colors.Blue : Colors.Green, 100, (double)matrix.Output[x] / (double)outputMax);
                var brush = Tools.Draw.GetBrush(in color);
                CtlPresenter.DrawRectangle(brush, _penSilver, ref Rects.Get(axisOffset + x * size, 10 + axisOffset + matrix.Input.Length * size, size, (int)(bound * (double)matrix.Output[x] / (double)outputMax)));
            }

            long inputMax = matrix.MaxInput();
            for (int y = 0; y < matrix.Input.Length; ++y)
            {
                var color = Tools.Draw.GetColorDradient(Colors.White, Colors.Green, 100, (double)matrix.Input[y] / (double)inputMax);
                var brush = Tools.Draw.GetBrush(in color);
                CtlPresenter.DrawRectangle(brush, _penSilver, ref Rects.Get(11 + axisOffset + matrix.Output.Length * size, axisOffset + y * size, (int)(bound * (double)matrix.Input[y] / (double)inputMax), size));
            }

            CtlPresenter.Update();
        }

        public void Clear()
        {
            CtlPresenter.Clear();
        }
    }

    sealed public class ErrorMatrix : ListXNode<ErrorMatrix>
    {
        public long[] Input;
        public long[] Output;
        public long[,] Matrix;

        public List<string> Classes;

        public long Count { get; private set; }

        public ErrorMatrix(List<string> classes)
        {
            Classes = classes ?? throw new ArgumentNullException(nameof(classes));
            var count = Classes.Count;

            Input = new long[count];
            Output = new long[count];
            Matrix = new long[count, count];
        }

        public void AddData(int input, int output)
        {
            ++Input[input];
            ++Output[output];
            ++Matrix[input, output];
            ++Count;
        }

        public long MaxInput()
        {
            long max = 0;
            for (int i = 0; i < Input.Length; ++i)
            {
                if (Input[i] > max)
                {
                    max = Input[i];
                }
            }

            return Math.Max(max, 1);
        }

        public long MaxOutput()
        {
            long max = 0;
            for (int i = 0; i < Output.Length; ++i)
            {
                if (Output[i] > max)
                {
                    max = Output[i];
                }
            }

            return Math.Max(max, 1);
        }

        public void ClearData()
        {
            Array.Clear(Input, 0, Input.Length);
            Array.Clear(Output, 0, Output.Length);
            
            for (int x = 0; x < Output.Length; ++x)
            {
                for (int y = 0; y < Input.Length; ++y)
                {
                    Matrix[x, y] = 0;
                }
            }

            Count = 0;
        }
    }
}

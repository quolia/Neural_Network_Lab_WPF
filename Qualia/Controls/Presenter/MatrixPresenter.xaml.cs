using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Qualia.Controls.Base;
using Qualia.Tools;

namespace Qualia.Controls.Presenter;

public sealed partial class MatrixPresenter : BaseUserControl
{
    private const int POINT_SIZE = 9;
    private const int AXIS_OFFSET = 12;
    private const int BOUND = 30;

    private static readonly Typeface s_font = new(new FontFamily("Tahoma"),
        FontStyles.Normal,
        FontWeights.Bold,
        FontStretches.Normal);

    private readonly FormattedText _textOutput = new("Output",
        Culture.Current,
        FlowDirection.LeftToRight,
        s_font,
        10,
        Brushes.Black,
        RenderSettings.PixelsPerDip);

    private readonly FormattedText _textInput = new("Input",
        Culture.Current,
        FlowDirection.LeftToRight,
        s_font,
        10,
        Brushes.Black,
        RenderSettings.PixelsPerDip);

    private readonly Dictionary<string, FormattedText> _classesFormatText = new();

    private readonly Pen _penSilver = Draw.GetPen(in ColorsX.Silver);
    private readonly Brush _correctBrush = Draw.GetBrush(ColorsX.Green);
    private readonly Brush _incorrectBrush = Draw.GetBrush(ColorsX.Red);

    private List<string> _classes;

    private double _maxWidth;

    public MatrixPresenter()
        : base(0)
    {
        InitializeComponent();

        _penSilver.Freeze();
        _correctBrush.Opacity = 0.25;
        _correctBrush.Freeze();
        _incorrectBrush.Opacity = 0.25;
        _incorrectBrush.Freeze();

        SnapsToDevicePixels = true;
        UseLayoutRounding = true;

        SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
    }

    public void DrawErrorMatrix(ErrorMatrix matrix, long lastInput, long lastOutput)
    {
        if (!IsLoaded)
        {
            return;
        }

        if (matrix == null)
        {
            throw new ArgumentNullException(nameof(matrix));
        }

        if (IsClassesChanged(matrix.OutputClasses))
        {
            InitClassesFormatText(matrix.OutputClasses);
            DrawBase(matrix);
        }

        _classes = matrix.OutputClasses;
        CtlDataCanvas.Clear();

        long goodMax = 1;
        long badMax = 1;


        for ( var x = 0; x < matrix.Input.Length; ++x )
        {
            for ( var y = 0; y < matrix.Output.Length; ++y )
            {
                if (x == y)
                {
                    goodMax = MathX.Max(goodMax, matrix.Matrix[x, y]);
                }
                else
                {
                    badMax = MathX.Max(badMax, matrix.Matrix[x, y]);
                }
            }
        }

        for ( var y = 0; y < matrix.Output.Length; ++y )
        {
            for ( var x = 0; x < matrix.Input.Length; ++x )
            {
                if (matrix.Matrix[y, x] > 0)
                {
                    var value = matrix.Matrix[y, x] / (double)(x == y ? goodMax : badMax);
                    var color = Draw.GetColorDradient(in ColorsX.LightGray,
                        x == y ? ColorsX.Green : ColorsX.Red,
                        255,
                        value);

                    var brush = Draw.GetBrush(in color);

                    CtlDataCanvas.DrawRectangle(brush, _penSilver, ref Rects.Get(AXIS_OFFSET + x * POINT_SIZE,
                        AXIS_OFFSET + y * POINT_SIZE,
                        POINT_SIZE,
                        POINT_SIZE));
                }
            }
        }

        var outputMax = matrix.MaxOutput();
        for ( var x = 0; x < matrix.Output.Length; ++x )
        {
            var color = Draw.GetColorDradient(in ColorsX.White,
                matrix.Output[x] > matrix.Input[x]
                    ? ColorsX.Red
                    : matrix.Output[x] < matrix.Input[x]
                        ? ColorsX.Blue
                        : ColorsX.Green,
                100,
                matrix.Output[x] / (double)outputMax);

            var brush = Draw.GetBrush(in color);
            CtlDataCanvas.DrawRectangle(brush,
                _penSilver,
                ref Rects.Get(AXIS_OFFSET + x * POINT_SIZE,
                    10 + AXIS_OFFSET + matrix.Input.Length * POINT_SIZE,
                    POINT_SIZE,
                    (int)(BOUND * (double)matrix.Output[x] / outputMax)));
        }

        var inputMax = matrix.MaxInput();
        for ( var y = 0; y < matrix.Input.Length; ++y )
        {
            var color = Draw.GetColorDradient(in ColorsX.White,
                in ColorsX.Green,
                100,
                (double)matrix.Input[y] / inputMax);

            var brush = Draw.GetBrush(in color);
            CtlDataCanvas.DrawRectangle(brush,
                _penSilver,
                ref Rects.Get(11 + AXIS_OFFSET + matrix.Output.Length * POINT_SIZE,
                    AXIS_OFFSET + y * POINT_SIZE,
                    (int)(BOUND * (double)matrix.Input[y] / inputMax),
                    POINT_SIZE));
        }

        DrawCross(lastInput, lastOutput);
    }

    public void Clear()
    {
        CtlDataCanvas.Clear();
    }
    
    private bool IsClassesChanged(IReadOnlyList<string> classes)
    {
        if (_classes == null || _classes.Count != classes.Count)
        {
            return true;
        }

        for ( var i = 0; i < classes.Count; ++i )
        {
            if (classes[i] != _classes[i])
            {
                return true;
            }
        }

        return false;
    }

    private void InitClassesFormatText(List<string> classes)
    {
        _classesFormatText.Clear();

        for ( var i = 0; i < classes.Count; ++i )
        {
            _classesFormatText[classes[i]] = new(classes[i],
                Culture.Current,
                FlowDirection.LeftToRight,
                s_font,
                7,
                Brushes.Black,
                RenderSettings.PixelsPerDip);
        }
    }

    private void DrawBase(ErrorMatrix matrix)
    {
        if (matrix == null)
        {
            throw new ArgumentNullException(nameof(matrix));
        }

        CtlBaseCanvas.Clear();

        const int POINTS_SIZE = 9;
        const int AXIS_OFFSET = 12;

        for ( var y = 0; y < matrix.Output.Length; ++y )
        {
            for ( var x = 0; x < matrix.Input.Length; ++x )
            {
                CtlBaseCanvas.DrawRectangle(null,
                    _penSilver,
                    ref Rects.Get(AXIS_OFFSET + x * POINTS_SIZE,
                        AXIS_OFFSET + y * POINTS_SIZE,
                        POINTS_SIZE,
                        POINTS_SIZE));
            }
        }

        _maxWidth = matrix.Output.Length * POINT_SIZE + AXIS_OFFSET + 11 + BOUND;
        Width = _maxWidth;

        for ( var x = 0; x < matrix.Output.Length; ++x )
        {
            var text = _classesFormatText[matrix.OutputClasses[x]];
            CtlBaseCanvas.DrawText(text,
                ref Points.Get(AXIS_OFFSET + x * POINTS_SIZE + (POINTS_SIZE - text.Width) / 2,
                    1 + AXIS_OFFSET + matrix.Input.Length * POINTS_SIZE));
        }

        for ( var y = 0; y < matrix.Input.Length; ++y )
        {
            var text = _classesFormatText[matrix.OutputClasses[y]];
            CtlBaseCanvas.DrawText(text,
                ref Points.Get(1 + AXIS_OFFSET + matrix.Output.Length * POINTS_SIZE + (POINTS_SIZE - text.Width) / 2,
                    AXIS_OFFSET + y * POINTS_SIZE));
        }

        var textOutputX = MathX.Max(0, (matrix.Output.Length * POINTS_SIZE - _textOutput.Width) / 2);
        CtlBaseCanvas.DrawText(_textOutput,
            ref Points.Get(AXIS_OFFSET + textOutputX,
                AXIS_OFFSET - _textOutput.Height - 1));

        var textInputX = MathX.Max(_textInput.Width,
            (matrix.Input.Length * POINTS_SIZE + _textInput.Width) / 2);

        CtlBaseCanvas.DrawText(_textInput,
            ref Points.Get(-AXIS_OFFSET - textInputX,
                AXIS_OFFSET - _textInput.Height - 1),
            -90);
    }
    
    private void DrawCross(long input, long output)
    {
        CtlDataCanvas.DrawRectangle(_correctBrush,
            _penSilver,
            ref Rects.Get(AXIS_OFFSET,
                AXIS_OFFSET + input * POINT_SIZE,
                _classes.Count * POINT_SIZE + 11,
                POINT_SIZE));

        CtlDataCanvas.DrawRectangle(input == output ? _correctBrush : _incorrectBrush,
            _penSilver,
            ref Rects.Get(AXIS_OFFSET + output * POINT_SIZE,
                AXIS_OFFSET,
                POINT_SIZE,
                _classes.Count * POINT_SIZE + 11));
    }
}

public sealed class ErrorMatrix : ListXNode<ErrorMatrix>
{
    public long[] Input;
    public long[] Output;
    public long[,] Matrix;

    public List<string> OutputClasses;

    public long Count { get; private set; }

    public ErrorMatrix(List<string> outputClasses)
    {
        OutputClasses = outputClasses;
        var count = OutputClasses.Count;

        Input = new long[count];
        Output = new long[count];
        Matrix = new long[count, count];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddData(int input, int output)
    {
        ++Input[input];
        ++Output[output];
        ++Matrix[input, output];
        ++Count;
    }

    public long MaxInput()
    {
        return MaxInArray(in Input);
    }

    public long MaxOutput()
    {
        return MaxInArray(in Output);
    }

    private long MaxInArray(in long[] array)
    {
        long max = 1;

        for (var i = 0; i < array.Length; ++i)
        {
            if (array[i] > max)
            {
                max = array[i];
            }
        }

        return max;
    }

    public void ClearData()
    {
        Array.Clear(Input, 0, Input.Length);
        Array.Clear(Output, 0, Output.Length);
        Array.Clear(Matrix, 0, Matrix.Length);
        Count = 0;
    }
}
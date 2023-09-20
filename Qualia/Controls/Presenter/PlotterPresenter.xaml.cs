using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Qualia.Controls.Base;
using Qualia.Models;
using Qualia.Tools;

namespace Qualia.Controls.Presenter;

public delegate ref Point GetPointDelegate(PlotterStatistics.PlotPointsList plotPoints,
    PlotterStatistics.PlotPoint plotPoint,
    long timeTicks);

public sealed partial class PlotterPresenter : BaseUserControl
{
    private const int AXIS_OFFSET = 6;
    private bool _isBaseRedrawNeeded;

    private readonly Typeface _font = new(new("Tahoma"),
        FontStyles.Normal,
        FontWeights.Bold,
        FontStretches.Normal);

    private readonly Typeface _fontLabels = new(new("Tahoma"),
        FontStyles.Normal,
        FontWeights.Bold,
        FontStretches.Normal);

    private readonly Pen _penBlack = Draw.GetPen(in ColorsX.Black);
    private readonly Pen _penLightGray = Draw.GetPen(in ColorsX.LightGray);


    public PlotterPresenter()
        : base(0)
    {
        InitializeComponent();

        _penBlack.Freeze();
        _penLightGray.Freeze();

        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
        SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);

        CtlDataCanvas.SizeChanged += PlotterPresenter_OnSizeChanged;
    }

    private void PlotterPresenter_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _isBaseRedrawNeeded = true;
    }

    public void OptimizePlotPointsCount(NetworkDataModel network)
    {
        while (network != null)
        {
            if (!network.IsEnabled)
            {
                network = network.Next;
                continue;
            }

            LinearOptimization(network.PlotterStatistics.PercentData, GetPointPercentData);
            LinearOptimization(network.PlotterStatistics.CostData, GetPointCostData);

            DensityOptimization(network.PlotterStatistics.PercentData, GetPointPercentData);
            DensityOptimization(network.PlotterStatistics.CostData, GetPointCostData);

            network.PlotterStatistics.CopyForRender = new(network.PlotterStatistics);

            network = network.Next;
        }
    }

    public void DrawPlot(NetworkDataModel network, NetworkDataModel selectedNetwork)
    {
        if (_isBaseRedrawNeeded)
        {
            RenderPlotter();
            _isBaseRedrawNeeded = false;
        }

        CtlDataCanvas.Clear();

        var firstNetwork = network;

        while (network != null)
        { 
            if (!network.IsEnabled)
            {
                network = network.Next;
                continue;
            }

            RenderData(network.PlotterStatistics.CopyForRender.PercentData,
                Draw.GetColor(220, network.Color),
                GetPointPercentData,
                false);

            RenderData(network.PlotterStatistics.CopyForRender.CostData,
                Draw.GetColor(150, network.Color),
                GetPointCostData,
                true);

            network = network.Next;
        }

        if (selectedNetwork == null || selectedNetwork.PlotterStatistics.PercentData.Count == 0 || !selectedNetwork.IsEnabled)
        {
            selectedNetwork = firstNetwork;
        }

        if (selectedNetwork != null && selectedNetwork.PlotterStatistics.PercentData.Count > 0)
        {
            DrawLabel(selectedNetwork.PlotterStatistics.PercentData,
                in selectedNetwork.Color);

            RenderPlotterLabels(selectedNetwork.PlotterStatistics.PercentData);
        }
    }

    private void RenderPlotter()
    {
        CtlBaseCanvas.Clear();

        var step = (ActualWidth - AXIS_OFFSET) / 10;
        var y = ActualHeight - AXIS_OFFSET - AXIS_OFFSET / 2;
        double x;

        for (x = 0; x < 11; ++x)
        {
            CtlBaseCanvas.DrawLine(_penLightGray,
                ref Points.Get(AXIS_OFFSET + step * x, y),
                ref Points.Get(AXIS_OFFSET + step * x, 0));

            CtlBaseCanvas.DrawLine(_penBlack,
                ref Points.Get(AXIS_OFFSET + step * x, y),
                ref Points.Get(AXIS_OFFSET + step * x, y + AXIS_OFFSET));
        }

        step = (ActualHeight - AXIS_OFFSET) / 10;
        x = AXIS_OFFSET / 2;

        for (y = 0; y < 11; ++y)
        {
            CtlBaseCanvas.DrawLine(_penLightGray,
                ref Points.Get(x,
                    ActualHeight - AXIS_OFFSET - step * y),
                ref Points.Get(ActualWidth,
                    ActualHeight - AXIS_OFFSET - step * y));

            CtlBaseCanvas.DrawLine(_penBlack,
                ref Points.Get(x,
                    ActualHeight - AXIS_OFFSET - step * y),
                ref Points.Get(x + AXIS_OFFSET,
                    ActualHeight - AXIS_OFFSET - step * y));
        }

        CtlBaseCanvas.DrawLine(_penBlack,
            ref Points.Get(AXIS_OFFSET, 0),
            ref Points.Get(AXIS_OFFSET, ActualHeight));

        CtlBaseCanvas.DrawLine(_penBlack,
            ref Points.Get(0, ActualHeight - AXIS_OFFSET),
            ref Points.Get(ActualWidth, ActualHeight - AXIS_OFFSET));
    }

    private void RenderPlotterLabels(PlotterStatistics.PlotPointsList pointsData)
    {
        var step_x = (ActualWidth - AXIS_OFFSET) / 10;
        var step_y = (ActualHeight - AXIS_OFFSET) / 10;

        var y = ActualHeight / 2 - AXIS_OFFSET - AXIS_OFFSET / 2;
        double x;

        // Render time-line.

        var ticks = pointsData.Last().TimeTicks - pointsData[0].TimeTicks;

        for (x = 1; x < 10; ++x)
        {
            FormattedText text = new(TimeSpan.FromTicks((long)(ticks / 10 * x))
                    .ToString(Culture.TimeFormat, Culture.Current),
                Culture.Current,
                FlowDirection.LeftToRight,
                _fontLabels,
                9,
                Draw.GetBrush(in ColorsX.Gray),
                RenderSettings.PixelsPerDip);

            CtlDataCanvas.DrawRectangle(Draw.GetBrush(Draw.GetColor(150, in ColorsX.White)),
                null,
                ref Points.Get(AXIS_OFFSET - text.Width / 2 + step_x * x - 5,
                    text.Width * 1.5 < step_x || x % 2 == 1 ? y : y - text.Height * 1.2),
                text.Width + 10,
                text.Height);

            CtlDataCanvas.DrawText(text,
                ref Points.Get(AXIS_OFFSET - text.Width / 2 + step_x * x,
                    text.Width * 1.5 < step_x || x % 2 == 1 ? y : y - text.Height * 1.2));
        }

        // Render percent-line.

        for (y = 1; y < 10; ++y)
        {
            if (y == 5)
            {
                continue;
            }

            FormattedText text = new(Converter.IntToText((long)y * 10),
                Culture.Current,
                FlowDirection.LeftToRight,
                _fontLabels,
                9,
                Draw.GetBrush(in ColorsX.Gray),
                RenderSettings.PixelsPerDip);

            CtlDataCanvas.DrawRectangle(Draw.GetBrush(Draw.GetColor(150, in ColorsX.White)),
                null,
                ref Points.Get(ActualWidth / 2 - text.Width / 2 + AXIS_OFFSET / 2,
                    ActualHeight - AXIS_OFFSET - step_y * y - text.Height / 2),
                text.Width,
                text.Height);

            CtlDataCanvas.DrawText(text, ref Points.Get(ActualWidth / 2 - text.Width / 2 + AXIS_OFFSET / 2,
                ActualHeight - AXIS_OFFSET - step_y * y - text.Height / 2));
        }
    }

    private void RenderData(PlotterStatistics.PlotPointsList pointsData,
        in Color color,
        GetPointDelegate getPoint,
        bool isRect)
    {
        if (pointsData == null || !pointsData.Any())
        {
            return;
        }

        const double RECT_SIZE = 4;
        const double CIRC_SIZE = 5;

        var pen = Draw.GetPen(in color);
            
        var firstPointData = pointsData[0];
        var lastPointData = pointsData.Last();
        var ticks = lastPointData.TimeTicks - firstPointData.TimeTicks;
        var prevPointData = firstPointData;

        foreach (var pointData in pointsData)
        {
            ref var point = ref getPoint(pointsData, pointData, ticks);

            ref var fromPoint = ref getPoint(pointsData, prevPointData, ticks);
            CtlDataCanvas.DrawLine(pen, ref fromPoint, ref point);

            if (isRect)
            {
                CtlDataCanvas.DrawRectangle(pen.Brush,
                    pen,
                    ref Rects.Get(point.X - RECT_SIZE / 2, point.Y - RECT_SIZE / 2, RECT_SIZE, RECT_SIZE));
            }
            else
            {
                CtlDataCanvas.DrawEllipse(pen.Brush,
                    pen,
                    ref Points.Get(point.X, point.Y),
                    CIRC_SIZE / 2,
                    CIRC_SIZE / 2);
            }

            prevPointData = pointData;
        }
    }

    private void DrawLabel(PlotterStatistics.PlotPointsList pointsData,
        in Color color)
    {
        FormattedText text = new(TimeSpan.FromTicks(pointsData.Last().TimeTicks - pointsData[0].TimeTicks)
                                     .ToString(Culture.TimeFormat, Culture.Current)
                                 + " / "
                                 + Converter.DoubleToText(pointsData.Last().Value, "N6", true)
                                 + " %",
            Culture.Current,
            FlowDirection.LeftToRight,
            _font,
            10,
            Draw.GetBrush(in color),
            RenderSettings.PixelsPerDip);

        CtlDataCanvas.DrawRectangle(Draw.GetBrush(Draw.GetColor(150, in ColorsX.White)),
            null,
            ref Rects.Get((ActualWidth - AXIS_OFFSET - text.Width) / 2 - 5,
                ActualHeight - AXIS_OFFSET - 20,
                text.Width + 10,
                text.Height));

        CtlDataCanvas.DrawText(text,
            ref Points.Get((ActualWidth - AXIS_OFFSET - text.Width) / 2,
                ActualHeight - AXIS_OFFSET - 20));
    }

    private ref Point GetPointPercentData(PlotterStatistics.PlotPointsList pointsData,
        PlotterStatistics.PlotPoint plotPoint,
        long ticks)
    {
        var pointData0 = pointsData[0];

        var pointX = ticks == 0
            ? AXIS_OFFSET
            : AXIS_OFFSET + (ActualWidth - AXIS_OFFSET) * (plotPoint.TimeTicks - pointData0.TimeTicks) / ticks;

        var pointY = (ActualHeight - AXIS_OFFSET) * (1 - plotPoint.Value / 100);

        return ref Points.Get(pointX, pointY);
    }

    private ref Point GetPointCostData(PlotterStatistics.PlotPointsList pointsData,
        PlotterStatistics.PlotPoint plotPoint,
        long ticks)
    {
        var pointData0 = pointsData[0];
        var pointX = ticks == 0
            ? AXIS_OFFSET
            : AXIS_OFFSET + (ActualWidth - AXIS_OFFSET) * (plotPoint.TimeTicks - pointData0.TimeTicks) / ticks;

        var pointY = (ActualHeight - AXIS_OFFSET) * (1 - MathX.Min(1, plotPoint.Value));

        return ref Points.Get(pointX, pointY);
    }

    private void LinearOptimization(PlotterStatistics.PlotPointsList pointsData,
        GetPointDelegate getPoint)
    {
        const double VANISH_AREA = 3.5;
        const int MIN_POINTS_COUNT = 10;

        while (true)
        {
            if (pointsData.Count <= MIN_POINTS_COUNT)
            {
                return;
            }

            for (var i = Rand.RandomFlat.Next() % 2; i < pointsData.Count - MIN_POINTS_COUNT; ++i)
            {
                var ticks = pointsData.Last().TimeTicks - pointsData[0].TimeTicks;

                ref var point0 = ref getPoint(pointsData, pointsData[i], ticks);
                ref var point1 = ref getPoint(pointsData, pointsData[i + 1], ticks);
                ref var point2 = ref getPoint(pointsData, pointsData[i + 2], ticks);

                if (IsSameLine(point0, point1, point2))
                {
                    pointsData.AddToRemove(pointsData[i + 1]);

                    if (pointsData.Count - pointsData.PointsToRemoveCount < MIN_POINTS_COUNT)
                    {
                        break;
                    }

                    i += 1;
                }
                else
                {
                    if ((point1.X - point0.X) < VANISH_AREA && MathX.Abs(point1.Y - point0.Y) < VANISH_AREA)
                    {
                        pointsData.AddToRemove(pointsData[i + 1]);

                        if (pointsData.Count - pointsData.PointsToRemoveCount < MIN_POINTS_COUNT)
                        {
                            break;
                        }

                        i += 2;
                    }
                }
            }

            if (pointsData.PointsToRemoveCount == 0)
            {
                return;
            }

            pointsData.CommitRemove();
        }
    }

    private static bool IsSameLine(Point p1, Point p2, Point p3)
    {
        var x1 = (int)p1.X;
        var y1 = (int)p1.Y;

        var x2 = (int)p2.X;
        var y2 = (int)p2.Y;

        var x3 = (int)p3.X;
        var y3 = (int)p3.Y;

        if (x2 - x1 == 0 || x3 - x2 == 0)
        {
            return false;
        }

        double a1 = 100 * (y2 - y1) / (x2 - x1);
        double a2 = 100 * (y3 - y2) / (x3 - x2);

        return (int)a1 == (int)a2;
    }

    private static void DensityOptimization(PlotterStatistics.PlotPointsList pointsData,
        GetPointDelegate getPoint)
    {
        const int VANISH_AREA = 6;
        const int MIN_POINTS_COUNT = 10;

        while (true)
        {
            if (pointsData.Count <= MIN_POINTS_COUNT)
            {
                return;
            }

            for (var i = Rand.RandomFlat.Next() % 3; i < pointsData.Count - MIN_POINTS_COUNT; ++i)
            {
                var ticks = pointsData.Last().TimeTicks - pointsData[0].TimeTicks;

                ref var point0 = ref getPoint(pointsData, pointsData[i], ticks);
                ref var point5 = ref getPoint(pointsData, pointsData[i + 5], ticks);

                if (MathX.Abs(point0.X - point5.X) < VANISH_AREA)
                {
                    List<Point> points = new()
                    {
                        getPoint(pointsData, pointsData[i + 1], ticks),
                        getPoint(pointsData, pointsData[i + 2], ticks),
                        getPoint(pointsData, pointsData[i + 3], ticks),
                        getPoint(pointsData, pointsData[i + 4], ticks),
                    };

                    var maxY = points.Max(p => p.Y);
                    var minY = points.Min(p => p.Y);

                    for (var n = 0; n < points.Count; ++n)
                    {
                        if (points[n].Y < maxY && points[n].Y > minY)
                        {
                            pointsData.AddToRemove(pointsData[i + n + 1]);
                        }
                    }

                    i += points.Count;
                }

                if (pointsData.Count - pointsData.PointsToRemoveCount < MIN_POINTS_COUNT)
                {
                    break;
                }
            }

            if (pointsData.PointsToRemoveCount == 0)
            {
                return;
            }

            pointsData.CommitRemove();
        }
    }

    public void Clear()
    {
        DrawPlot(null, null);
    }
}
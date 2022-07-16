using Qualia.Model;
using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Linq;

namespace Qualia.Controls
{
    public delegate ref Point GetPointDelegate(PlotterStatistics.PlotPointsList plotPoints,
                                               PlotterStatistics.PlotPoint plotPoint,
                                               long timeTicks);

    sealed public partial class PlotterPresenter : BaseUserControl
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

        public void OptimizePlotPointsCount(ListX<NetworkDataModel> networks!!)
        {
            if (!networks.Any())
            {
                throw new ArgumentException("No network models.", nameof(networks));
            }

            OptimizePlotPointsCount(networks.First);
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

        public void DrawPlot(ListX<NetworkDataModel> networks,
                             NetworkDataModel selectedNetwork)
        {
            if (_isBaseRedrawNeeded)
            {
                RenderPlotter();
                _isBaseRedrawNeeded = false;
            }

            CtlDataCanvas.Clear();

            if (networks == null)
            {
                return;
            }

            if (!networks.Any())
            {
                throw new ArgumentException("No network models.", nameof(networks));
            }

            var networkModel = networks.First;

            while (networkModel != null)
            { 
                if (!networkModel.IsEnabled)
                {
                    networkModel = networkModel.Next;
                    continue;
                }

                RenderData(networkModel.PlotterStatistics.CopyForRender.PercentData,
                           Draw.GetColor(220, networkModel.Color),
                           GetPointPercentData,
                           false);

                RenderData(networkModel.PlotterStatistics.CopyForRender.CostData,
                           Draw.GetColor(150, networkModel.Color),
                           GetPointCostData,
                           true);

                networkModel = networkModel.Next;
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

            double step = (ActualWidth - AXIS_OFFSET) / 10;
            double y = ActualHeight - AXIS_OFFSET - AXIS_OFFSET / 2;
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
            double step_x = (ActualWidth - AXIS_OFFSET) / 10;
            double step_y = (ActualHeight - AXIS_OFFSET) / 10;

            double y = ActualHeight / 2 - AXIS_OFFSET - AXIS_OFFSET / 2;
            double x;

            // Render time-line.

            long ticks = pointsData.Last().TimeTicks - pointsData[0].TimeTicks;

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

            var pen = Draw.GetPen(in color);
            
            var firstPointData = pointsData[0];
            var lastPointData = pointsData.Last();

            var ticks = lastPointData.TimeTicks - firstPointData.TimeTicks;

            Point prevPoint = new(-1000, -1000);
            var prevPointData = firstPointData;

            foreach (var pointData in pointsData)
            {
                ref var point = ref getPoint(pointsData, pointData, ticks);

                //if ((point.X - prevPoint.X) > 8 || MathX.Abs(point.Y - prevPoint.Y) > 8)// || pointData == lastPointData) // opt
                {
                    ref var fromPoint = ref getPoint(pointsData, prevPointData, ticks);
                    CtlDataCanvas.DrawLine(pen, ref fromPoint, ref point);

                    if (isRect)
                    {
                        CtlDataCanvas.DrawRectangle(pen.Brush,
                                                    pen,
                                                    ref Rects.Get(point.X - 6 / 2, point.Y - 6 / 2, 6, 6));
                    }
                    else
                    {
                        CtlDataCanvas.DrawEllipse(pen.Brush,
                                                  pen,
                                                  ref Points.Get(point.X, point.Y),
                                                  7 / 2,
                                                  7 / 2);
                    }

                    prevPointData = pointData;
                    prevPoint = point;
                }
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
            const int VANISH_AREA = 8;
            const int MIN_POINTS_COUNT = 10;

            while (true)
            {
                if (pointsData.Count <= MIN_POINTS_COUNT)
                {
                    return;
                }

                for (int i = 0; i < pointsData.Count - MIN_POINTS_COUNT; ++i)
                {
                    var ticks = pointsData.Last().TimeTicks - pointsData[0].TimeTicks;

                    ref var point0 = ref getPoint(pointsData, pointsData[i], ticks);
                    if (point0.X > ActualWidth - ActualWidth / 40)
                    {
                        //return;
                    }

                    ref var point1 = ref getPoint(pointsData, pointsData[i + 1], ticks);
                    ref var point2 = ref getPoint(pointsData, pointsData[i + 2], ticks);

                    var a1 = MathX.Abs(Angle(in point0, in point1) - Angle(in point1, in point2));
                    var a2 = MathX.Abs(Angle(in point0, in point1) - Angle(in point1, in point2));

                    var a = MathX.Max(a1, a2);

                    //if (a > Math.PI - Math.PI / 360D)
                    if (IsSameLine(point0, point1, point2))
                    {
                        pointsData.AddToRemove(pointsData[i + 1]);

                        if (pointsData.Count - pointsData.PointsToRemoveCount < MIN_POINTS_COUNT)
                        {
                            break;
                        }

                        i += 2;
                    }
                    else
                    {
                        /*
                        if (MathX.Abs(point2.X - point0.X) < VANISH_AREA && MathX.Abs(point2.Y - point0.Y) < VANISH_AREA
                            && MathX.Abs(point1.X - point0.X) < VANISH_AREA && MathX.Abs(point1.Y - point0.Y) < VANISH_AREA)
                        {
                            pointsData.AddToRemove(pointsData[i + 1]);

                            if (pointsData.Count - pointsData.PointsToRemoveCount < MIN_POINTS_COUNT)
                            {
                                break;
                            }

                            i += 3;
                        }
                        */
                        if ((point1.X - point0.X) < VANISH_AREA && MathX.Abs(point1.Y - point0.Y) < VANISH_AREA)// || pointData == lastPointData) // opt
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

        private bool IsSameLine(Point p1, Point p2, Point p3)
        {
            double m = 150 * (p2.Y - p1.Y) / (p2.X - p1.X);
            double n = 150 * (p3.Y - p1.Y) / (p3.X - p1.X);

            return (int)m == (int)n;
        }

        private void DensityOptimization(PlotterStatistics.PlotPointsList pointsData,
                                         GetPointDelegate getPoint)
        {
            const int VANISH_AREA = 7;
            const int MIN_POINTS_COUNT = 10;

            while (true)
            {
                if (pointsData.Count <= MIN_POINTS_COUNT)
                {
                    return;
                }

                for (int i = 0; i < pointsData.Count - MIN_POINTS_COUNT; ++i)
                {
                    var ticks = pointsData.Last().TimeTicks - pointsData[0].TimeTicks;

                    ref var point0 = ref getPoint(pointsData, pointsData[i], ticks);
                    if (point0.X > ActualWidth - ActualWidth / 40)
                    {
                        //return;
                    }

                    ref var point5 = ref getPoint(pointsData, pointsData[i + 5], ticks);

                    if (MathX.Abs(point0.X - point5.X) < VANISH_AREA)
                    {
                        List<Point> points = new()
                        {
                            //getPoint(pointsData, pointsData[i], ticks),
                            getPoint(pointsData, pointsData[i + 1], ticks),
                            getPoint(pointsData, pointsData[i + 2], ticks),
                            getPoint(pointsData, pointsData[i + 3], ticks),
                            getPoint(pointsData, pointsData[i + 4], ticks),
                            //getPoint(pointsData, pointsData[i + 5], ticks)
                        };

                        var maxY = points.Max(p => p.Y);
                        var minY = points.Min(p => p.Y);

                        for (int n = 0; n < points.Count; ++n)
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

        private double Angle(in Point point1, in Point point2)
        {
            return Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
        }

        public void Clear()
        {
            DrawPlot(null, null);
        }
    }
}

using Qualia.Model;
using Qualia.Tools;
using System;
using System.Windows;
using System.Windows.Media;

namespace Qualia.Controls
{
    public delegate ref Point GetPointDelegate(DynamicStatistics.PlotPointsList plotPoints,
                                               DynamicStatistics.PlotPoint plotPoint,
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

                OptimizePointsCount(network.DynamicStatistics.PercentData, GetPointPercentData);
                OptimizePointsCount(network.DynamicStatistics.CostData, GetPointCostData);

                network.DynamicStatistics.CopyForRender = new(network.DynamicStatistics);

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

                RenderData(networkModel.DynamicStatistics.CopyForRender.PercentData,
                           Draw.GetColor(220, networkModel.Color),
                           GetPointPercentData,
                           false);

                RenderData(networkModel.DynamicStatistics.CopyForRender.CostData,
                           Draw.GetColor(150, networkModel.Color),
                           GetPointCostData,
                           true);

                networkModel = networkModel.Next;
            }

            if (selectedNetwork != null && selectedNetwork.DynamicStatistics.PercentData.Count > 0)
            {
                DrawLabel(selectedNetwork.DynamicStatistics.PercentData,
                          in selectedNetwork.Color);

                RenderPlotterLabels(selectedNetwork.DynamicStatistics.PercentData);
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

        private void RenderPlotterLabels(DynamicStatistics.PlotPointsList pointsData)
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

        private void RenderData(DynamicStatistics.PlotPointsList pointsData,
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

                if ((point.X - prevPoint.X) > 10 || MathX.Abs(point.Y - prevPoint.Y) > 10 || pointData == lastPointData) // opt
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

        private void DrawLabel(DynamicStatistics.PlotPointsList pointsData,
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

        private ref Point GetPointPercentData(DynamicStatistics.PlotPointsList pointsData,
                                              DynamicStatistics.PlotPoint plotPoint,
                                              long ticks)
        {
            var pointData0 = pointsData[0];

            var pointX = ticks == 0
                         ? AXIS_OFFSET
                         : AXIS_OFFSET + (ActualWidth - AXIS_OFFSET) * (plotPoint.TimeTicks - pointData0.TimeTicks) / ticks;

            var pointY = (ActualHeight - AXIS_OFFSET) * (1 - plotPoint.Value / 100);

            return ref Points.Get(pointX, pointY);
        }

        private ref Point GetPointCostData(DynamicStatistics.PlotPointsList pointsData,
                                           DynamicStatistics.PlotPoint plotPoint,
                                           long ticks)
        {
            var pointData0 = pointsData[0];
            var pointX = ticks == 0
                         ? AXIS_OFFSET
                         : AXIS_OFFSET + (ActualWidth - AXIS_OFFSET) * (plotPoint.TimeTicks - pointData0.TimeTicks) / ticks;

            var pointY = (ActualHeight - AXIS_OFFSET) * (1 - MathX.Min(1, plotPoint.Value));

            return ref Points.Get(pointX, pointY);
        }

        private void OptimizePointsCount(DynamicStatistics.PlotPointsList pointsData,
                                         GetPointDelegate getPoint)
        {
            const int VANISH_AREA = 14;
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
                    ref var point1 = ref getPoint(pointsData, pointsData[i + 1], ticks);
                    ref var point2 = ref getPoint(pointsData, pointsData[i + 2], ticks);

                    if (MathX.Abs(Angle(in point0, in point1) - Angle(in point1, in point2)) < Math.PI / 720D)
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
                        if (MathX.Abs(point0.X - point1.X) < VANISH_AREA && MathX.Abs(point0.Y - point1.Y) < VANISH_AREA)
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    using PointFunc = Func<DynamicStatistics.PlotPoints, DynamicStatistics.PlotPoint, long, Point>;

    public partial class PlotterPresenter : UserControl
    {
        private readonly int _axisOffset = 6;
        private bool _isBaseRedrawNeeded;

        private readonly Typeface _font = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        public PlotterPresenter()
        {
            InitializeComponent();

            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);

            CtlPresenter.SizeChanged += PlotterPresenter_SizeChanged;
        }

        private void PlotterPresenter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _isBaseRedrawNeeded = true;
        }

        public void Vanish(ListX<NetworkDataModel> networkModels)
        {
            if (networkModels == null)
            {
                throw new ArgumentNullException(nameof(networkModels));
            }

            if (!networkModels.Any())
            {
                throw new ArgumentException("No network models.", nameof(networkModels));
            }

            var networkModel = networkModels[0];

            while (networkModel != null)
            {
                if (!networkModel.IsEnabled)
                {
                    networkModel = networkModel.Next;
                    continue;
                }

                Vanish(networkModel.DynamicStatistics.PercentData, GetPointPercentData);
                Vanish(networkModel.DynamicStatistics.CostData, GetPointCostData);

                networkModel.DynamicStatistics.CopyForRender = new DynamicStatistics(networkModel.DynamicStatistics);

                networkModel = networkModel.Next;
            }
        }

        public void Draw(ListX<NetworkDataModel> networkModels, NetworkDataModel selectedModel)
        {
            if (_isBaseRedrawNeeded)
            {
                DrawPlotter();
                _isBaseRedrawNeeded = false;
            }

            CtlPresenter.Clear();

            if (networkModels == null)
            {
                return;
            }

            if (!networkModels.Any())
            {
                throw new ArgumentException("No network models.", nameof(networkModels));
            }

            var networkModel = networkModels[0];

            while (networkModel != null)
            { 
                if (!networkModel.IsEnabled)
                {
                    networkModel = networkModel.Next;
                    continue;
                }

                DrawData(networkModel.DynamicStatistics.CopyForRender.PercentData,
                         Tools.Draw.GetColor(220, networkModel.Color),
                         GetPointPercentData,
                         false);

                DrawData(networkModel.DynamicStatistics.CopyForRender.CostData,
                         Tools.Draw.GetColor(150, networkModel.Color),
                         GetPointCostData,
                         true);

                networkModel = networkModel.Next;
            }

            if (selectedModel != null && selectedModel.DynamicStatistics.PercentData.Count > 0)
            {
                DrawLabel(selectedModel.DynamicStatistics.PercentData, selectedModel.Color);
            }

            CtlPresenter.Update();
        }

        private void DrawPlotter()
        {
            CtlBase.Clear();

            var penBlack = Tools.Draw.GetPen(Colors.Black);
            var penLightGray = Tools.Draw.GetPen(Colors.LightGray);

            double step = (ActualWidth - _axisOffset) / 10;
            double y = ActualHeight - _axisOffset - _axisOffset / 2;
            double x;

            for (x = 0; x < 11; ++x)
            {
                CtlBase.DrawLine(penLightGray, Points.Get((float)(_axisOffset + step * x), (float)y), Points.Get((float)(_axisOffset + step * x), 0));
                CtlBase.DrawLine(penBlack, Points.Get((float)(_axisOffset + step * x), (float)y), Points.Get((float)(_axisOffset + step * x), (float)(y + _axisOffset)));
            }

            step = (ActualHeight - _axisOffset) / 10;
            x = _axisOffset / 2;

            for (y = 0; y < 11; ++y)
            {
                CtlBase.DrawLine(penLightGray,
                                 Points.Get((float)x, (float)(ActualHeight - _axisOffset - step * y)),
                                 Points.Get(ActualWidth, (float)(ActualHeight - _axisOffset - step * y)));

                CtlBase.DrawLine(penBlack,
                                 Points.Get((float)x, (float)(ActualHeight - _axisOffset - step * y)),
                                 Points.Get((float)(x + _axisOffset), (float)(ActualHeight - _axisOffset - step * y)));
            }

            CtlBase.DrawLine(penBlack,
                             Points.Get(_axisOffset, 0),
                             Points.Get(_axisOffset, ActualHeight));

            CtlBase.DrawLine(penBlack,
                             Points.Get(0, ActualHeight - _axisOffset),
                             Points.Get(ActualWidth, ActualHeight - _axisOffset));
        }

        private void DrawData(DynamicStatistics.PlotPoints plotPoints, Color color, PointFunc func, bool isRect)
        {
            if (plotPoints == null || !plotPoints.Any())
            {
                return;
            }

            var pen = Tools.Draw.GetPen(color);
            
            var firstPointData = plotPoints[0];
            var lastPointData = plotPoints.Last();

            var ticks = lastPointData.TimeTicks - firstPointData.TimeTicks;

            var prevPoint = new Point(-1000, -1000);
            var prevPointData = firstPointData;

            foreach (var pointData in plotPoints)
            {
                var point = func(plotPoints, pointData, ticks);

                if ((point.X - prevPoint.X) > 10 || Math.Abs(point.Y - prevPoint.Y) > 10 || pointData == lastPointData) // opt
                {
                    CtlPresenter.DrawLine(pen, func(plotPoints, prevPointData, ticks), point);

                    if (isRect)
                    {
                        CtlPresenter.DrawRectangle(pen.Brush, pen, Rects.Get(point.X - 6 / 2, point.Y - 6 / 2, 6, 6));
                    }
                    else
                    {
                        CtlPresenter.DrawEllipse(pen.Brush, pen, Points.Get(point.X, point.Y), 7 / 2, 7 / 2);
                    }

                    prevPointData = pointData;
                    prevPoint = point;
                }
            }
        }

        private void DrawLabel(DynamicStatistics.PlotPoints plotPoints, Color color)
        {     
            var text = new FormattedText(TimeSpan.FromTicks(plotPoints.Last().TimeTicks - plotPoints[0].TimeTicks).ToString(Culture.TimeFormat)
                                         + " / " + Converter.DoubleToText(plotPoints.Last().Value, "N6", true) + " %",
                                         Culture.Current,
                                         FlowDirection.LeftToRight,
                                         _font,
                                         10,
                                         Tools.Draw.GetBrush(color),
                                         Render.PixelsPerDip);

            CtlPresenter.DrawRectangle(Tools.Draw.GetBrush(Tools.Draw.GetColor(150, Colors.White)),
                                       null,
                                       Rects.Get((ActualWidth - _axisOffset - text.Width) / 2 - 5,
                                                 ActualHeight - _axisOffset - 20,
                                                 text.Width + 10,
                                                 text.Height));

            CtlPresenter.DrawText(text, Points.Get((ActualWidth - _axisOffset - text.Width) / 2, ActualHeight - _axisOffset - 20));
        }

        private Point GetPointPercentData(DynamicStatistics.PlotPoints plotPoints, DynamicStatistics.PlotPoint point, long ticks)
        {
            var point0 = plotPoints[0];
            var pointX = ticks == 0 ? _axisOffset : _axisOffset + (ActualWidth - _axisOffset) * (point.TimeTicks - point0.TimeTicks) / ticks;
            var pointY = (ActualHeight - _axisOffset) * (1 - (point.Item1 / 100));

            return Points.Get((int)pointX, (int)pointY);
        }

        private Point GetPointCostData(DynamicStatistics.PlotPoints plotPoints, DynamicStatistics.PlotPoint point, long ticks)
        {
            var point0 = plotPoints[0];
            var pointX = ticks == 0 ? _axisOffset : _axisOffset + (ActualWidth - _axisOffset) * (point.TimeTicks - point0.TimeTicks) / ticks;
            var pointY = (ActualHeight - _axisOffset) * (1 - Math.Min(1, point.Value));

            return Points.Get((int)pointX, (int)pointY);
        }

        private void Vanish(DynamicStatistics.PlotPoints points, PointFunc func)
        {
            const int VANISH_AREA = 14;
            const int MIN_POINTS_COUNT = 10;

            while (true)
            {
                if (points.Count <= MIN_POINTS_COUNT)
                {
                    return;
                }

                List<DynamicStatistics.PlotPoint> pointsToRemove = null;

                for (int i = 0; i < points.Count - MIN_POINTS_COUNT/*minPointsCount*/; ++i)
                {
                    var ticks = points.Last().TimeTicks - points[0].TimeTicks;
                    var point0 = func(points, points[i], ticks);
                    var point1 = func(points, points[i + 1], ticks);
                    var point2 = func(points, points[i + 2], ticks);

                    if (Math.Abs(Angle(point0, point1) - Angle(point1, point2)) < Math.PI / 720D)
                    {
                        if (pointsToRemove == null)
                        {
                            pointsToRemove = new List<DynamicStatistics.PlotPoint>();
                        }

                        pointsToRemove.Add(points[i + 1]);

                        if (points.Count - pointsToRemove.Count < MIN_POINTS_COUNT)
                        {
                            break;
                        }

                        i += 2;
                    }
                    else
                    {
                        if (Math.Abs(point0.X - point1.X) < VANISH_AREA && Math.Abs(point0.Y - point1.Y) < VANISH_AREA)
                        {
                            if (pointsToRemove == null)
                            {
                                pointsToRemove = new List<DynamicStatistics.PlotPoint>();
                            }

                            pointsToRemove.Add(points[i + 1]);

                            if (points.Count - pointsToRemove.Count < MIN_POINTS_COUNT)
                            {
                                break;
                            }

                            i += 2;
                        }
                    }
                }

                if (pointsToRemove == null)
                {
                    return;
                }

                pointsToRemove.ForEach(p => points.Remove(p));
            }
        }

        private double Angle(Point point0, Point point1)
        {
            return Math.Atan2(point1.Y - point0.Y, point1.X - point0.X);
        }

        public void Clear()
        {
            Draw(null, null);
        }
    }
}

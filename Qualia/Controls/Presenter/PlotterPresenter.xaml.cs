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

            var ticks = lastPointData.Item2 - firstPointData.Item2;

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

        private void DrawLabel(DynamicStatistics.PlotPoints data, Color color)
        {     
            var text = new FormattedText(TimeSpan.FromTicks(data.Last().Item2 - data[0].Item2).ToString(Culture.TimeFormat)
                                         + " / " + Converter.DoubleToText(data.Last().Item1, "N6", false) + " %",
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

        private Point GetPointPercentData(DynamicStatistics.PlotPoints data, DynamicStatistics.PlotPoint point, long ticks)
        {
            var p0 = data[0];
            var px = ticks == 0 ? _axisOffset : _axisOffset + (ActualWidth - _axisOffset) * (point.Item2 - p0.Item2) / ticks;
            var py = (ActualHeight - _axisOffset) * (1 - (point.Item1 / 100));

            return Points.Get((int)px, (int)py);
        }

        private Point GetPointCostData(DynamicStatistics.PlotPoints data, DynamicStatistics.PlotPoint point, long ticks)
        {
            var p0 = data[0];
            var px = ticks == 0 ? _axisOffset : _axisOffset + (ActualWidth - _axisOffset) * (point.Item2 - p0.Item2) / ticks;
            var py = (ActualHeight - _axisOffset) * (1 - Math.Min(1, point.Item1));

            return Points.Get((int)px, (int)py);
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

                var pointsToRemove = new List<DynamicStatistics.PlotPoint>();

                for (int i = 0; i < points.Count - MIN_POINTS_COUNT/*minPointsCount*/; ++i)
                {
                    var ticks = points.Last().Item2 - points[0].Item2;
                    var point0 = func(points, points[i], ticks);
                    var point1 = func(points, points[i + 1], ticks);
                    var point2 = func(points, points[i + 2], ticks);

                    if (Math.Abs(Angle(point0, point1) - Angle(point1, point2)) < Math.PI / 720D)
                    {
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
                            pointsToRemove.Add(points[i + 1]);

                            if (points.Count - pointsToRemove.Count < MIN_POINTS_COUNT)
                            {
                                break;
                            }

                            i += 2;
                        }
                    }
                }

                if (!pointsToRemove.Any())
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

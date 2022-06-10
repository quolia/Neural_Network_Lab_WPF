using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    using PointFunc = Func<DynamicStatistics.PlotPoints, DynamicStatistics.PlotPoint, long, Point>;

    sealed public partial class PlotterPresenter : UserControl
    {
        private const int AXIS_OFFSET = 6;
        private bool _isBaseRedrawNeeded;

        private readonly Typeface _font = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        private Pen _penBlack = Tools.Draw.GetPen(Colors.Black);
        private Pen _penLightGray = Tools.Draw.GetPen(Colors.LightGray);


        public PlotterPresenter()
        {
            InitializeComponent();

            _penBlack.Freeze();
            _penLightGray.Freeze();

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

            var networkModel = networkModels.First;

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

        public void Draw(ListX<NetworkDataModel> networkModels, NetworkDataModel selectedNetworkModel)
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

            var networkModel = networkModels.First;

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

            if (selectedNetworkModel != null && selectedNetworkModel.DynamicStatistics.PercentData.Count > 0)
            {
                DrawLabel(selectedNetworkModel.DynamicStatistics.PercentData, in selectedNetworkModel.Color);
            }

            CtlPresenter.Update();
        }

        private void DrawPlotter()
        {
            CtlBase.Clear();

            double step = (ActualWidth - AXIS_OFFSET) / 10;
            double y = ActualHeight - AXIS_OFFSET - AXIS_OFFSET / 2;
            double x;

            for (x = 0; x < 11; ++x)
            {
                CtlBase.DrawLine(_penLightGray, ref Points.Get((float)(AXIS_OFFSET + step * x), (float)y), ref Points.Get((float)(AXIS_OFFSET + step * x), 0));
                CtlBase.DrawLine(_penBlack, ref Points.Get((float)(AXIS_OFFSET + step * x), (float)y), ref Points.Get((float)(AXIS_OFFSET + step * x), (float)(y + AXIS_OFFSET)));
            }

            step = (ActualHeight - AXIS_OFFSET) / 10;
            x = AXIS_OFFSET / 2;

            for (y = 0; y < 11; ++y)
            {
                CtlBase.DrawLine(_penLightGray,
                                 ref Points.Get((float)x, (float)(ActualHeight - AXIS_OFFSET - step * y)),
                                 ref Points.Get(ActualWidth, (float)(ActualHeight - AXIS_OFFSET - step * y)));

                CtlBase.DrawLine(_penBlack,
                                 ref Points.Get((float)x, (float)(ActualHeight - AXIS_OFFSET - step * y)),
                                 ref Points.Get((float)(x + AXIS_OFFSET), (float)(ActualHeight - AXIS_OFFSET - step * y)));
            }

            CtlBase.DrawLine(_penBlack,
                             ref Points.Get(AXIS_OFFSET, 0),
                             ref Points.Get(AXIS_OFFSET, ActualHeight));

            CtlBase.DrawLine(_penBlack,
                             ref Points.Get(0, ActualHeight - AXIS_OFFSET),
                             ref Points.Get(ActualWidth, ActualHeight - AXIS_OFFSET));
        }

        private void DrawData(DynamicStatistics.PlotPoints pointsData, in Color color, PointFunc func, bool isRect)
        {
            if (pointsData == null || !pointsData.Any())
            {
                return;
            }

            var pen = Tools.Draw.GetPen(in color);
            
            var firstPointData = pointsData[0];
            var lastPointData = pointsData.Last();

            var ticks = lastPointData.TimeTicks - firstPointData.TimeTicks;

            var prevPoint = new Point(-1000, -1000);
            var prevPointData = firstPointData;

            foreach (var pointData in pointsData)
            {
                var point = func(pointsData, pointData, ticks);

                if ((point.X - prevPoint.X) > 10 || Math.Abs(point.Y - prevPoint.Y) > 10 || pointData == lastPointData) // opt
                {
                    var fromPoint = func(pointsData, prevPointData, ticks);
                    CtlPresenter.DrawLine(pen, ref fromPoint, ref point);

                    if (isRect)
                    {
                        CtlPresenter.DrawRectangle(pen.Brush, pen, ref Rects.Get(point.X - 6 / 2, point.Y - 6 / 2, 6, 6));
                    }
                    else
                    {
                        CtlPresenter.DrawEllipse(pen.Brush, pen, ref Points.Get(point.X, point.Y), 7 / 2, 7 / 2);
                    }

                    prevPointData = pointData;
                    prevPoint = point;
                }
            }
        }

        private void DrawLabel(DynamicStatistics.PlotPoints pointsData, in Color color)
        {     
            var text = new FormattedText(TimeSpan.FromTicks(pointsData.Last().TimeTicks - pointsData[0].TimeTicks).ToString(Culture.TimeFormat)
                                         + " / " + Converter.DoubleToText(pointsData.Last().Value, "N6", true) + " %",
                                         Culture.Current,
                                         FlowDirection.LeftToRight,
                                         _font,
                                         10,
                                         Tools.Draw.GetBrush(in color),
                                         Render.PixelsPerDip);

            CtlPresenter.DrawRectangle(Tools.Draw.GetBrush(Tools.Draw.GetColor(150, Colors.White)),
                                       null,
                                       ref Rects.Get((ActualWidth - AXIS_OFFSET - text.Width) / 2 - 5,
                                                     ActualHeight - AXIS_OFFSET - 20,
                                                     text.Width + 10,
                                                     text.Height));

            CtlPresenter.DrawText(text, ref Points.Get((ActualWidth - AXIS_OFFSET - text.Width) / 2, ActualHeight - AXIS_OFFSET - 20));
        }

        private Point GetPointPercentData(DynamicStatistics.PlotPoints pointsData, DynamicStatistics.PlotPoint plotPoint, long ticks)
        {
            var pointData0 = pointsData[0];
            var pointX = ticks == 0 ? AXIS_OFFSET : AXIS_OFFSET + (ActualWidth - AXIS_OFFSET) * (plotPoint.TimeTicks - pointData0.TimeTicks) / ticks;
            var pointY = (ActualHeight - AXIS_OFFSET) * (1 - (plotPoint.Item1 / 100));

            return Points.Get((int)pointX, (int)pointY);
        }

        private Point GetPointCostData(DynamicStatistics.PlotPoints pointsData, DynamicStatistics.PlotPoint plotPoint, long ticks)
        {
            var pointData0 = pointsData[0];
            var pointX = ticks == 0 ? AXIS_OFFSET : AXIS_OFFSET + (ActualWidth - AXIS_OFFSET) * (plotPoint.TimeTicks - pointData0.TimeTicks) / ticks;
            var pointY = (ActualHeight - AXIS_OFFSET) * (1 - Math.Min(1, plotPoint.Value));

            return Points.Get((int)pointX, (int)pointY);
        }

        private void Vanish(DynamicStatistics.PlotPoints pointsData, PointFunc func)
        {
            const int VANISH_AREA = 14;
            const int MIN_POINTS_COUNT = 10;

            while (true)
            {
                if (pointsData.Count <= MIN_POINTS_COUNT)
                {
                    return;
                }

                List<DynamicStatistics.PlotPoint> pointsDataToRemove = null;

                for (int i = 0; i < pointsData.Count - MIN_POINTS_COUNT; ++i)
                {
                    var ticks = pointsData.Last().TimeTicks - pointsData[0].TimeTicks;
                    var point0 = func(pointsData, pointsData[i], ticks);
                    var point1 = func(pointsData, pointsData[i + 1], ticks);
                    var point2 = func(pointsData, pointsData[i + 2], ticks);

                    if (Math.Abs(Angle(in point0, in point1) - Angle(in point1, in point2)) < Math.PI / 720D)
                    {
                        if (pointsDataToRemove == null)
                        {
                            pointsDataToRemove = new List<DynamicStatistics.PlotPoint>();
                        }

                        pointsDataToRemove.Add(pointsData[i + 1]);

                        if (pointsData.Count - pointsDataToRemove.Count < MIN_POINTS_COUNT)
                        {
                            break;
                        }

                        i += 2;
                    }
                    else
                    {
                        if (Math.Abs(point0.X - point1.X) < VANISH_AREA && Math.Abs(point0.Y - point1.Y) < VANISH_AREA)
                        {
                            if (pointsDataToRemove == null)
                            {
                                pointsDataToRemove = new List<DynamicStatistics.PlotPoint>();
                            }

                            pointsDataToRemove.Add(pointsData[i + 1]);

                            if (pointsData.Count - pointsDataToRemove.Count < MIN_POINTS_COUNT)
                            {
                                break;
                            }

                            i += 2;
                        }
                    }
                }

                if (pointsDataToRemove == null)
                {
                    return;
                }

                pointsDataToRemove.ForEach(pointData => pointsData.Remove(pointData));
            }
        }

        private double Angle(in Point point1, in Point point2)
        {
            return Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
        }

        public void Clear()
        {
            Draw(null, null);
        }
    }
}

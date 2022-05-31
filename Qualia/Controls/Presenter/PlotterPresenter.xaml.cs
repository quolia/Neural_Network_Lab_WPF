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

        public void Vanish(ListX<NetworkDataModel> models)
        {
            var model = models[0];

            while (model != null)
            {
                if (!model.IsEnabled)
                {
                    continue;
                }

                Vanish(model.DynamicStatistics.PercentData, GetPointPercentData);
                Vanish(model.DynamicStatistics.CostData, GetPointCostData);

                model.DynamicStatistics.CopyForRender = new DynamicStatistics(model.DynamicStatistics);

                model = model.Next;
            }
        }

        public void Draw(ListX<NetworkDataModel> models, NetworkDataModel selectedModel)
        {
            if (_isBaseRedrawNeeded)
            {
                DrawPlotter();
                _isBaseRedrawNeeded = false;
            }

            CtlPresenter.Clear();

            if (models == null)
            {
                return;
            }

            var model = models[0];

            while (model != null)
            { 
                if (!model.IsEnabled)
                {
                    continue;
                }

                DrawData(model.DynamicStatistics.CopyForRender.PercentData, Tools.Draw.GetColor(220, model.Color), GetPointPercentData, false);
                DrawData(model.DynamicStatistics.CopyForRender.CostData, Tools.Draw.GetColor(150, model.Color), GetPointCostData, true);

                model = model.Next;
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
            double x = 0;
            for (x = 0; x < 11; ++x)
            {
                CtlBase.DrawLine(penLightGray, Points.Get((float)(_axisOffset + step * x), (float)y), Points.Get((float)(_axisOffset + step * x), 0));
                CtlBase.DrawLine(penBlack, Points.Get((float)(_axisOffset + step * x), (float)y), Points.Get((float)(_axisOffset + step * x), (float)(y + _axisOffset)));
            }

            step = (ActualHeight - _axisOffset) / 10;
            x = _axisOffset / 2;
            for (y = 0; y < 11; ++y)
            {
                CtlBase.DrawLine(penLightGray, Points.Get((float)x, (float)(ActualHeight - _axisOffset - step * y)), Points.Get(ActualWidth, (float)(ActualHeight - _axisOffset - step * y)));
                CtlBase.DrawLine(penBlack, Points.Get((float)x, (float)(ActualHeight - _axisOffset - step * y)), Points.Get((float)(x + _axisOffset), (float)(ActualHeight - _axisOffset - step * y)));
            }

            CtlBase.DrawLine(penBlack, Points.Get(_axisOffset, 0), Points.Get(_axisOffset, ActualHeight));
            CtlBase.DrawLine(penBlack, Points.Get(0, ActualHeight - _axisOffset), Points.Get(ActualWidth, ActualHeight - _axisOffset));
        }

        private void DrawData(DynamicStatistics.PlotPoints data, Color color, PointFunc func, bool isRect)
        {
            if (data == null || data.FirstOrDefault() == null)
            {
                return;
            }

            var pen = Tools.Draw.GetPen(color);
            
            var firstData = data[0];
            var lastData = data.Last();

            var ticks = lastData.Item2 - firstData.Item2;

            Point prevPoint = new Point(-1000, -1000);
            var prevData = firstData;
            foreach (var d in data)
            {
                var point = func(data, d, ticks);
                if ((point.X - prevPoint.X) > 10 || Math.Abs(point.Y - prevPoint.Y) > 10 || d == lastData) // opt
                {
                    CtlPresenter.DrawLine(pen, func(data, prevData, ticks), point);

                    if (isRect)
                    {
                        CtlPresenter.DrawRectangle(pen.Brush, pen, Rects.Get(point.X - 6 / 2, point.Y - 6 / 2, 6, 6));
                    }
                    else
                    {
                        CtlPresenter.DrawEllipse(pen.Brush, pen, Points.Get(point.X, point.Y), 7 / 2, 7 / 2);
                    }

                    prevData = d;
                    prevPoint = point;
                }
            }
        }

        private void DrawLabel(DynamicStatistics.PlotPoints data, Color color)
        {     
            var text = new FormattedText(TimeSpan.FromTicks(data.Last().Item2 - data[0].Item2).ToString(@"hh\:mm\:ss") + " / " + Converter.DoubleToText(data.Last().Item1, "N6", false) + " %", Culture.Current, FlowDirection.LeftToRight, _font, 10, Tools.Draw.GetBrush(color), Render.PixelsPerDip);
            CtlPresenter.DrawRectangle(Tools.Draw.GetBrush(Tools.Draw.GetColor(150, Colors.White)), null, Rects.Get((ActualWidth - _axisOffset - text.Width) / 2 - 5, ActualHeight - _axisOffset - 20, text.Width + 10, text.Height));
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

        private void Vanish(DynamicStatistics.PlotPoints data, PointFunc func)
        {
            const int vanishArea = 14;
            const int minPointsCount = 10;

            while (true)
            {
                if (data.Count <= minPointsCount)
                {
                    return;
                }

                var pointsToRemove = new List<DynamicStatistics.PlotPoint>();

                for (int i = 0; i < data.Count - minPointsCount/*minPointsCount*/; ++i)
                {
                    var ticks = data.Last().Item2 - data[0].Item2;
                    var p0 = func(data, data[i], ticks);
                    var p1 = func(data, data[i + 1], ticks);
                    var p2 = func(data, data[i + 2], ticks);

                    if (Math.Abs(Angle(p0, p1) - Angle(p1, p2)) < Math.PI / 720D) // 90
                    {
                        pointsToRemove.Add(data[i + 1]);

                        if (data.Count - pointsToRemove.Count < minPointsCount)
                        {
                            break;
                        }

                        i += 2;
                    }
                    else
                    {
                        if (Math.Abs(p0.X - p1.X) < vanishArea && Math.Abs(p0.Y - p1.Y) < vanishArea)
                        {
                            pointsToRemove.Add(data[i + 1]);

                            if (data.Count - pointsToRemove.Count < minPointsCount)
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

                pointsToRemove.ForEach(p => data.Remove(p));
            }
        }

        private double Angle(Point p1, Point p2)
        {
            return Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
        }

        public void Clear()
        {
            Draw(null, null);
        }
    }
}

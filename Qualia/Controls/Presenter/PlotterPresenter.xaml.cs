using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    using PointFunc = Func<DynamicStatistics.PlotPoints, DynamicStatistics.PlotPoint, long, Point>;

    public partial class PlotterPresenter : UserControl
    {
        int AxisOffset = 6;
        bool IsBaseRedrawNeeded;

        Typeface Font = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

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
            IsBaseRedrawNeeded = true;
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
            if (IsBaseRedrawNeeded)
            {
                DrawPlotter();
                IsBaseRedrawNeeded = false;
            }

            CtlPresenter.Clear();

            var model = models[0];

            while (model != null)
            { 
                if (!model.IsEnabled)
                {
                    continue;
                }

                //Vanish(model.DynamicStatistics.PercentData, GetPointPercentData);
                //Vanish(model.DynamicStatistics.CostData, GetPointCostData);

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

            double step = (ActualWidth - AxisOffset) / 10;
            double y = ActualHeight - AxisOffset - AxisOffset / 2;
            double x = 0;
            for (x = 0; x < 11; ++x)
            {
                CtlBase.DrawLine(penLightGray, Points.Get((float)(AxisOffset + step * x), (float)y), Points.Get((float)(AxisOffset + step * x), 0));
                CtlBase.DrawLine(penBlack, Points.Get((float)(AxisOffset + step * x), (float)y), Points.Get((float)(AxisOffset + step * x), (float)(y + AxisOffset)));
            }

            step = (ActualHeight - AxisOffset) / 10;
            x = AxisOffset / 2;
            for (y = 0; y < 11; ++y)
            {
                CtlBase.DrawLine(penLightGray, Points.Get((float)x, (float)(ActualHeight - AxisOffset - step * y)), Points.Get(ActualWidth, (float)(ActualHeight - AxisOffset - step * y)));
                CtlBase.DrawLine(penBlack, Points.Get((float)x, (float)(ActualHeight - AxisOffset - step * y)), Points.Get((float)(x + AxisOffset), (float)(ActualHeight - AxisOffset - step * y)));
            }

            CtlBase.DrawLine(penBlack, Points.Get(AxisOffset, 0), Points.Get(AxisOffset, ActualHeight));
            CtlBase.DrawLine(penBlack, Points.Get(0, ActualHeight - AxisOffset), Points.Get(ActualWidth, ActualHeight - AxisOffset));
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
            var text = new FormattedText(TimeSpan.FromTicks(data.Last().Item2 - data[0].Item2).ToString(@"hh\:mm\:ss") + " / " + Converter.DoubleToText(data.Last().Item1, "N6", false) + " %", Culture.Current, FlowDirection.LeftToRight, Font, 10, Tools.Draw.GetBrush(color), Render.PixelsPerDip);
            CtlPresenter.DrawRectangle(Tools.Draw.GetBrush(Tools.Draw.GetColor(150, Colors.White)), null, Rects.Get((ActualWidth - AxisOffset - text.Width) / 2 - 5, ActualHeight - AxisOffset - 20, text.Width + 10, text.Height));
            CtlPresenter.DrawText(text, Points.Get((ActualWidth - AxisOffset - text.Width) / 2, ActualHeight - AxisOffset - 20));
        }

        private Point GetPointPercentData(DynamicStatistics.PlotPoints data, DynamicStatistics.PlotPoint point, long ticks)
        {
            var p0 = data[0];
            var px = ticks == 0 ? AxisOffset : AxisOffset + (ActualWidth - AxisOffset) * (point.Item2 - p0.Item2) / ticks;
            var py = (ActualHeight - AxisOffset) * (1 - (point.Item1 / 100));

            return Points.Get((int)px, (int)py);
        }

        private Point GetPointCostData(DynamicStatistics.PlotPoints data, DynamicStatistics.PlotPoint point, long ticks)
        {
            var p0 = data[0];
            var px = ticks == 0 ? AxisOffset : AxisOffset + (ActualWidth - AxisOffset) * (point.Item2 - p0.Item2) / ticks;
            var py = (ActualHeight - AxisOffset) * (1 - Math.Min(1, point.Item1));

            return Points.Get((int)px, (int)py);
        }

        /*
        private void Vanish(DynamicStatistics.PlotPoints data, PointFunc func)
        {
            int vanishArea = 16;

            if (data.Count > 10)
            {
                var pointsToRemove = new List<DynamicStatistics.PlotPoint>();
                var totolTicks = data.Last().Item2 - data[0].Item2;

                for (int i = 0; i < data.Count * 0.8; ++i)
                {
                    var ticks = data.Last().Item2 - data[0].Item2;
                    var p0 = func(data, data[i], ticks);
                    var p1 = func(data, data[i + 1], ticks);
                    var p2 = func(data, data[i + 2], ticks);

                    if ((Math.Abs(p0.X - p2.X) < vanishArea && Math.Abs(p0.Y - p2.Y) < vanishArea) &&
                        (Math.Abs(p0.X - p1.X) < vanishArea && Math.Abs(p0.Y - p1.Y) < vanishArea) &&
                        (Math.Abs(p1.X - p2.X) < vanishArea && Math.Abs(p1.Y - p2.Y) < vanishArea))
                    {
                        pointsToRemove.Add(data[i + 1]);
                    }
                    else
                    {
                        if ((p0.X == p1.X && p1.X == p2.X && Math.Abs(p0.Y - p1.Y) < vanishArea) || (p0.Y == p1.Y && p1.Y == p2.Y && Math.Abs(p0.X - p1.X) < vanishArea))
                        {
                            pointsToRemove.Add(data[i + 1]);
                        }
                    }
                }

                pointsToRemove.ForEach(p => data.Remove(p));
            }
        }
        */

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
                        //break;
                        if (data.Count - pointsToRemove.Count < minPointsCount)
                        {
                            break;
                        }

                        i += 2;
                    }
                    else
                    {
                        if (//Math.Abs(p0.X - p2.X) < vanishArea && Math.Abs(p0.Y - p2.Y) < vanishArea &&
                            Math.Abs(p0.X - p1.X) < vanishArea && Math.Abs(p0.Y - p1.Y) < vanishArea)// &&
                            //Math.Abs(p1.X - p2.X) < vanishArea && Math.Abs(p1.Y - p2.Y) < vanishArea)
                        {
                            pointsToRemove.Add(data[i + 1]);
                            //break;
                            if (data.Count - pointsToRemove.Count < minPointsCount)
                            {
                                break;
                            }

                            i += 2;
                        }
                        else
                        {/*
                            if ((p0.X == p1.X && p1.X == p2.X && Math.Abs(p0.Y - p1.Y) < vanishArea) || (p0.Y == p1.Y && p1.Y == p2.Y && Math.Abs(p0.X - p1.X) < vanishArea))
                            {
                                pointsToRemove.Add(data[i + 1]);
                                break;
                            }
                            */
                        }
                    }
                }

                if (!pointsToRemove.Any())
                {
                    return;
                }
                /*
                pointsToRemove.ForEach(p =>
                {
                    if (data.Count > 10)
                    {
                        data.Remove(p);
                    }
                    else
                    {
                        return;
                    }
                });
                */
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

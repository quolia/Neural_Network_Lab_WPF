using System;
using System.Collections.Generic;
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
    public partial class DataPresenter : UserControl
    {
        public Action<int> ValueChanged = delegate { };

        int PointSize;
        int PointsRearrangeSnap;
        int PointsCount;
        double Threshold;
        double[] Data;

        public INetworkTask Task;

        public DataPresenter()
        {
            InitializeComponent();

            PointSize = Config.Main.GetInt(Const.Param.PointSize, 7).Value;
            PointsRearrangeSnap = Config.Main.GetInt(Const.Param.PointsArrangeSnap, 10).Value;

            //CtlInputCount.Changed += CtlInputCount_ValueChanged;
            SizeChanged += DataPresenter_SizeChanged;
            CtlTask.SelectedIndexChanged += CtlTask_SelectedIndexChanged;
        }

        private void CtlTask_SelectedIndexChanged(int index)
        {
            Task = NetworkTask.Helper.GetInstance(CtlTask.SelectedItem.ToString());
        }

        private void DataPresenter_SizeChanged(object sender, EventArgs e)
        {
            Rearrange(Const.CurrentValue);
        }

        public int InputCount => (int)CtlInputCount.Value;

        public void LoadConfig(Config config, Action<int> onValueChanged)
        {
            CtlInputCount.MinimumValue = Config.Main.GetInt(Const.Param.InputNeuronsMinCount, 10).Value;
            CtlInputCount.MaximumValue = Config.Main.GetInt(Const.Param.InputNeuronsMaxCount, 10000).Value;

            NetworkTask.Helper.FillComboBox(CtlTask, config, Const.Param.Task, null);

            ValueChanged = onValueChanged;

            CtlInputCount.Changed -= CtlInputCount_ValueChanged;
            CtlInputCount.Value = config.GetInt(Const.Param.InputNeuronsCount, Const.DefaultInputNeuronsCount).Value;
            CtlInputCount.Changed += CtlInputCount_ValueChanged;
        }

        public void SaveConfig(Config config)
        {
            config.Set(Const.Param.InputNeuronsCount, (int)CtlInputCount.Value);
            config.Set(Const.Param.Task, CtlTask.SelectedItem.ToString());
        }

        private void CtlInputCount_ValueChanged()
        {
            ValueChanged((int)CtlInputCount.Value);
        }

        private void DrawPoint(int x, int y, double value)
        {
            var brush = value == 0 ? Brushes.White : Draw.GetBrush(value);
            var pen = Draw.GetPen(Colors.Black);

            CtlPresenter.DrawRectangle(brush, pen, new Rect(x * PointSize, y * PointSize, PointSize, PointSize));
        }

        private void TogglePoint(int c, double value)
        {
            var pos = GetPointPosition(c);
            DrawPoint(pos.Item1, pos.Item2, value);
        }

        public void SetInputDataAndDraw(NetworkDataModel model)
        {
            Threshold = model.InputThreshold;
            Data = new double[model.Layers.First().Neurons.Where(n => !n.IsBias).Count()];
            Range.ForEach(model.Layers.First().Neurons.Where(n => !n.IsBias), neuron => Data[neuron.Id] = neuron.Activation);
            Rearrange(PointsCount);
        }

        public void RearrangeWithNewPointsCount()
        {
            Rearrange((int)CtlInputCount.Value);
        }

        private void Rearrange(int pointsCount)
        {
            CtlPresenter.Clear();

            if (pointsCount == Const.CurrentValue)
            {
                pointsCount = PointsCount;
            }
            else
            {
                PointsCount = pointsCount;
            }

            int width = (int)Math.Max(ActualWidth, PointsRearrangeSnap * PointSize);

            int snaps = width / (PointsRearrangeSnap * PointSize);

            //CtlPresenter.Height = 1 + PointSize * (int)Math.Ceiling(1 + (double)(PointsCount / (snaps * PointsRearrangeSnap)));

            //CtlPresenter.StartRender();

            Range.For(PointsCount, p =>
            {
                var pos = GetPointPosition(p);
                DrawPoint(pos.Item1, pos.Item2, 0);
            });

            if (Data != null)
            {
                Range.For(Data.Length, y => TogglePoint(y, Data[y] > Threshold ? Data[y] : 0));
            }

            //CtlPresenter.CtlBox.Invalidate();
        }

        private Tuple<int, int> GetPointPosition(int pointNumber)
        {
            int width = Math.Max((int)ActualWidth, PointsRearrangeSnap * PointSize);

            int snaps = width / (PointsRearrangeSnap * PointSize);
            int y = (int)Math.Ceiling((double)(pointNumber / (snaps * PointsRearrangeSnap)));
            int x = pointNumber - (y * snaps * PointsRearrangeSnap);

            return new Tuple<int, int>(x, y);
        }
    }
}

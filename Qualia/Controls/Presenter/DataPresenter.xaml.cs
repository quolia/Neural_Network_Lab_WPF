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
        public INetworkTask Task;
        
        int PointSize;
        int PointsRearrangeSnap;
        int PointsCount;
        double Threshold;
        double[] Data;

        INetworkTaskChanged TaskChanged;

        public DataPresenter()
        {
            InitializeComponent();

            PointSize = Config.Main.GetInt(Const.Param.PointSize, 7).Value;
            PointsRearrangeSnap = Config.Main.GetInt(Const.Param.PointsArrangeSnap, 10).Value;

            SizeChanged += DataPresenter_SizeChanged;
            CtlTask.SetChangeEvent(CtlTask_SelectedIndexChanged);
        }

        private void CtlTask_SelectedIndexChanged()
        {
            if (CtlTask.SelectedItem != null)
            {
                Task = NetworkTask.Helper.GetInstance(CtlTask.SelectedItem.ToString());
                Task.SetChangeEvent(TaskParameterChanged);
                CtlHolder.Children.Clear();
                CtlHolder.Children.Add(Task.GetVisualControl());
                if (TaskChanged != null)
                {
                    TaskChanged.TaskChanged();
                }
            }
        }

        private void DataPresenter_SizeChanged(object sender, EventArgs e)
        {
            if (Task != null && Task.IsGridSnapAdjustmentAllowed())
            {
                Rearrange(Const.CurrentValue);
            }
        }

        public void LoadConfig(Config config, INetworkTaskChanged taskChanged)
        {  
            NetworkTask.Helper.FillComboBox(CtlTask, config, null);
            Task = NetworkTask.Helper.GetInstance(CtlTask.SelectedItem.ToString());
            CtlHolder.Children.Clear();
            CtlHolder.Children.Add(Task.GetVisualControl());
            Task.Load(config);
            Task.SetChangeEvent(TaskParameterChanged);
            TaskChanged = taskChanged;
            TaskParameterChanged();
        }

        void TaskParameterChanged()
        {
            if (TaskChanged != null)
            {
                RearrangeWithNewPointsCount();
                TaskChanged.TaskParameterChanged();
            }
        }

        public void SaveConfig(Config config)
        {
            CtlTask.Save(config);
            Task.Save(config);
            config.FlushToDrive();
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
            Rearrange(Task.GetInputCount());
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

            Range.For(PointsCount, p =>
            {
                var pos = GetPointPosition(p);
                DrawPoint(pos.Item1, pos.Item2, 0);
            });

            if (Data != null)
            {
                Range.For(Data.Length, y => TogglePoint(y, Data[y] > Threshold ? Data[y] : 0));
            }

            CtlPresenter.Update();
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

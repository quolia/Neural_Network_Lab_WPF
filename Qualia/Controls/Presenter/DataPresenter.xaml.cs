using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
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
        double[] Stat;

        INetworkTaskChanged TaskChanged;

        public DataPresenter()
        {
            InitializeComponent();

            PointSize = (int)Config.Main.GetInt(Const.Param.PointSize, 7).Value;

            SizeChanged += DataPresenter_SizeChanged;
            CtlTask.SetChangeEvent(CtlTask_SelectedIndexChanged);
        }

        private void CtlTask_SelectedIndexChanged()
        {
            if (CtlTask.SelectedItem != null)
            {
                Task = NetworkTask.Helper.GetInstance(CtlTask.SelectedItem.ToString());
                PointsRearrangeSnap = Task.GetPointsRearrangeSnap();
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
            PointsRearrangeSnap = Task.GetPointsRearrangeSnap();
            CtlHolder.Children.Clear();
            CtlHolder.Children.Add(Task.GetVisualControl());
            Task.SetConfig(config);
            Task.LoadConfig();
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
            CtlTask.SetConfig(config);
            Task.SetConfig(config);

            CtlTask.SaveConfig();
            Task.SaveConfig();

            config.FlushToDrive();
        }

        private void DrawPoint(int x, int y, double value, bool isData)
        {
            var brush = value == 0 ? Brushes.White : (isData ? Draw.GetBrush(value) : Draw.GetBrush(Draw.GetColor((byte)(255 * value), Colors.Green)));
            var pen = Draw.GetPen(Colors.Black);

            CtlPresenter.DrawRectangle(brush, pen, Rects.Get(x * PointSize, y * PointSize, PointSize, PointSize));
        }
        public void SetInputDataAndDraw(NetworkDataModel model)
        {
            Threshold = model.InputThreshold;
            var count = model.Layers[0].Neurons.Count(n => !n.IsBias);
            if (Data == null || Data.Length != count)
            {
                Data = new double[count];
            }
            else
            {
                Array.Clear(Data, 0, Data.Length);
            }

            var neuron = model.Layers[0].Neurons[0];
            while (neuron != null)
            {
                if (!neuron.IsBias)
                {
                    Data[neuron.Id] = neuron.Activation;
                }
                neuron = neuron.Next;
            }

            Rearrange(PointsCount);
            if (Stat == null || Stat.Length != Data.Length)
            {
                Stat = new double[Data.Length];
            }
        }
        public void RearrangeWithNewPointsCount()
        {
            Rearrange(Task.GetInputCount());
        }

        private int GetSnaps()
        {
            int width = (int)Math.Max(ActualWidth, PointsRearrangeSnap * PointSize);
            return Task.IsGridSnapAdjustmentAllowed() ? width / (PointsRearrangeSnap * PointSize) : 1;
        }

        private void Rearrange(int pointsCount)
        {
            CtlPresenter.Clear();

            if (pointsCount != Const.CurrentValue)
            {
                PointsCount = pointsCount;
            }

            if (Data == null || Data.Length != PointsCount)
            {
                Data = new double[PointsCount];
            }

            double maxStat = Stat == null ? 0 : Stat.Max();

            for (int p = 0; p < PointsCount; ++p)
            {
                var pos = GetPointPosition(p);

                if (Data[p] > Threshold)
                {
                    DrawPoint(pos.Item1, pos.Item2, Data[p], true);
                }
                else
                {
                    DrawPoint(pos.Item1, pos.Item2, maxStat > 0 ? Stat[p] / maxStat : 0, false);
                }
            }

            CtlPresenter.Update();
        }

        private Tuple<int, int> GetPointPosition(int pointNumber)
        {
            int snaps = GetSnaps();
            int y = (int)Math.Ceiling((double)(pointNumber / (snaps * PointsRearrangeSnap)));
            int x = pointNumber - (y * snaps * PointsRearrangeSnap);

            return new Tuple<int, int>(x, y);
        }

        public void SetInputStat(NetworkDataModel model)
        {
            if (Stat != null)
            {
                int i = 0;
                var neuron = model.Layers[0].Neurons[0];
                while (neuron != null)
                {
                    if (!neuron.IsBias)
                    {
                        Stat[i] += neuron.Activation > Threshold ? neuron.Activation : 0;
                    }
                    ++i;
                    neuron = neuron.Next;
                }
            }
        }
    }
}

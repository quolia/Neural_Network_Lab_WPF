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
        long[] Stat;

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

        private void DrawPoint(int x, int y, double value, bool isData)
        {
            var brush = value == 0 ? Brushes.White : (isData ? Draw.GetBrush(value) : Draw.GetBrush(Draw.GetColor((byte)(255 * value), Colors.LightGray)));
            var pen = Draw.GetPen(Colors.Black);

            CtlPresenter.DrawRectangle(brush, pen, Rects.Get(x * PointSize, y * PointSize, PointSize, PointSize));
        }
        public void SetInputDataAndDraw(NetworkDataModel model)
        {
            Threshold = model.InputThreshold;
            var count = model.Layers.First().Neurons.Count(n => !n.IsBias);
            if (Data == null || Data.Length != count)
            {
                Data = new double[count];
            }
            else
            {
                Array.Clear(Data, 0, Data.Length);
            }

            var neuron = model.Layers.First().Neurons.First();
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
                Stat = new long[Data.Length];
            }
        }
        public void RearrangeWithNewPointsCount()
        {
            Rearrange(Task.GetInputCount());
        }

        private void Rearrange(int pointsCount)
        {
            var sw = Stopwatch.StartNew();

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

            long maxStat = 0;
            if (Stat != null)
            {
                var minBase = Stat.Min();
                Range.For(Stat.Length, i => Stat[i] -= minBase);
                maxStat = Stat.Max();
            }

            for (int p = 0; p < PointsCount; ++p)
            {
                var pos = GetPointPosition(p);

                if (Data == null)
                {
                    DrawPoint(pos.Item1, pos.Item2, 0, true);
                }
                else
                {
                    if (Data[p] > Threshold)
                    {
                        DrawPoint(pos.Item1, pos.Item2, Data[p], true);
                    }
                    else
                    {
                        DrawPoint(pos.Item1, pos.Item2, maxStat > 0 ? Stat[p] / maxStat : 0, false);
                    }
                }
            }

            CtlPresenter.Update();

            sw.Stop();
            RenderTime.Data = sw.Elapsed.Ticks;
        }

        private Tuple<int, int> GetPointPosition(int pointNumber)
        {
            int width = Math.Max((int)ActualWidth, PointsRearrangeSnap * PointSize);

            int snaps = width / (PointsRearrangeSnap * PointSize);
            int y = (int)Math.Ceiling((double)(pointNumber / (snaps * PointsRearrangeSnap)));
            int x = pointNumber - (y * snaps * PointsRearrangeSnap);

            return new Tuple<int, int>(x, y);
        }

        public void SetInputStat(NetworkDataModel model)
        {
            if (Stat != null)
            {
                var neuron = model.Layers.First().Neurons.First();
                while (neuron != null)
                {
                    if (!neuron.IsBias)
                    {
                        for (int i = 0; i < Stat.Length; ++i)
                        {
                            Stat[i] += neuron.Activation > Threshold ? 1 : 0;
                        }
                    }
                    neuron = neuron.Next;
                }
            }
        }
    }
}

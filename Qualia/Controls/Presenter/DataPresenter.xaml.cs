using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public partial class DataPresenter : UserControl
    {
        public INetworkTask Task;
        
        private int _pointSize;
        private int _pointsRearrangeSnap;
        private int _pointsCount;
        private double _threshold;
        private double[] _data;
        private double[] _stat;

        private INetworkTaskChanged _onTaskChanged;

        public DataPresenter()
        {
            InitializeComponent();

            _pointSize = (int)Config.Main.GetInt(Const.Param.PointSize, 7).Value;

            SizeChanged += DataPresenter_SizeChanged;
            CtlTask.SetChangeEvent(CtlTask_SelectedIndexChanged);
        }

        private void CtlTask_SelectedIndexChanged()
        {
            if (CtlTask.SelectedItem == null)
            {
                return;
            }

            Task = NetworkTask.Helper.GetInstance(CtlTask.SelectedItem.ToString());
            _pointsRearrangeSnap = Task.GetPointsRearrangeSnap();
            Task.SetChangeEvent(TaskParameterChanged);
            CtlHolder.Children.Clear();
            CtlHolder.Children.Add(Task.GetVisualControl());
            if (_onTaskChanged != null)
            {
                _onTaskChanged.TaskChanged();
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
            _pointsRearrangeSnap = Task.GetPointsRearrangeSnap();
            CtlHolder.Children.Clear();
            CtlHolder.Children.Add(Task.GetVisualControl());
            Task.SetConfig(config);
            Task.LoadConfig();
            Task.SetChangeEvent(TaskParameterChanged);
            _onTaskChanged = taskChanged;
            TaskParameterChanged();
        }

        void TaskParameterChanged()
        {
            if (_onTaskChanged == null)
            {
                return;
            }

            RearrangeWithNewPointsCount();
            _onTaskChanged.TaskParameterChanged();
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

            CtlPresenter.DrawRectangle(brush, pen, Rects.Get(x * _pointSize, y * _pointSize, _pointSize, _pointSize));
        }
        public void SetInputDataAndDraw(NetworkDataModel model)
        {
            _threshold = model.InputThreshold;
            var count = model.Layers[0].Neurons.Count(n => !n.IsBias);
            if (_data == null || _data.Length != count)
            {
                _data = new double[count];
                _stat = new double[_data.Length];
            }
            else
            {
                Array.Clear(_data, 0, _data.Length);
            }

            var neuron = model.Layers[0].Neurons[0];
            while (neuron != null)
            {
                if (!neuron.IsBias)
                {
                    _data[neuron.Id] = neuron.Activation;
                }
                neuron = neuron.Next;
            }

            Rearrange(_pointsCount);
        }
        public void RearrangeWithNewPointsCount()
        {
            _data = null;
            Rearrange(Task.GetInputCount());
        }

        private int GetSnaps()
        {
            int width = (int)Math.Max(ActualWidth, _pointsRearrangeSnap * _pointSize);

            return Task.IsGridSnapAdjustmentAllowed() ? width / (_pointsRearrangeSnap * _pointSize) : 1;
        }

        private void Rearrange(int pointsCount)
        {
            CtlPresenter.Clear();

            if (pointsCount != Const.CurrentValue)
            {
                _pointsCount = pointsCount;
            }

            if (_data == null || _data.Length != _pointsCount)
            {
                _data = new double[_pointsCount];
                _stat = new double[_pointsCount];
            }

            double maxStat = _stat == null ? 0 : _stat.Max();

            for (int p = 0; p < _pointsCount; ++p)
            {
                var pos = GetPointPosition(p);

                if (_data[p] > _threshold)
                {
                    DrawPoint(pos.Item1, pos.Item2, _data[p], true);
                }
                else
                {
                    DrawPoint(pos.Item1, pos.Item2, maxStat > 0 ? _stat[p] / maxStat : 0, false);
                }
            }

            CtlPresenter.Update();
        }

        private Tuple<int, int> GetPointPosition(int pointNumber)
        {
            int snaps = GetSnaps();
            int y = (int)Math.Ceiling((double)(pointNumber / (snaps * _pointsRearrangeSnap)));
            int x = pointNumber - (y * snaps * _pointsRearrangeSnap);

            return new Tuple<int, int>(x, y);
        }

        public void SetInputStat(NetworkDataModel model)
        {
            if (_stat != null)
            {
                int i = 0;
                var neuron = model.Layers[0].Neurons[0];
                while (neuron != null)
                {
                    if (!neuron.IsBias)
                    {
                        _stat[i] += neuron.Activation > _threshold ? neuron.Activation : 0;
                    }
                    ++i;
                    neuron = neuron.Next;
                }
            }
        }
    }
}

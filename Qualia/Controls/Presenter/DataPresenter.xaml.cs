using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public partial class DataPresenter : UserControl
    {
        public INetworkTask NetworkTask;
        
        private readonly int _pointSize;
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

            NetworkTask = Tools.NetworkTask.Helper.GetInstance(CtlTask.SelectedItem.ToString());
            _pointsRearrangeSnap = NetworkTask.GetPointsRearrangeSnap();
            NetworkTask.SetChangeEvent(TaskParameterChanged);
            CtlHolder.Children.Clear();
            CtlHolder.Children.Add(NetworkTask.GetVisualControl());
            if (_onTaskChanged != null)
            {
                _onTaskChanged.TaskChanged();
            }
        }

        private void DataPresenter_SizeChanged(object sender, EventArgs e)
        {
            if (NetworkTask != null && NetworkTask.IsGridSnapAdjustmentAllowed())
            {
                Rearrange(Const.CurrentValue);
            }
        }

        public void LoadConfig(Config config, INetworkTaskChanged taskChanged)
        {
            Tools.NetworkTask.Helper.FillComboBox(CtlTask, config, null);
            NetworkTask = Tools.NetworkTask.Helper.GetInstance(CtlTask.SelectedItem.ToString());
            _pointsRearrangeSnap = NetworkTask.GetPointsRearrangeSnap();
            CtlHolder.Children.Clear();
            CtlHolder.Children.Add(NetworkTask.GetVisualControl());
            NetworkTask.SetConfig(config);
            NetworkTask.LoadConfig();
            NetworkTask.SetChangeEvent(TaskParameterChanged);
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
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));   
            }

            CtlTask.SetConfig(config);
            NetworkTask.SetConfig(config);

            CtlTask.SaveConfig();
            NetworkTask.SaveConfig();

            config.FlushToDrive();
        }

        private void DrawPoint(int x, int y, double value, bool isData)
        {
            var brush = value == 0
                        ? Brushes.White
                        : (isData ? Draw.GetBrush(value) : Draw.GetBrush(Draw.GetColor((byte)(255 * value), Colors.Green)));

            var pen = Draw.GetPen(Colors.Black);

            CtlPresenter.DrawRectangle(brush, pen, Rects.Get(x * _pointSize, y * _pointSize, _pointSize, _pointSize));
        }
        public void SetInputDataAndDraw(NetworkDataModel networkModel)
        {
            if (networkModel == null)
            {
                throw new ArgumentNullException(nameof(networkModel));
            }

            _threshold = networkModel.InputThreshold;
            var count = networkModel.Layers[0].Neurons.Count(n => !n.IsBias);

            if (_data == null || _data.Length != count)
            {
                _data = new double[count];
                _stat = new double[_data.Length];
            }
            else
            {
                Array.Clear(_data, 0, _data.Length);
            }

            var neuron = networkModel.Layers[0].Neurons[0];
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
            Rearrange(NetworkTask.GetInputCount());
        }

        private int GetSnaps()
        {
            int width = (int)Math.Max(ActualWidth, _pointsRearrangeSnap * _pointSize);

            return NetworkTask.IsGridSnapAdjustmentAllowed()
                   ? width / (_pointsRearrangeSnap * _pointSize)
                   : 1;
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

            for (int ind = 0; ind < _pointsCount; ++ind)
            {
                var position = GetPointPosition(ind);

                if (_data[ind] > _threshold)
                {
                    DrawPoint(position.Item1, position.Item2, _data[ind], true);
                }
                else
                {
                    DrawPoint(position.Item1, position.Item2, maxStat > 0 ? _stat[ind] / maxStat : 0, false);
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

        public void SetInputStat(NetworkDataModel networkModel)
        {
            if (_stat == null)
            {
                return;
            }

            if (networkModel == null)
            {
                throw new ArgumentNullException(nameof(networkModel));
            }

            int i = 0;
            var neuron = networkModel.Layers[0].Neurons[0];

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

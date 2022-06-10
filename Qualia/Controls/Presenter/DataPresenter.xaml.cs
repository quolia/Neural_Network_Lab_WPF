using System;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    using Point = System.Windows.Point;
    public partial class DataPresenter : UserControl
    {
        private const int CURRENT_POINTS_COUNT = -1;

        public INetworkTask NetworkTask;
        
        private readonly int _pointSize;
        private int _pointsRearrangeSnap;
        private int _pointsCount;
        private double _threshold;
        private double[] _data;
        private double[] _stat;

        private INetworkTaskChanged _onTaskChanged;

        private readonly System.Windows.Media.Pen _penBlack = Draw.GetPen(Colors.Black);

        public DataPresenter()
        {
            InitializeComponent();

            _penBlack.Freeze();

            _pointSize = (int)Config.Main.GetInt(Const.Param.PointSize, 7).Value;

            SizeChanged += DataPresenter_SizeChanged;
            CtlTasks.SetChangeEvent(CtlTask_SelectedIndexChanged);
        }

        private void CtlTask_SelectedIndexChanged()
        {
            if (CtlTasks.SelectedItem == null)
            {
                return;
            }

            NetworkTask = Tools.NetworkTask.Helper.GetInstance(CtlTasks.SelectedItem.ToString());
            _pointsRearrangeSnap = NetworkTask.GetPointsRearrangeSnap();
            NetworkTask.SetChangeEvent(TaskParameterChanged);

            CtlHolder.Children.Clear();

            var controls = NetworkTask.GetVisualControl();
            CtlHolder.Children.Add(controls);

            if (_onTaskChanged != null)
            {
                _onTaskChanged.TaskChanged();
            }
        }

        private void DataPresenter_SizeChanged(object sender, EventArgs e)
        {
            if (NetworkTask != null && NetworkTask.IsGridSnapAdjustmentAllowed())
            {
                Rearrange(CURRENT_POINTS_COUNT);
            }
        }

        public void LoadConfig(Config config, INetworkTaskChanged taskChanged)
        {
            Tools.NetworkTask.Helper.FillComboBox(CtlTasks, config, null);
            NetworkTask = Tools.NetworkTask.Helper.GetInstance(CtlTasks.SelectedItem.ToString());
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

            CtlTasks.SetConfig(config);
            NetworkTask.SetConfig(config);

            CtlTasks.SaveConfig();
            NetworkTask.SaveConfig();

            config.FlushToDrive();
        }

        private void DrawPoint(double x, double y, double value, bool isData)
        {
            var brush = value == 0
                        ? System.Windows.Media.Brushes.White
                        : (isData ? Draw.GetBrush(value) : Draw.GetBrush(Draw.GetColor((byte)(255 * value), Colors.Green)));

            CtlPresenter.DrawRectangle(brush, _penBlack, ref Rects.Get(x * _pointSize, y * _pointSize, _pointSize, _pointSize));
        }
        public void SetInputDataAndDraw(NetworkDataModel networkModel)
        {
            if (networkModel == null)
            {
                throw new ArgumentNullException(nameof(networkModel));
            }

            _threshold = networkModel.InputThreshold;
            var count = networkModel.Layers.First.Neurons.CountIf(n => !n.IsBias);

            if (_data == null || _data.Length != count)
            {
                _data = new double[count];
                _stat = new double[_data.Length];
            }
            else
            {
                Array.Clear(_data, 0, _data.Length);
            }

            var neuronModel = networkModel.Layers.First.Neurons.First;
            while (neuronModel != null)
            {
                if (!neuronModel.IsBias)
                {
                    _data[neuronModel.Id] = neuronModel.Activation;
                }
                neuronModel = neuronModel.Next;
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

            if (pointsCount != CURRENT_POINTS_COUNT)
            {
                _pointsCount = pointsCount;
            }

            if (_data == null || _data.Length != _pointsCount)
            {
                _data = new double[_pointsCount];
                _stat = new double[_pointsCount];
            }

            double maxStat = MaxStat();

            for (int i = 0; i < _pointsCount; ++i)
            {
                ref var pointPosition = ref GetPointPosition(i);

                if (_data[i] > _threshold)
                {
                    DrawPoint(pointPosition.X, pointPosition.Y, _data[i], true);
                }
                else
                {
                    DrawPoint(pointPosition.X, pointPosition.Y, maxStat > 0 ? _stat[i] / maxStat : 0, false);
                }
            }

            CtlPresenter.Update();
        }

        private double MaxStat()
        {
            if (_stat == null)
            {
                return 0;
            }

            double max = 0;
            for (int i = 0; i < _stat.Length; ++i)
            {
                if (_stat[i] > max)
                {
                    max = _stat[i];
                }
            }

            return max;
        }

        private ref Point GetPointPosition(int pointNumber)
        {
            int snaps = GetSnaps();
            int y = (int)Math.Ceiling((double)(pointNumber / (snaps * _pointsRearrangeSnap)));
            int x = pointNumber - (y * snaps * _pointsRearrangeSnap);

            return ref Points.Get(x, y);
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

            int index = 0;
            var neuronModel = networkModel.Layers.First.Neurons.First;

            while (neuronModel != null)
            {
                if (!neuronModel.IsBias)
                {
                    _stat[index] += neuronModel.Activation > _threshold ? neuronModel.Activation : 0;
                }

                ++index;
                neuronModel = neuronModel.Next;
            }
        }
    }
}

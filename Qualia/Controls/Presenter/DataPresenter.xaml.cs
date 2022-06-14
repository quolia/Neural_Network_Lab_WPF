using Qualia.Model;
using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Qualia.Controls
{
    using Point = System.Windows.Point;

    sealed public partial class DataPresenter : UserControl
    {
        private const int CURRENT_POINTS_COUNT = -1;

        public TaskFunction TaskFunction;

        private readonly int _pointSize;
        private int _pointsRearrangeSnap;
        private int _pointsCount;
        private double _threshold;
        private double[] _data;
        private double[] _stat;

        private INetworkTaskChanged _onTaskChanged;

        private readonly System.Windows.Media.Pen _penBlack = Draw.GetPen(in QColors.Black);

        public DataPresenter()
        {
            InitializeComponent();

            _penBlack.Freeze();

            _pointSize = (int)Config.Main.GetInt(Constants.Param.PointSize, 7).Value;

            SizeChanged += DataPresenter_SizeChanged;
            CtlTaskFunction.SetChangeEvent(CtlTask_SelectedIndexChanged);
            CtlInputDataFunction.SetChangeEvent(OnInputDataFunctionChanged);
        }

        private void CtlTask_SelectedIndexChanged()
        {
            if (CtlTaskFunction.SelectedItem == null)
            {
                return;
            }

            TaskFunction = TaskFunction.GetInstance(CtlTaskFunction.SelectedItem);
            _pointsRearrangeSnap = TaskFunction.VisualControl.GetPointsRearrangeSnap();
            TaskFunction.VisualControl.SetChangeEvent(TaskParameterChanged);

            CtlHolder.Children.Clear();

            var controls = TaskFunction.VisualControl.GetVisualControl();
            CtlHolder.Children.Add(controls);

            if (_onTaskChanged != null)
            {
                _onTaskChanged.TaskChanged();
            }
        }

        private void OnInputDataFunctionChanged()
        {
            if (CtlInputDataFunction.SelectedItem == null)
            {
                return;
            }

            TaskFunction.InputDataFunction = InputDataFunction.GetInstance(CtlInputDataFunction.SelectedItem);
        }

        private void DataPresenter_SizeChanged(object sender, EventArgs e)
        {
            if (TaskFunction != null && TaskFunction.VisualControl.IsGridSnapAdjustmentAllowed())
            {
                Rearrange(CURRENT_POINTS_COUNT);
            }
        }

        public void LoadConfig(Config config, INetworkTaskChanged taskChanged)
        {
            Initializer.FillComboBox<TaskFunction>(CtlTaskFunction, config, null);
            TaskFunction = TaskFunction.GetInstance(CtlTaskFunction.SelectedItem);

            var parametersConfig = config.Extend(CtlTaskFunction.SelectedItem);
            Initializer.FillComboBox<InputDataFunction>(CtlInputDataFunction, parametersConfig, nameof(InputDataFunction.FlatRandom));
            TaskFunction.InputDataFunction = InputDataFunction.GetInstance(CtlInputDataFunction.SelectedItem);

            _pointsRearrangeSnap = TaskFunction.VisualControl.GetPointsRearrangeSnap();

            CtlHolder.Children.Clear();
            CtlHolder.Children.Add(TaskFunction.VisualControl.GetVisualControl());

            TaskFunction.VisualControl.SetConfig(config);
            TaskFunction.VisualControl.LoadConfig();
            TaskFunction.VisualControl.SetChangeEvent(TaskParameterChanged);

            CtlInputDataFunction.SetConfig(parametersConfig);
            CtlInputDataFunction.LoadConfig();
            CtlInputDataFunction.SetChangeEvent(OnInputDataFunctionChanged);

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
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));   
            }

            CtlTaskFunction.SetConfig(config);
            TaskFunction.VisualControl.SetConfig(config);

            var parametersConfig = config.Extend(CtlTaskFunction.SelectedValue);
            CtlInputDataFunction.SetConfig(parametersConfig);

            CtlTaskFunction.SaveConfig();
            TaskFunction.VisualControl.SaveConfig();
            CtlInputDataFunction.SaveConfig();

            config.FlushToDrive();
            parametersConfig.FlushToDrive();
        }

        private void DrawPoint(double x, double y, double value, bool isData)
        {
            var brush = value == 0
                        ? System.Windows.Media.Brushes.White
                        : (isData
                           ? Draw.GetBrush(value)
                           : Draw.GetBrush(Draw.GetColor((byte)(255 * value), in QColors.Green)));

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
                _stat = new double[count];
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
            Rearrange(TaskFunction.VisualControl.GetInputCount());
        }

        private int GetSnaps()
        {
            int width = (int)MathX.Max(ActualWidth, _pointsRearrangeSnap * _pointSize);

            return TaskFunction.VisualControl.IsGridSnapAdjustmentAllowed()
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
            int y = (int)MathX.Ceiling((double)(pointNumber / (snaps * _pointsRearrangeSnap)));
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

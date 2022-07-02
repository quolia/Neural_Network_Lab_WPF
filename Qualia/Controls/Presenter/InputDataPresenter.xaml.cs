using Qualia.Model;
using Qualia.Tools;
using System;

namespace Qualia.Controls
{
    using Point = System.Windows.Point;

    sealed public partial class InputDataPresenter : BaseUserControl
    {
        private const int CURRENT_POINTS_COUNT = -1;

        public TaskFunction TaskFunction { get; private set; }

        private readonly int _pointSize;
        private int _pointsRearrangeSnap;
        private int _pointsCount;
        private double _threshold;
        private double[] _data;
        private double[] _stat;

        private readonly System.Windows.Media.Pen _penBlack = Draw.GetPen(in ColorsX.Black);

        public InputDataPresenter()
        {
            InitializeComponent();

            _penBlack.Freeze();
            _pointSize = Config.Main.Get(Constants.Param.PointSize, 7);
            
            SizeChanged += Size_OnChanged;

            _configParams = new()
            {
                CtlTaskFunction
                    .Initialize(nameof(TaskFunction.CountDots))
                    .SetUIParam(Notification.ParameterChanged.TaskFunction),

                CtlInputDataFunction
                    .Initialize(defaultFunction: nameof(InputDataFunction.FlatRandom), defaultParam: 1)
                    .SetUIParam(Notification.ParameterChanged.TaskInputDataFunction)
            };
        }

        private new void OnChanged(Notification.ParameterChanged param)
        {
            if (param == Notification.ParameterChanged.TaskFunction)
            {
                if (CtlTaskFunction.SelectedItem == null)
                {
                    return;
                }

                TaskFunction = TaskFunction.GetInstance(CtlTaskFunction);

                var taskFunctionConfig = _config.Extend(CtlTaskFunction.Name).Extend(CtlTaskFunction.Value);

                TaskFunction.InputDataFunction = CtlInputDataFunction.SetConfig<InputDataFunction>(taskFunctionConfig);
                CtlInputDataFunction.LoadConfig();

                var taskControl = TaskFunction.ITaskControl;

                _pointsRearrangeSnap = taskControl.GetPointsRearrangeSnap();

                taskControl.SetConfig(taskFunctionConfig);
                taskControl.LoadConfig();
                taskControl.SetOnChangeEvent(TaskParameter_OnChanged);

                CtlHolder.Children.Clear();
                CtlHolder.Children.Add(taskControl.GetVisualControl());
            }
            else if (param == Notification.ParameterChanged.TaskInputDataFunction)
            {
                if (CtlInputDataFunction.SelectedFunction == null)
                {
                    return;
                }

                var inputDataFunction = CtlInputDataFunction.GetInstance<InputDataFunction>();

                if (inputDataFunction != TaskFunction.InputDataFunction)
                {
                    TaskFunction.InputDataFunction = CtlInputDataFunction.GetInstance<InputDataFunction>();

                    var taskFunctionConfig = _config.Extend(CtlTaskFunction.Name).Extend(CtlTaskFunction.Value);
                    CtlInputDataFunction.SetConfig(taskFunctionConfig);
                    CtlInputDataFunction.LoadConfig();
                }
            }

            base.OnChanged(Notification.ParameterChanged.TaskInputData);
        }

        private void Size_OnChanged(object sender, EventArgs e)
        {
            if (TaskFunction != null && TaskFunction.ITaskControl.IsGridSnapAdjustmentAllowed())
            {
                Rearrange(CURRENT_POINTS_COUNT);
            }
        }

        public override void SetConfig(Config config)
        {
            _config = config.Extend(Name);
            CtlTaskFunction.SetConfig(_config);
        }

        public override void LoadConfig()
        {
            TaskFunction = CtlTaskFunction.Fill<TaskFunction>(_config);
            LoadTaskFunctionUI();
        }

        private void LoadTaskFunctionUI()
        {
            var taskFunctionConfig = _config.Extend(CtlTaskFunction.Name).Extend(CtlTaskFunction.Value);

            TaskFunction.InputDataFunction = CtlInputDataFunction.SetConfig<InputDataFunction>(taskFunctionConfig);
            CtlInputDataFunction.LoadConfig();

            var taskControl = TaskFunction.ITaskControl;

            _pointsRearrangeSnap = taskControl.GetPointsRearrangeSnap();

            taskControl.SetConfig(taskFunctionConfig);
            taskControl.LoadConfig();
            taskControl.SetOnChangeEvent(TaskParameter_OnChanged);

            CtlHolder.Children.Clear();
            CtlHolder.Children.Add(taskControl.GetVisualControl());
        }

        void TaskParameter_OnChanged(Notification.ParameterChanged _)
        {
            //if (_onTaskChanged == null)
            {
                //return;
            }

            RearrangeWithNewPointsCount();
            //_onTaskChanged.TaskParameter_OnChanged();
        }

        public override void SaveConfig()
        {
            CtlTaskFunction.SaveConfig();
            TaskFunction.ITaskControl.SaveConfig();
            CtlInputDataFunction.SaveConfig();

            _config.FlushToDrive();
        }

        public override void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            //_onChanged -= onChanged;
            //_onChanged += onChanged;

            _configParams.ForEach(p => p.SetOnChangeEvent(onChanged));
        }

        private void DrawPoint(double x, double y, double value, bool isData)
        {
            var brush = value == 0
                        ? System.Windows.Media.Brushes.CornflowerBlue
                        : (isData
                           ? Draw.GetBrush(value)
                           : Draw.GetBrush(Draw.GetColor((byte)(255 * value), in ColorsX.Green)));

            CtlCanvas.DrawRectangle(brush,
                                    _penBlack,
                                    ref Rects.Get(_pointSize * x,
                                                  _pointSize * y,
                                                  _pointSize,
                                                  _pointSize));
        }

        public void SetInputDataAndDraw(NetworkDataModel network)
        {
            _threshold = network.InputThreshold;
            var count = network.Layers.First.Neurons.CountIf(n => !n.IsBias);

            if (_data == null || _data.Length != count)
            {
                _data = new double[count];
                _stat = new double[count];
            }
            else
            {
                Array.Clear(_data, 0, _data.Length);
            }

            var neuron = network.Layers.First.Neurons.First;
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
            Rearrange(TaskFunction.ITaskControl.GetInputCount());
        }

        private int GetSnaps()
        {
            int width = (int)MathX.Max(ActualWidth, _pointsRearrangeSnap * _pointSize);

            return TaskFunction.ITaskControl.IsGridSnapAdjustmentAllowed()
                   ? width / (_pointsRearrangeSnap * _pointSize)
                   : 1;
        }

        private void Rearrange(int pointsCount)
        {
            CtlCanvas.Clear();

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
                    DrawPoint(pointPosition.X,
                              pointPosition.Y,
                              _data[i],
                              true);
                }
                else
                {
                    DrawPoint(pointPosition.X,
                              pointPosition.Y,
                              maxStat > 0 ? _stat[i] / maxStat : 0,
                              false);
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
            int x = pointNumber - y * snaps * _pointsRearrangeSnap;

            return ref Points.Get(x, y);
        }

        public void SetInputStat(NetworkDataModel network)
        {
            if (_stat == null)
            {
                return;
            }

            if (network == null)
            {
                throw new ArgumentNullException(nameof(network));
            }

            int index = 0;
            var neuron = network.Layers.First.Neurons.First;

            while (neuron != null)
            {
                if (!neuron.IsBias)
                {
                    _stat[index] += neuron.Activation > _threshold ? neuron.Activation : 0;
                }

                ++index;
                neuron = neuron.Next;
            }
        }

        public TaskModel GetModel()
        {
            return new()
            {
                TaskFunction = TaskFunction.GetInstance(CtlTaskFunction),
                InputDataFunction = InputDataFunction.GetInstance(CtlInputDataFunction),
                InputDataFunctionParam = CtlInputDataFunction.CtlParam.Value
            };
        }
    }
}

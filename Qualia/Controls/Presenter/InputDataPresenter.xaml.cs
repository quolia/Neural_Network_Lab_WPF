using Qualia.Model;
using Qualia.Tools;
using System;

namespace Qualia.Controls
{
    using Point = System.Windows.Point;

    sealed public partial class InputDataPresenter : BaseUserControl
    {
        private const int CURRENT_POINTS_COUNT = -1;

        public bool IsPreventDataRepetition => CtlIsPreventRepetition.Value;

        private int _pointsRearrangeSnap;
        private bool _isGridSnapAdjustmentAllowed;

        private readonly int _pointSize;
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
                    .Initialize(nameof(TaskFunction.DotsCount))
                    .SetUIParam(Notification.ParameterChanged.TaskFunction),

                CtlDistributionFunction
                    .Initialize(defaultFunction: nameof(DistributionFunction.FlatRandom),
                                defaultParam: 1,
                                paramMinValue: 0,
                                paramMaxValue: 1)
                    .SetUIParam(Notification.ParameterChanged.TaskDistributionFunction,
                                Notification.ParameterChanged.TaskDistributionFunctionParam),

                CtlIsPreventRepetition
                    .Initialize(false)
                    .SetUIParam(Notification.ParameterChanged.IsPreventRepetition)
            };

            CtlTaskFunction.SelectionChanged += TaskFunction_OnChanged;
        }

        private void TaskFunction_OnChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CtlTaskDescription.Text = TaskFunction.GetDescription(CtlTaskFunction.Value);
        }

        private new void OnChanged(Notification.ParameterChanged param)
        {
            if (param == Notification.ParameterChanged.TaskFunction)
            {
                if (CtlTaskFunction.SelectedItem == null)
                {
                    return;
                }

                var taskFunction = TaskFunction.GetInstance(CtlTaskFunction);

                var taskFunctionConfig = _config.Extend(CtlTaskFunction.Name)
                                                .Extend(CtlTaskFunction.Value);

                CtlDistributionFunction.SetConfig<DistributionFunction>(taskFunctionConfig);
                CtlDistributionFunction.LoadConfig();

                CtlIsPreventRepetition.SetConfig(taskFunctionConfig);
                CtlIsPreventRepetition.LoadConfig();

                var taskControl = taskFunction.ITaskControl;

                _pointsRearrangeSnap = taskControl.GetPointsRearrangeSnap();
                _isGridSnapAdjustmentAllowed = taskControl.IsGridSnapAdjustmentAllowed();

                taskControl.SetConfig(taskFunctionConfig);
                taskControl.LoadConfig();
                taskControl.SetOnChangeEvent(TaskParameter_OnChanged);

                CtlTaskControlHolder.Children.Clear();
                CtlTaskControlHolder.Children.Add(taskControl.GetVisualControl());
            }
            else if (param == Notification.ParameterChanged.TaskDistributionFunction)
            {
                if (CtlDistributionFunction.SelectedFunction == null)
                {
                    return;
                }

                var distributionFunction = CtlDistributionFunction.GetInstance<DistributionFunction>();
                
                //if (distributionFunction != TaskFunction.DistributionFunction)
                {
                    //TaskFunction.DistributionFunction = CtlDistributionFunction.GetInstance<DistributionFunction>();

                    var taskFunctionConfig = _config.Extend(CtlTaskFunction.Name)
                                                    .Extend(CtlTaskFunction.Value);

                    CtlDistributionFunction.SetConfig(taskFunctionConfig);
                    CtlDistributionFunction.LoadConfig();

                    CtlIsPreventRepetition.SetConfig(taskFunctionConfig);
                    CtlIsPreventRepetition.LoadConfig();
                }
            }

            base.OnChanged(Notification.ParameterChanged.TaskParameter);
        }

        private void Size_OnChanged(object sender, EventArgs e)
        {
            var model = GetModel();

            //if (TaskFunction != null && TaskFunction.ITaskControl.IsGridSnapAdjustmentAllowed())
            if (model.TaskFunction != null && model.TaskFunction.ITaskControl.IsGridSnapAdjustmentAllowed())
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
            CtlTaskFunction
                .Fill<TaskFunction>(_config);

            var taskFunctionConfig = _config.Extend(CtlTaskFunction.Name)
                                            .Extend(CtlTaskFunction.Value);

            CtlDistributionFunction.SetConfig<DistributionFunction>(taskFunctionConfig);
            CtlDistributionFunction.LoadConfig();

            CtlIsPreventRepetition.SetConfig(taskFunctionConfig);
            CtlIsPreventRepetition.LoadConfig();

            var model = GetModel();

            var taskControl = model.TaskFunction.ITaskControl;

            _pointsRearrangeSnap = taskControl.GetPointsRearrangeSnap();
            _isGridSnapAdjustmentAllowed = taskControl.IsGridSnapAdjustmentAllowed();

            taskControl.SetConfig(taskFunctionConfig);
            taskControl.LoadConfig();
            taskControl.SetOnChangeEvent(TaskParameter_OnChanged);

            CtlTaskControlHolder.Children.Clear();
            CtlTaskControlHolder.Children.Add(taskControl.GetVisualControl());
        }

        void TaskParameter_OnChanged(Notification.ParameterChanged param)
        {
            //if (_onTaskChanged == null)
            {
                //return;
            }

            RearrangeWithNewPointsCount();
            OnChanged(param);
            //_onTaskChanged.TaskParameter_OnChanged();
        }

        public override void SaveConfig()
        {
            CtlTaskFunction.SaveConfig();

            var model = GetModel();

            var taskFunctionConfig = _config.Extend(CtlTaskFunction.Name)
                                            .Extend(CtlTaskFunction.Value);

            model.TaskFunction.ITaskControl.SetConfig(taskFunctionConfig);
            model.TaskFunction.ITaskControl.SaveConfig();
            CtlDistributionFunction.SaveConfig();
            CtlIsPreventRepetition.SaveConfig();

            _config.FlushToDrive();
        }

        public override void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            _onChanged -= onChanged;
            _onChanged += onChanged;

            _configParams.ForEach(p => p.SetOnChangeEvent(OnChanged));
        }

        private void DrawPoint(double x, double y, double value, bool isData)
        {
            var brush = value == 0
                        ? System.Windows.Media.Brushes.CornflowerBlue
                        : (isData
                           ? Draw.GetBrush(value)
                           : Draw.GetBrush(Draw.GetColor((byte)(255 * value),
                                                         in ColorsX.Green)));

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

            var model = GetModel();
            Rearrange(model.TaskFunction.ITaskControl.GetInputCount());
        }

        private int GetSnaps()
        {
            int width = (int)MathX.Max(ActualWidth, _pointsRearrangeSnap * _pointSize);

            return _isGridSnapAdjustmentAllowed
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
            var taskFunction = TaskFunction.GetInstance(CtlTaskFunction);
            var distributionFunction = DistributionFunction.GetInstance(CtlDistributionFunction);
            var distributionFunctionParam = CtlDistributionFunction.CtlParam.Value;
            var solutionsData = taskFunction?.GetSolutionsData();

            if (taskFunction != null)
            {
                taskFunction.DistributionFunction = distributionFunction;
                taskFunction.DistributionFunctionParam = distributionFunctionParam;
            }

            return new()
            {
                TaskFunction = taskFunction,
                DistributionFunction = distributionFunction,
                DistributionFunctionParam = distributionFunctionParam,
                SolutionsData = solutionsData
            };
        }
    }
}

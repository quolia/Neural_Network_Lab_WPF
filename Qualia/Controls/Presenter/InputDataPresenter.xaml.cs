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

        private INetworkTaskChanged _onTaskChanged;

        private readonly System.Windows.Media.Pen _penBlack = Draw.GetPen(in ColorsX.Black);

        public InputDataPresenter()
        {
            InitializeComponent();

            _penBlack.Freeze();

            _pointSize = Config.Main.Get(Constants.Param.PointSize, 7);

            CtlTaskFunction.Initialize(nameof(TaskFunction.CountDots));
            CtlInputDataFunction.Initialize(defaultFunctionName: nameof(InputDataFunction.FlatRandom), defaultParamValue: 1);

            SizeChanged += Presenter_OnSizeChanged;
            CtlTaskFunction.AddChangeEventListener(TaskFunction_OnChanged);
            CtlInputDataFunction.AddChangeEventListener(InputDataFunction_OnChanged);
        }

        private void TaskFunction_OnChanged()
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
            taskControl.AddChangeEventListener(TaskParameter_OnChanged);

            CtlHolder.Children.Clear();
            CtlHolder.Children.Add(taskControl.GetVisualControl());

            _onTaskChanged?.TaskChanged();
        }

        private void InputDataFunction_OnChanged()
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

        private void Presenter_OnSizeChanged(object sender, EventArgs e)
        {
            if (TaskFunction != null && TaskFunction.ITaskControl.IsGridSnapAdjustmentAllowed())
            {
                Rearrange(CURRENT_POINTS_COUNT);
            }
        }

        public override void SetConfig(Config config)
        {
            _config = config;
            CtlTaskFunction.SetConfig(_config);
        }

        public void LoadConfig(INetworkTaskChanged taskChanged)
        {
            TaskFunction = CtlTaskFunction.Fill<TaskFunction>(_config);

            _onTaskChanged = taskChanged;
            TaskParameter_OnChanged();
        }

        void TaskParameter_OnChanged()
        {
            if (_onTaskChanged == null)
            {
                return;
            }

            RearrangeWithNewPointsCount();
            _onTaskChanged.TaskParameter_OnChanged();
        }

        public override void SaveConfig()
        {
            if (_config is null)
            {
                throw new InvalidOperationException(nameof(_config));
            }

            CtlTaskFunction.SaveConfig();
            TaskFunction.ITaskControl.SaveConfig();
            CtlInputDataFunction.SaveConfig();

            _config.FlushToDrive();
        }

        private void DrawPoint(double x, double y, double value, bool isData)
        {
            var brush = value == 0
                        ? System.Windows.Media.Brushes.CornflowerBlue
                        : (isData
                           ? Draw.GetBrush(value)
                           : Draw.GetBrush(Draw.GetColor((byte)(255 * value), in ColorsX.Green)));

            CtlCanvas.DrawRectangle(brush, _penBlack, ref Rects.Get(x * _pointSize, y * _pointSize, _pointSize, _pointSize));
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

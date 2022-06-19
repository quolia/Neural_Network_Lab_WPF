using Qualia.Model;
using Qualia.Tools;
using System;
using System.Windows.Controls;

namespace Qualia.Controls
{
    using Point = System.Windows.Point;

    sealed public partial class InputDataPresenter : UserControl
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

        private readonly System.Windows.Media.Pen _penBlack = Draw.GetPen(in ColorsX.Black);

        public InputDataPresenter()
        {
            InitializeComponent();

            _penBlack.Freeze();

            _pointSize = Config.Main.Get(Constants.Param.PointSize, 7);

            CtlTaskFunction.Initialize(nameof(TaskFunction.CountDots));
            CtlInputDataFunction.Initialize(defaultFunctionName: nameof(InputDataFunction.FlatRandom), defaultParamValue: 1);

            SizeChanged += Presenter_OnSizeChanged;
            CtlTaskFunction.SetChangeEvent(OnTaskChanged);
            CtlInputDataFunction.SetChangeEvent(InputDataFunction_OnChanged);
        }

        private void OnTaskChanged()
        {
            if (CtlTaskFunction.SelectedItem == null)
            {
                return;
            }

            TaskFunction = TaskFunction.GetInstance(CtlTaskFunction);

            _pointsRearrangeSnap = TaskFunction.VisualControl.GetPointsRearrangeSnap();
            TaskFunction.VisualControl.SetChangeEvent(TaskParameter_OnChanged);

            CtlHolder.Children.Clear();

            var control = TaskFunction.VisualControl.GetVisualControl();
            CtlHolder.Children.Add(control);

            _onTaskChanged?.TaskChanged();
        }

        private void InputDataFunction_OnChanged()
        {
            if (CtlInputDataFunction.SelectedFunction.Name == null)
            {
                return;
            }

            TaskFunction.InputDataFunction = CtlInputDataFunction.GetInstance<InputDataFunction>();// InputDataFunction.GetInstance(CtlInputDataFunction);

            //var inputDataFunctionConfig = config.Extend(CtlInputDataFunction.SelectedItem);
            //TaskFunction.InputDataFunctionParam = Config.Main.GetString(Constants.Param.);
        }

        private void Presenter_OnSizeChanged(object sender, EventArgs e)
        {
            if (TaskFunction != null && TaskFunction.VisualControl.IsGridSnapAdjustmentAllowed())
            {
                Rearrange(CURRENT_POINTS_COUNT);
            }
        }

        public void LoadConfig(Config config, INetworkTaskChanged taskChanged)
        {
            TaskFunction = CtlTaskFunction.Fill<TaskFunction>(config);
            var taskFunctionConfig = config.Extend(CtlTaskFunction);

            TaskFunction.InputDataFunction = CtlInputDataFunction.Fill<InputDataFunction>(taskFunctionConfig);
            var parametersConfig = taskFunctionConfig.Extend(CtlInputDataFunction);

            //TaskFunction.InputDataFunctionParam = parametersConfig.Get(FunctionControl.Param);


            _pointsRearrangeSnap = TaskFunction.VisualControl.GetPointsRearrangeSnap();

            CtlHolder.Children.Clear();
            CtlHolder.Children.Add(TaskFunction.VisualControl.GetVisualControl());

            TaskFunction.VisualControl.SetConfig(parametersConfig);
            TaskFunction.VisualControl.LoadConfig();
            TaskFunction.VisualControl.SetChangeEvent(TaskParameter_OnChanged);

            CtlInputDataFunction.SetConfig(parametersConfig);
            CtlInputDataFunction.LoadConfig();
            CtlInputDataFunction.SetChangeEvent(InputDataFunction_OnChanged);

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

        public void SaveConfig(Config config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));   
            }

            CtlTaskFunction.SetConfig(config);
            TaskFunction.VisualControl.SetConfig(config);

            var parametersConfig = config.Extend(CtlTaskFunction);
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

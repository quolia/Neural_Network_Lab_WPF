﻿using System;
using System.Linq;
using Qualia.Controls.Base;
using Qualia.Models;
using Qualia.Tools;
using Qualia.Tools.Functions;
using Qualia.Tools.Managers;

namespace Qualia.Controls.Presenter;

using Point = System.Windows.Point;

public sealed partial class InputDataPresenter : BaseUserControl
{
    public TaskFunction TaskFunction { get; private set; }

    private int _pointsRearrangeSnap;
    private bool _isGridSnapAdjustmentAllowed;

    private readonly int _pointSize;
    private int _pointsCount;
    private double _threshold;

    // Array to keep input values.
    private double[] _data; 

    private double[] _stat;

    private readonly System.Windows.Media.Pen _penBlack = Draw.GetPen(in ColorsX.Black);

    public InputDataPresenter()
        : base(0)
    {
        InitializeComponent();

        _penBlack.Freeze();
        _pointSize = Config.Main.Get(Constants.Param.PointSize, 7);
            
        SizeChanged += Size_OnChanged;

        this.SetConfigParams(new()
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
        });

        CtlTaskFunction.SelectionChanged += TaskFunction_OnChanged;
    }

    private void TaskFunction_OnChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        CtlTaskDescription.Text = TaskFunction.GetDescription(CtlTaskFunction);
    }

    private new void OnChanged(ApplyAction action)
    {
        var param = action.Param;

        if (param == Notification.ParameterChanged.TaskFunction)
        {
            if (CtlTaskFunction.SelectedItem == null)
            {
                return;
            }

            ApplyAction instantAction = new(this, param)
            {
                Apply = (isRunning) =>
                {
                    ApplyChanges();
                },
                ApplyInstant = (isRunning) =>
                {
                    ActionManager.Instance.Lock();

                    var taskFunction = TaskFunction.GetInstance(CtlTaskFunction);

                    var taskFunctionConfig = this.GetConfig().Extend(CtlTaskFunction.Name)
                        .Extend(CtlTaskFunction.Value.Text);

                    CtlDistributionFunction.SetConfig<DistributionFunction>(taskFunctionConfig);
                    CtlDistributionFunction.LoadConfig();

                    CtlIsPreventRepetition.SetConfig(taskFunctionConfig);
                    CtlIsPreventRepetition.LoadConfig();

                    var taskControl = taskFunction.VisualControl;

                    taskControl.SetConfig(taskFunctionConfig);
                    taskControl.LoadConfig();
                    taskControl.SetOnChangeEvent(TaskParameter_OnChanged);

                    CtlTaskControlHolder.Children.Clear();
                    CtlTaskControlHolder.Children.Add(taskControl.GetVisualControl());

                    ActionManager.Instance.Unlock();
                }
            };

            base.OnChanged(instantAction);
        }
        else if (param == Notification.ParameterChanged.TaskDistributionFunction)
        {
            ApplyAction instantAction = new(this, param)
            {
                ApplyInstant = (isRunning) =>
                {
                    if (CtlDistributionFunction.SelectedFunction == null)
                    {
                        return;
                    }

                    var distributionFunction = CtlDistributionFunction.GetInstance<DistributionFunction>();
                    var taskFunctionConfig = this.GetConfig().Extend(CtlTaskFunction.Name)
                        .Extend(CtlTaskFunction.Value.Text);

                    ActionManager.Instance.Lock();

                    CtlDistributionFunction.SetConfig(taskFunctionConfig);
                    CtlDistributionFunction.LoadConfig();

                    ActionManager.Instance.Unlock();
                }
            };

            base.OnChanged(instantAction);
            if (!action.IsActive)
            {
                return;
            }
        }
        else if (param == Notification.ParameterChanged.TaskDistributionFunctionParam)
        {
            if (action.IsActive)
            {
                ApplyAction instantAction = new(this, param)
                {
                    Apply = (isRunning) =>
                    {
                        ApplyChanges();
                    }
                };

                base.OnChanged(instantAction);
            }
            else
            {
                return;
            }
        }

        if (param == Notification.ParameterChanged.Unknown)
        {
            action.Param = Notification.ParameterChanged.TaskParameter;
        }
        base.OnChanged(action);
    }

    private void Size_OnChanged(object sender, EventArgs e)
    {
        var taskFunction = GetTaskFunction();
        if (taskFunction != null && taskFunction.VisualControl.IsGridSnapAdjustmentAllowed())
        {
            Rearrange(_pointsCount);
        }
    }

    public override void SetConfig(Config config)
    {
        this.PutConfig(config.Extend(Name));
        CtlTaskFunction.SetConfig(this.GetConfig());
    }

    public override void LoadConfig()
    {
        CtlTaskFunction
            .Fill<TaskFunction>(this.GetConfig());

        var taskFunctionConfig = this.GetConfig().Extend(CtlTaskFunction.Name)
            .Extend(CtlTaskFunction.Value.Text);

        CtlDistributionFunction.SetConfig<DistributionFunction>(taskFunctionConfig);
        CtlDistributionFunction.LoadConfig();

        CtlIsPreventRepetition.SetConfig(taskFunctionConfig);
        CtlIsPreventRepetition.LoadConfig();

        var taskFunction = GetTaskFunction();
        var taskControl = taskFunction.VisualControl;

        _pointsRearrangeSnap = taskControl.GetPointsRearrangeSnap();
        _isGridSnapAdjustmentAllowed = taskControl.IsGridSnapAdjustmentAllowed();

        taskControl.SetConfig(taskFunctionConfig);
        taskControl.LoadConfig();
        taskControl.SetOnChangeEvent(TaskParameter_OnChanged);

        CtlTaskControlHolder.Children.Clear();
        CtlTaskControlHolder.Children.Add(taskControl.GetVisualControl());
    }

    void TaskParameter_OnChanged(ApplyAction action)
    {
        OnChanged(action);
    }

    public override void SaveConfig()
    {
        CtlTaskFunction.SaveConfig();

        var taskFunctionConfig = this.GetConfig().Extend(CtlTaskFunction.Name)
            .Extend(CtlTaskFunction.Value.Text);

        var taskFunction = GetTaskFunction();
        taskFunction.VisualControl.SetConfig(taskFunctionConfig);
        taskFunction.VisualControl.SaveConfig();

        CtlDistributionFunction.SaveConfig();
        CtlIsPreventRepetition.SaveConfig();

        this.GetConfig().FlushToDrive();
    }

    public override void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChanged)
    {
        this.SetUIHandler(onChanged);
        this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(OnChanged));
    }

    public void SetInputDataAndDraw(NetworkDataModel network)
    {
        _threshold = network.InputInitial0;
        var count = network.Layers.First.Neurons.Count;

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
            _data[neuron.Id] = neuron.Activation;
            neuron = neuron.Next;
        }

        Rearrange(_pointsCount);
    }

    public void RearrangeWithNewPointsCount()
    {
        _data = null;
        Rearrange(TaskFunction.VisualControl.GetInputCount());
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

        var index = 0;
        var neuron = network.Layers.First.Neurons.First;

        while (neuron != null)
        {
            _stat[index] += neuron.Activation > _threshold ? neuron.Activation : 0; //?
            ++index;

            neuron = neuron.Next;
        }
    }

    public TaskFunction GetTaskFunction()
    {
        var taskFunction = TaskFunction.GetInstance(CtlTaskFunction);
        var distributionFunction = DistributionFunction.GetInstance(CtlDistributionFunction);
        var distributionFunctionParam = CtlDistributionFunction.CtlParam.Value;

        if (taskFunction != null)
        {
            taskFunction.DistributionFunction = distributionFunction;
            taskFunction.DistributionFunctionParam = distributionFunctionParam;
        }

        return taskFunction;
    }

    public void ApplyChanges()
    {
        var newFunction = GetTaskFunction();
        var newTaskControl = newFunction.VisualControl;
            
        if (newFunction != TaskFunction || newTaskControl.GetInputCount() != _pointsCount)
        {
            TaskFunction = newFunction;
            TaskFunction.VisualControl.SetIsPreventRepetition(CtlIsPreventRepetition.Value);

            _pointsRearrangeSnap = newTaskControl.GetPointsRearrangeSnap();
            _isGridSnapAdjustmentAllowed = newTaskControl.IsGridSnapAdjustmentAllowed();
            RearrangeWithNewPointsCount();
        }
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
    
    private int GetSnaps()
    {
        var width = (int)MathX.Max(ActualWidth, _pointsRearrangeSnap * _pointSize);

        return _isGridSnapAdjustmentAllowed
            ? width / (_pointsRearrangeSnap * _pointSize)
            : 1;
    }

    private void Rearrange(int pointsCount)
    {
        CtlCanvas.Clear();
        _pointsCount = pointsCount;

        if (_data == null || _data.Length != _pointsCount)
        {
            _data = new double[_pointsCount];
            _stat = new double[_pointsCount];
        }

        var maxStat = GetMaxStat();

        for (var i = 0; i < _pointsCount; ++i)
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
                var statValue = maxStat > 0 ? _stat[i] / maxStat : 0;

                DrawPoint(pointPosition.X,
                    pointPosition.Y,
                    statValue,
                    false);
            }
        }
    }

    private double GetMaxStat()
    {
        if (_stat == null)
        {
            return 0;
        }

        return _stat.Prepend(0).Max();
    }

    private ref Point GetPointPosition(int pointNumber)
    {
        var snaps = GetSnaps();
        var y = (int)MathX.Ceiling((double)(pointNumber / (snaps * _pointsRearrangeSnap)));
        var x = pointNumber - y * snaps * _pointsRearrangeSnap;

        return ref Points.Get(x, y);
    }
}
using Qualia.Tools;
using System;

namespace Qualia.Controls
{
    sealed public partial class CrossCountControl : BaseUserControl
    {
        public CrossCountControl()
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlMinCrossesAmountToCount
                    .Initialize(defaultValue: 0,
                                minValue: 0,
                                maxValue: Constants.SquareRoot - 1)
                    .SetUIParam(Notification.ParameterChanged.TaskParameter),

                CtlMaxCrossesAmoutToCount
                    .Initialize(defaultValue: 10,
                                minValue: 1,
                                maxValue: Constants.SquareRoot)
                    .SetUIParam(Notification.ParameterChanged.TaskParameter),

                CtlNoisePointsAmount
                    .Initialize(defaultValue: 5,
                                minValue: 0,
                                maxValue: Constants.SquareRoot * Constants.SquareRoot / 2)
                    .SetUIParam(Notification.ParameterChanged.TaskParameter)
            };
        }

        public int MaxCrossesAmountToCount => (int)CtlMaxCrossesAmoutToCount.Value;
        public int MinCrossesAmountToCount => (int)CtlMinCrossesAmountToCount.Value;
        public int NoisePointsAmount => (int)CtlNoisePointsAmount.Value;

        private void Parameter_OnChanged(Notification.ParameterChanged param)
        {
            if (IsValid())
            {
                OnChanged(param);
            }
        }

        // IConfigParam

        override public void SetConfig(Config config)
        {
            _configParams.ForEach(p => p.SetConfig(config));
        }

        override public void LoadConfig()
        {
            _configParams.ForEach(p => p.LoadConfig());
        }

        override public void SaveConfig()
        {
            _configParams.ForEach(p => p.SaveConfig());
        }

        override public void RemoveFromConfig()
        {
            _configParams.ForEach(p => p.RemoveFromConfig());
        }

        override public bool IsValid()
        {
            if (_configParams.TrueForAll(p => p.IsValid()))
            {
                if (CtlMaxCrossesAmoutToCount.Value <= Constants.SquareRoot
                    && CtlMaxCrossesAmoutToCount.Value > CtlMinCrossesAmountToCount.Value)
                {
                    return true;
                }

                InvalidateValue();
            }

            return false;
        }

        override public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChange)
        {
            _onChanged = onChange;
            _configParams.ForEach(p => p.SetOnChangeEvent(Parameter_OnChanged));
        }

        override public void InvalidateValue()
        {
            _configParams.ForEach(p => p.InvalidateValue());
        }

        //
    }
}

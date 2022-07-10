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
                    .SetUIParam(Notification.ParameterChanged.TaskParameter)
            };
        }

        public int MaxCrossesAmountToCount => (int)CtlMaxCrossesAmoutToCount.Value;
        public int MinCrossesAmountToCount => (int)CtlMinCrossesAmountToCount.Value;

        private void Parameter_OnChanged(Notification.ParameterChanged param)
        {
            if (IsValid())
            {
                OnChanged(param);
            }
        }

        public override void SetConfig(Config config)
        {
            _configParams.ForEach(p => p.SetConfig(config));
        }

        public override void LoadConfig()
        {
            _configParams.ForEach(p => p.LoadConfig());
        }

        public override void SaveConfig()
        {
            _configParams.ForEach(p => p.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            _configParams.ForEach(p => p.RemoveFromConfig());
        }

        public override bool IsValid()
        {
            if (_configParams.TrueForAll(p => p.IsValid()))
            {
                if (CtlMaxCrossesAmoutToCount.Value <= Constants.SquareRoot
                    && CtlMaxCrossesAmoutToCount.Value >= CtlMinCrossesAmountToCount.Value)
                {
                    return true;
                }

                InvalidateValue();
            }

            return false;
        }

        public override void SetOnChangeEvent(Action<Notification.ParameterChanged> onChange)
        {
            _onChanged -= onChange;
            _onChanged += onChange;

            _configParams.ForEach(p => p.SetOnChangeEvent(Parameter_OnChanged));
        }

        public override void InvalidateValue()
        {
            _configParams.ForEach(p => p.InvalidateValue());
        }
    }
}

using Qualia.Tools;
using System;

namespace Qualia.Controls
{
    sealed public partial class DotsCountControl : BaseUserControl
    {
        public DotsCountControl()
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlCommonDotsAmount
                    .Initialize(defaultValue: 10,
                                minValue: 1,
                                maxValue: Constants.SquareRoot * Constants.SquareRoot)
                    .SetUIParam(Notification.ParameterChanged.TaskParameter),

                CtlMinDotsAmountToCount
                    .Initialize(defaultValue: 0,
                                minValue: 0,
                                maxValue: Constants.SquareRoot - 1)
                    .SetUIParam(Notification.ParameterChanged.TaskParameter),

                CtlMaxDotsAmoutToCount
                    .Initialize(defaultValue: 10,
                                minValue: 1,
                                maxValue: Constants.SquareRoot)
                    .SetUIParam(Notification.ParameterChanged.TaskParameter)
            };
        }

        public int CommonDotsAmount => (int)CtlCommonDotsAmount.Value;
        public int MaxDotsAmountToCount => (int)CtlMaxDotsAmoutToCount.Value;
        public int MinDotsAmountToCount => (int)CtlMinDotsAmountToCount.Value;

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
                if (CtlCommonDotsAmount.Value >= CtlMaxDotsAmoutToCount.Value
                    && CtlMaxDotsAmoutToCount.Value >= CtlMinDotsAmountToCount.Value
                    && CtlMaxDotsAmoutToCount.Value - CtlMinDotsAmountToCount.Value <= Constants.SquareRoot)
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

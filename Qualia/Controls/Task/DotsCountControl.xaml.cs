using Qualia.Tools;
using System;
using System.Collections.Generic;

namespace Qualia.Controls
{
    sealed public partial class DotsCountControl : BaseUserControl
    {
        public DotsCountControl()
        {
            InitializeComponent();

            this.SetConfigParams(new()
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
            });
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

        // IConfigParam

        override public void SetConfig(Config config)
        {
            this.GetConfigParams().ForEach(p => p.SetConfig(config));
        }

        override public void LoadConfig()
        {
            this.GetConfigParams().ForEach(p => p.LoadConfig());
        }

        override public void SaveConfig()
        {
            this.GetConfigParams().ForEach(p => p.SaveConfig());
        }

        override public void RemoveFromConfig()
        {
            this.GetConfigParams().ForEach(p => p.RemoveFromConfig());
        }

        override public bool IsValid()
        {
            if (this.GetConfigParams().TrueForAll(p => p.IsValid()))
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

        override public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChange)
        {
            _onChanged = onChange;
            this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(Parameter_OnChanged));
        }

        override public void InvalidateValue()
        {
            this.GetConfigParams().ForEach(p => p.InvalidateValue());
        }

        //
    }
}

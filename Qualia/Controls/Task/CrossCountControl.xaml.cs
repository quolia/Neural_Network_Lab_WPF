using Qualia.Tools;
using System;
using System.Collections.Generic;

namespace Qualia.Controls
{
    sealed public partial class CrossCountControl : BaseUserControl
    {
        public CrossCountControl()
        {
            InitializeComponent();

            this.SetConfigParams(new()
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
            });
        }

        public int MaxCrossesAmountToCount => (int)CtlMaxCrossesAmoutToCount.Value;
        public int MinCrossesAmountToCount => (int)CtlMinCrossesAmountToCount.Value;
        public int NoisePointsAmount => (int)CtlNoisePointsAmount.Value;

        private void Parameter_OnChanged(Notification.ParameterChanged param, ApplyAction action)
        {
            if (param == Notification.ParameterChanged.Invalidate)
            {
                OnChanged(param, action);
                return;
            }

            bool isValid = IsValid();
            OnChanged(param, action);
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
                if (CtlMaxCrossesAmoutToCount.Value <= Constants.SquareRoot
                    && CtlMaxCrossesAmoutToCount.Value > CtlMinCrossesAmountToCount.Value)
                {
                    return true;
                }
            }

            InvalidateValue();
            return false;
        }

        override public void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChange)
        {
            this.SetUIHandler(onChange);
            this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(Parameter_OnChanged));
        }

        override public void InvalidateValue()
        {
            this.GetConfigParams().ForEach(p => p.InvalidateValue());
        }

        //
    }
}

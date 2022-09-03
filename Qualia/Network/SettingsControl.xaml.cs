using Qualia.Model;
using Qualia.Tools;
using System;

namespace Qualia.Controls
{
    sealed public partial class SettingsControl : BaseUserControl
    {
        public Settings Settings;

        public SettingsControl()
        {
            InitializeComponent();

            this.SetConfigParams(new()
            {
                CtlSkipRoundsToDrawErrorMatrix
                    .Initialize(defaultValue: 10000)
                    .SetUIParam(Notification.ParameterChanged.Settings),

                CtlSkipRoundsToDrawNetworks
                    .Initialize(defaultValue: 10000)
                    .SetUIParam(Notification.ParameterChanged.Settings),

                CtlSkipRoundsToDrawStatistics
                    .Initialize(defaultValue: 10000)
                    .SetUIParam(Notification.ParameterChanged.Settings),

                CtlIsNoSleepMode
                    .Initialize(defaultValue: true)
                    .SetUIParam(Notification.ParameterChanged.NoSleepMode)
            });
        }

        public override void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChanged)
        {
            this.SetUIHandler(onChanged);
            this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(Value_OnChanged));

            ApplyChanges(false);
            Value_OnChanged(Notification.ParameterChanged.NoSleepMode, new(this));
        }

        private void Value_OnChanged(Notification.ParameterChanged param, ApplyAction action)
        {
            if (param == Notification.ParameterChanged.Settings
                || param == Notification.ParameterChanged.NoSleepMode)
            {
                if (param == Notification.ParameterChanged.Settings)
                {
                    //ActionsManager.Instance.Add(GetApplyAction());
                }

                OnChanged(param, action);
            }
            else
            {
                throw new ArgumentException(param.ToString());
            }
        }

        private Settings Get()
        {
            return new()
            {
                SkipRoundsToDrawErrorMatrix = (int)CtlSkipRoundsToDrawErrorMatrix.Value,
                SkipRoundsToDrawNetworks = (int)CtlSkipRoundsToDrawNetworks.Value,
                SkipRoundsToDrawStatistics = (int)CtlSkipRoundsToDrawStatistics.Value,
                IsNoSleepMode = CtlIsNoSleepMode.Value
            };
        }

        public void ApplyChanges(bool isRunning)
        {
            Settings = Get();
        }

        public ApplyAction GetApplyAction(bool isRunning)
        {
            return new(this)
            {
                Apply = (isRunning) => ApplyChanges(isRunning)
            };
        }
    }

    sealed public class Settings
    {
        public int SkipRoundsToDrawErrorMatrix;
        public int SkipRoundsToDrawNetworks;
        public int SkipRoundsToDrawStatistics;
        public bool IsNoSleepMode;
    }
}

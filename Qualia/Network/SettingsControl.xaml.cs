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

        public override void SetOnChangeEvent(ActionsManager.ApplyActionDelegate onChanged)
        {
            this.SetUIHandler(onChanged);
            this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(Value_OnChanged));

            ApplyChanges();
            Value_OnChanged(Notification.ParameterChanged.NoSleepMode, null);
        }

        private void Value_OnChanged(Notification.ParameterChanged param, ApplyAction action)
        {
            if (param == Notification.ParameterChanged.Settings
                || param == Notification.ParameterChanged.NoSleepMode)
            {
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

        public void ApplyChanges()
        {
            Settings = Get();
        }

        public ApplyAction GetApplyAction()
        {
            return new()
            {
                RunningAction = ApplyChanges,
                StandingAction = ApplyChanges
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

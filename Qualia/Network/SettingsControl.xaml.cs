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

        public override void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            this.SetUIHandler(onChanged);
            this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(Value_OnChanged));

            ApplyChanges();
            Value_OnChanged(Notification.ParameterChanged.NoSleepMode);
        }

        private void Value_OnChanged(Notification.ParameterChanged param)
        {
            if (param == Notification.ParameterChanged.Settings
                || param == Notification.ParameterChanged.NoSleepMode)
            {
                OnChanged(param);
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

        public void CancelChanges()
        {
            LoadConfig();    
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

using Qualia.Model;
using Qualia.Tools;
using System;
using System.Linq;

namespace Qualia.Controls
{
    sealed public partial class SettingsControl : BaseUserControl
    {
        public SettingsControl()
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlSkipRoundsToDrawErrorMatrix
                    .Initialize(defaultValue: 10000),

                CtlSkipRoundsToDrawNetworks
                    .Initialize(defaultValue: 10000),

                CtlSkipRoundsToDrawStatistics
                    .Initialize(defaultValue: 10000),

                CtlPreventComputerFromSleep
                    .Initialize(defaultValue: true)
                    .SetUIParam(Notification.ParameterChanged.PreventComputerFromSleep),

                new FakeValue()
            };
        }

        public override void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            _onChanged -= onChanged;
            _onChanged += onChanged;

            _configParams.ForEach(p => p.SetOnChangeEvent(Value_OnChanged));
        }

        private void Value_OnChanged(Notification.ParameterChanged param)
        {
            if (param == Notification.ParameterChanged.Fake)
            {
                OnChanged(Notification.ParameterChanged.Settings);
            }

            if (param == Notification.ParameterChanged.PreventComputerFromSleep)
            {
                SystemTools.SetPreventComputerFromSleep(CtlPreventComputerFromSleep.Value);
            }
        }

        public SettingsModel GetModel()
        {
            return new()
            {
                SkipRoundsToDrawErrorMatrix = (int)CtlSkipRoundsToDrawErrorMatrix.Value,
                SkipRoundsToDrawNetworks = (int)CtlSkipRoundsToDrawNetworks.Value,
                SkipRoundsToDrawStatistics = (int)CtlSkipRoundsToDrawStatistics.Value
            };
        }
    }
}

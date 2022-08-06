using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Qualia.Controls
{
    sealed public class ConfigParamWrapper : BaseUserControl
    {
        public ConfigParamWrapper(FrameworkElement control, List<IConfigParam> configParams = null)
        {
            Name = control.Name;

            this.SetConfigParams(configParams ?? new List<IConfigParam>(control.FindVisualChildren<IConfigParam>()));
        }

        // IConfigParam

        public override void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            _onChanged = onChanged;
            this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(Param_OnChanged));
        }

        //

        private void Param_OnChanged(Notification.ParameterChanged param)
        {
            OnChanged(param == Notification.ParameterChanged.Unknown ? this.GetUIParam() : param);
        }
    }
}


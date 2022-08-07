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
            this.SetConfigParams(configParams ?? new(control.FindVisualChildren<IConfigParam>()));
        }

        // IConfigParam

        public override void SetOnChangeEvent(ActionsManager.ApplyActionDelegate onChanged)
        {
            this.SetUIHandler(onChanged);
            this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(Param_OnChanged));
        }

        //

        private void Param_OnChanged(Notification.ParameterChanged param, ApplyAction action)
        {
            this.InvokeUIHandler(param);
        }
    }
}

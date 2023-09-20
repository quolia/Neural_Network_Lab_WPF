using System.Collections.Generic;
using System.Windows;
using Qualia.Tools;
using Qualia.Tools.Managers;

namespace Qualia.Controls.Base;

public sealed class ConfigParamWrapper : BaseUserControl
{
    public ConfigParamWrapper(FrameworkElement control, List<IConfigParam> configParams = null)
        : base(0)
    {
        Name = control.Name;
        this.SetConfigParams(configParams ?? new(control.FindVisualChildren<IConfigParam>()));
    }

    // IConfigParam

    public override void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChanged)
    {
        this.SetUIHandler(onChanged);
        this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(Param_OnChanged));
    }

    //

    private void Param_OnChanged(ApplyAction action)
    {
        this.InvokeUIHandler(action);
    }
}
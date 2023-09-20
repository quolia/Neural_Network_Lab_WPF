using System.Windows.Controls;
using Qualia.Tools;
using Qualia.Tools.Managers;

namespace Qualia.Controls.Base;

public abstract partial class BaseUserControl(long visualId) : UserControl, IConfigParam
{
    public readonly long VisualId = visualId;

    public void OnChanged(ApplyAction action)
    {
        this.InvokeUIHandler(action);
    }

    // IConfigParam

    public virtual bool IsValid()
    {
        return this.GetConfigParams().TrueForAll(cp => cp.IsValid());
    }

    public virtual void SetConfig(Config config)
    {
        this.PutConfig(config.Extend(Name));
        this.GetConfigParams().ForEach(cp => cp.SetConfig(this.GetConfig()));
    }

    public virtual void LoadConfig()
    {
        this.GetConfigParams().ForEach(cp => cp.LoadConfig());
    }

    public virtual void SaveConfig()
    {
        this.GetConfigParams().ForEach(cp => cp.SaveConfig());
    }

    public virtual void RemoveFromConfig()
    {
        this.GetConfig().Remove(Name);
        this.GetConfigParams().ForEach(cp => cp.RemoveFromConfig());
    }

    public virtual void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChanged)
    {
        this.SetUIHandler(onChanged);
        this.GetConfigParams().ForEach(cp => cp.SetOnChangeEvent(onChanged));
    }

    public virtual void InvalidateValue()
    {
        this.GetConfigParams().ForEach(cp => cp.InvalidateValue());
    }

    //
}
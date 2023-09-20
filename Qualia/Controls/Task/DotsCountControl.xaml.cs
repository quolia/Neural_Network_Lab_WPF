using Qualia.Controls.Base;
using Qualia.Tools;
using Qualia.Tools.Managers;

namespace Qualia.Controls.Task;

public sealed partial class DotsCountControl : BaseUserControl
{
    public DotsCountControl()
        : base(0)
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

    private void Parameter_OnChanged(ApplyAction action)
    {
        if (action.Param == Notification.ParameterChanged.Invalidate)
        {
            InvalidateValue();
            OnChanged(action);
            return;
        }

        var isValid = IsValid();
        if (!isValid)
        {
            action.Param = Notification.ParameterChanged.Invalidate;
        }

        OnChanged(action);
    }

    // IConfigParam

    public override void SetConfig(Config config)
    {
        this.GetConfigParams().ForEach(p => p.SetConfig(config));
    }

    public override void LoadConfig()
    {
        this.GetConfigParams().ForEach(p => p.LoadConfig());
    }

    public override void SaveConfig()
    {
        this.GetConfigParams().ForEach(p => p.SaveConfig());
    }

    public override void RemoveFromConfig()
    {
        this.GetConfigParams().ForEach(p => p.RemoveFromConfig());
    }

    public override bool IsValid()
    {
        if (this.GetConfigParams().TrueForAll(p => p.IsValid()))
        {
            if (CtlCommonDotsAmount.Value >= CtlMaxDotsAmoutToCount.Value
                && CtlMaxDotsAmoutToCount.Value >= CtlMinDotsAmountToCount.Value
                && CtlMaxDotsAmoutToCount.Value - CtlMinDotsAmountToCount.Value <= Constants.SquareRoot)
            {
                return true;
            }
        }
            
        InvalidateValue();
        return false;
    }

    public override void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChange)
    {
        this.SetUIHandler(onChange);
        this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(Parameter_OnChanged));
    }

    public override void InvalidateValue()
    {
        this.GetConfigParams().ForEach(p => p.InvalidateValue());
    }

    //
}
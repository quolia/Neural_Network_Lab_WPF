using Qualia.Network.Base;
using Qualia.Tools;
using Qualia.Tools.Functions;
using Qualia.Tools.Managers;

namespace Qualia.Network.Output;

public sealed partial class OutputNeuronControl : NeuronBaseControl
{
    public OutputNeuronControl(long id, Config config, ActionManager.ApplyActionDelegate onChanged, LayerBaseControl parentLayer)
        : base(id, config, onChanged, parentLayer)
    {
        InitializeComponent();

        this.SetConfigParams(new()
        {
            CtlActivationFunction
                .Initialize(nameof(ActivationFunction.LogisticSigmoid)),

            CtlActivationFunctionParam
                .Initialize(defaultValue: 1),

            CtlLabel,

            CtlPositiveTargetValue
                .Initialize(defaultValue: 1),

            CtlNegativeTargetValue
                .Initialize(defaultValue: 0)
        });

        this.GetConfigParams().ForEach(param => param.SetConfig(Config));

        LoadConfig();

        this.GetConfigParams().ForEach(param => param.SetOnChangeEvent(Neuron_OnChanged));
    }

    public override ActivationFunction ActivationFunction
    {
        get => ActivationFunction.GetInstance(CtlActivationFunction);
        set
        {
            CtlActivationFunction.SelectByText(ActivationFunction.GetNameByInstance(value));
        }
    }
        
    public override double ActivationFunctionParam
    {
        get => CtlActivationFunctionParam.Value;
        set => CtlActivationFunctionParam.Value = value;
    }

    public override double PositiveTargetValue
    {
        get => CtlPositiveTargetValue.Value;
        set => CtlPositiveTargetValue.Value = value;
    }

    public override double NegativeTargetValue
    {
        get => CtlNegativeTargetValue.Value;
        set => CtlNegativeTargetValue.Value = value;
    }

    public override string Label => CtlLabel.Text;

    public override void LoadConfig()
    {
        CtlActivationFunction
            .Fill<ActivationFunction>(Config);

        this.GetConfigParams().ForEach(param => param.LoadConfig());
    }

    private void Neuron_OnChanged(ApplyAction action)
    {
        action.Param = Notification.ParameterChanged.NetworkUpdated;
        OnChanged(action);
    }

    public override void SetOrdinalNumber(int number)
    {
        CtlNumber.Text = Converter.IntToText(number);
    }

    public override bool IsValid()
    {
        return CtlActivationFunctionParam.IsValid();
    }

    public override void SaveConfig()
    {
        this.GetConfigParams().ForEach(param => param.SaveConfig());
    }

    public override void RemoveFromConfig()
    {
        this.GetConfigParams().ForEach(param => param.RemoveFromConfig());
    }
}
using System;
using System.Windows;
using Qualia.Network.Base;
using Qualia.Tools.Functions;

namespace Qualia.Network.Input;

public sealed partial class InputNeuronControl : NeuronBaseControl
{
    public InputNeuronControl(long id, LayerBaseControl parentLayer)
        : base(id, null, null, parentLayer)
    {
        Visibility = Visibility.Collapsed; // Do not show input neuron.
    }

    public override ActivationFunction ActivationFunction { get; set; }
    public override double ActivationFunctionParam { get; set; }

    public override string Label => null;

    public override double PositiveTargetValue
    {
        get => throw new InvalidOperationException("Input neuron has no target value");
        set => throw new InvalidOperationException("Input neuron has no target value");
    }

    public override double NegativeTargetValue
    {
        get => throw new InvalidOperationException("Input neuron has no target value");
        set => throw new InvalidOperationException("Input neuron has no target value");
    }

    public override void SetOrdinalNumber(int number)
    {
        throw new InvalidOperationException("Input neuron has no ordinal number.");
    }

    // IConfigParam

    public override bool IsValid() => true;

    public override void SaveConfig()
    {
        throw new InvalidOperationException("Input neuron has no config.");
    }

    public override void RemoveFromConfig()
    {
        throw new InvalidOperationException("Input neuron has no config.");
    }

    //
}
using System.Runtime.CompilerServices;
using Qualia.Tools;
using Qualia.Tools.Functions;

namespace Qualia.Models;

public sealed class NeuronDataModel : ListXNode<NeuronDataModel>
{
    public readonly int Id;
    public long VisualId;

    public double X; // Value used as a parameter of activation function.
    public double Activation;
    public double Error;

    public ActivationFunction ActivationFunction;
    public double ActivationFunctionParam;

    public readonly ListX<WeightDataModel> Weights;

    public double Target;

    public ListX<ForwardNeuron> WeightsToPreviousLayer;

    public string Label; // Neuron text description.

    public double PositiveTargetValue; // Neuron positive target.
    public double NegativeTargetValue; // Neuron negative target.

    public NeuronDataModel(int id, int weightsCount)
    {
        Weights = new(weightsCount);
        Id = id;
        Range.For(weightsCount, i => Weights.Add(new(i)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double AxW(NeuronDataModel neuron) => Activation * WeightTo(neuron).Weight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WeightDataModel WeightTo(NeuronDataModel neuron) => Weights[neuron.Id];
}

public sealed class WeightDataModel(int id) : ListXNode<WeightDataModel>
{
    public readonly int Id = id;
    public double Weight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(double weight) => Weight += weight;
}

public sealed class ForwardNeuron(NeuronDataModel neuron, WeightDataModel weight) : ListXNode<ForwardNeuron>
{
    public readonly NeuronDataModel Neuron = neuron;
    public readonly WeightDataModel Weight = weight;

    public double AxW => Neuron.Activation * Weight.Weight;
}
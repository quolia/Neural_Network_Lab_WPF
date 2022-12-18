using Qualia.Tools;
using System.Runtime.CompilerServices;

namespace Qualia.Model
{
    sealed public class NeuronDataModel : ListXNode<NeuronDataModel>
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

    sealed public class WeightDataModel : ListXNode<WeightDataModel>
    {
        public readonly int Id;
        public double Weight;
        
        public WeightDataModel(int id)
        {
            Id = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(double weight) => Weight += weight;
    }

    sealed public class ForwardNeuron : ListXNode<ForwardNeuron>
    {
        public readonly NeuronDataModel Neuron;
        public readonly WeightDataModel Weight;

        public ForwardNeuron(NeuronDataModel neuron, WeightDataModel weight)
        {
            Neuron = neuron;
            Weight = weight;
        }

        public double AxW => Neuron.Activation * Weight.Weight;
    }
}

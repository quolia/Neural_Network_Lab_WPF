using System.Runtime.CompilerServices;
using Tools;

namespace Qualia
{
    sealed public class NeuronDataModel : ListXNode<NeuronDataModel>
    {
        public readonly int Id;
        public long VisualId;

        public double Activation;
        public double Error;

        public bool IsBias;
        public bool IsBiasConnected;

        public InitializeMode ActivationInitializer;
        public double? ActivationInitializerParam;

        public InitializeMode WeightsInitializer;
        public double? WeightsInitializerParam;

        public ActivationFunction ActivationFunction;
        public double? ActivationFuncParam;

        public readonly ListX<WeightDataModel> Weights;

        public double Target;

        public ListX<ForwardNeuron> WeightsToNextLayer;

        public NeuronDataModel(int id, int weightsCount)
        {
            Weights = new(weightsCount);
            Id = id;
            Range.For(weightsCount, i => Weights.Add(new(i)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double AxW(NeuronDataModel neuronModel) => Activation * WeightTo(neuronModel).Weight;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WeightDataModel WeightTo(NeuronDataModel neuronModel) => Weights[neuronModel.Id];
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
        public readonly WeightDataModel WeightModel;

        public ForwardNeuron(NeuronDataModel neuron, WeightDataModel weight)
        {
            Neuron = neuron;
            WeightModel = weight;
        }

        public double AxW => Neuron.Activation * WeightModel.Weight;
    }
}

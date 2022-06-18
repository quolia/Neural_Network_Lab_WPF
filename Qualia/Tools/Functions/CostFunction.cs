using Qualia.Model;
using System.Runtime.CompilerServices;
using static Qualia.Tools.CostFunction;

namespace Qualia.Tools
{
    unsafe public class CostFunction : BaseFunction<CostFunction>
    {
        //public override string DefaultFunction => nameof(MeanSquaredError);


        public readonly delegate*<NetworkDataModel, double> Do;
        public readonly delegate*<NetworkDataModel, NeuronDataModel, double> Derivative;

        public CostFunction(delegate*<NetworkDataModel, double> doFunc, delegate*<NetworkDataModel, NeuronDataModel, double> doDerivative)
            : base(nameof(MeanSquaredError))
        {
            Do = doFunc;
            Derivative = doDerivative;
        }

        unsafe sealed public class MeanSquaredError
        {
            public static readonly string Description = "f(network) = network.layers.last.neurons.sum((neuron.target - neuron.activation) ^ 2)";
            public static readonly string DerivativeDescription = "f(network)' = network.layers.last.neurons(neuron.target - neuron.activation)";

            public static readonly CostFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(NetworkDataModel networkModel)
            {
                double sum = 0;
                var neuronModel = networkModel.Layers.Last.Neurons.First;

                while (neuronModel != null)
                {
                    var diff = neuronModel.Target - neuronModel.Activation;
                    sum += diff * diff;

                    neuronModel = neuronModel.Next;
                }

                return sum;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(NetworkDataModel networkModel, NeuronDataModel neuronModel)
            {
                return neuronModel.Target - neuronModel.Activation;
            }
        }
    }
}

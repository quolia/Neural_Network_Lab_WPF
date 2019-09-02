using ILGPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace Qualia
{
    public class NeuronDataModel : ListNode<NeuronDataModel>
    {
        public int Id;
        public long VisualId;

        public double Activation;
        public double Error;

        public bool IsBias;
        public bool IsBiasConnected;

        public string ActivationInitializer;
        public double? ActivationInitializerParamA;

        public string WeightsInitializer;
        public double? WeightsInitializerParamA;

        public IActivationFunction ActivationFunction;
        public double? ActivationFuncParamA;

        public ListX<WeightDataModel> Weights;

        public double Target;

        public ListX<ForwardNeuron> ForwardHelper;

        public NeuronDataModel(int id, int weightsCount)
        {
            Weights = new ListX<WeightDataModel>(weightsCount);
            Id = id;
            Range.For(weightsCount, n => Weights.Add(new WeightDataModel(n)));
        }

        public double AxW(NeuronDataModel neuron) => Activation * WeightTo(neuron).Weight;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WeightDataModel WeightTo(NeuronDataModel neuron) => Weights[neuron.Id];
    }

    public class WeightDataModel : ListNode<WeightDataModel>
    {
        public int Id;
        public double Weight;

        public WeightDataModel(int id)
        {
            Id = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(double w) => Weight += w;
    }
}

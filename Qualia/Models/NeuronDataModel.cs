using System.Runtime.CompilerServices;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double AxW(NeuronDataModel neuron) => Activation * WeightTo(neuron).Weight;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WeightDataModel WeightTo(NeuronDataModel neuron) => Weights[neuron.Id];

        
        /*
        public override int GetHashCode()
        {
            return (int)VisualId;
        }

        public override bool Equals(object obj)
        {
            return VisualId == (obj as NeuronDataModel).VisualId;
        }
        */
    }

    public class WeightDataModel : ListNode<WeightDataModel>
    {
        private static int s_uniqId = 0;

        public int Id;
        public double Weight;

        //private int _uniqId;
        public int _uniqId;

        public WeightDataModel(int id)
        {
            Id = id;
            _uniqId = s_uniqId++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(double w) => Weight += w;

        /*
        public override int GetHashCode()
        {
            return _uniqId;
        }

        public override bool Equals(object obj)
        {
            return _uniqId == (obj as WeightDataModel)._uniqId;
        }
        */
    }
}

using System.Linq;
using ILGPU;
using ILGPU.Runtime;
using Tools;

namespace Qualia
{
    public class LayerDataModel : ListNode<LayerDataModel>
    {
        public int Id;
        public long VisualId;
        public ListX<NeuronDataModel> Neurons;
        public ListX<NeuronDataModel> ShuffledNeurons;

        private MemoryBuffer2D<double> _neurons;

        public LayerDataModel(int id, int neuronsCount, int weightsCount)
        {
            Neurons = new ListX<NeuronDataModel>(neuronsCount);
            Id = id;
            Range.For(neuronsCount, neuronId => Neurons.Add(new NeuronDataModel(neuronId, weightsCount)));
            ShuffledNeurons = new ListX<NeuronDataModel>(Neurons.Where(neuronModel => !neuronModel.IsBias));
        }

        public int Height => Neurons.Count;
    }
}

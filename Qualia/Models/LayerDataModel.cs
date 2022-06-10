using System.Linq;
using Tools;

namespace Qualia
{
    public class LayerDataModel : ListXNode<LayerDataModel>
    {
        public int Id;
        public long VisualId;
        public ListX<NeuronDataModel> Neurons;
        
        public LayerDataModel(int id, int neuronsCount, int weightsCountPerNeuron)
        {
            Neurons = new ListX<NeuronDataModel>(neuronsCount);
            Id = id;
            Range.For(neuronsCount, neuronId => Neurons.Add(new NeuronDataModel(neuronId, weightsCountPerNeuron)));
        }

        public int NeuronsCount => Neurons.Count;
    }
}

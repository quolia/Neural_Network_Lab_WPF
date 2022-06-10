using System.Linq;
using Tools;

namespace Qualia
{
    public class LayerDataModel : ListXNode<LayerDataModel>
    {
        public long VisualId;

        public readonly int Id;
        public readonly ListX<NeuronDataModel> Neurons;
        
        public LayerDataModel(int id, int neuronsCount, int weightsCountPerNeuron)
        {
            Neurons = new ListX<NeuronDataModel>(neuronsCount);
            Id = id;
            Range.For(neuronsCount, neuronId => Neurons.Add(new NeuronDataModel(neuronId, weightsCountPerNeuron)));
        }
    }
}

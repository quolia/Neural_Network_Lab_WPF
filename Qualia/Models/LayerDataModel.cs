using Qualia.Tools;

namespace Qualia.Model
{
    sealed public class LayerDataModel : ListXNode<LayerDataModel>
    {
        public long VisualId;

        public readonly int Id;
        public readonly ListX<NeuronDataModel> Neurons;
        
        public LayerDataModel(int id, int neuronsCount, int weightsCountPerNeuron)
        {
            Neurons = new(neuronsCount);
            Id = id;

            Range.For(neuronsCount, neuronId => Neurons.Add(new(neuronId, weightsCountPerNeuron)));
        }
    }
}

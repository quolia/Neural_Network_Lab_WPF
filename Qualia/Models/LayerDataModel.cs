using Qualia.Tools;

namespace Qualia.Models;

public sealed class LayerDataModel : ListXNode<LayerDataModel>
{
    public long VisualId;

    public readonly int Id;
    public readonly ListX<NeuronDataModel> Neurons;

    public bool IsOutputLayer => Next == null;
    public bool IsInputLayer => Previous == null;
        
    public LayerDataModel(int id, int neuronsCount, int weightsCountPerNeuron)
    {
        Neurons = new(neuronsCount);
        Id = id;

        Range.For(neuronsCount, neuronId => Neurons.Add(new(neuronId, weightsCountPerNeuron)));
    }
}
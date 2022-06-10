using System.Linq;
using Tools;

namespace Qualia
{
    public class LayerDataModel : ListXNode<LayerDataModel>
    {
        public int Id;
        public long VisualId;
        public ListX<NeuronDataModel> Neurons;
        
        private NeuronDataModel[] _shuffledNeurons;

        public LayerDataModel(int id, int neuronsCount, int weightsCountPerNeuron)
        {
            Neurons = new ListX<NeuronDataModel>(neuronsCount);
            Id = id;
            Range.For(neuronsCount, neuronId => Neurons.Add(new NeuronDataModel(neuronId, weightsCountPerNeuron)));

            var noneBias = Neurons.Where(neuronModel => !neuronModel.IsBias).ToList();
            _shuffledNeurons = new NeuronDataModel[noneBias.Count];// ListX<NeuronDataModel>(Neurons.Where(neuronModel => !neuronModel.IsBias));
            noneBias.CopyTo(_shuffledNeurons);
        }

        public NeuronDataModel[] GetShuffledNeurons()
        {
            int n = _shuffledNeurons.Length;
            while (n > 1)
            {
                n--;
                int k = Rand.Flat.Next(n + 1);
                (_shuffledNeurons[k], _shuffledNeurons[n]) = (_shuffledNeurons[n], _shuffledNeurons[k]);
            }

            return _shuffledNeurons;
        }

        public int NeuronsCount => Neurons.Count;
    }
}

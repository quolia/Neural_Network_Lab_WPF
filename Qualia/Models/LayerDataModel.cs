using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace Qualia
{
    public class LayerDataModel : ListNode<LayerDataModel>
    {
        public int Id;
        public long VisualId;
        public ListX<NeuronDataModel> Neurons = new ListX<NeuronDataModel>();

        public LayerDataModel(int id, int neuronsCount, int weightsCount)
        {
            Id = id;
            Range.For(neuronsCount, n => Neurons.Add(new NeuronDataModel(n, weightsCount)));
        }

        public int Height => Neurons.Count;
        public int Width => Neurons.First().Weights.Count();

        public int BiasCount => Neurons.Count(n => n.IsBias);

        public void ClearErrors()
        {
            foreach (var neuron in Neurons)
            {
                neuron.Error = 0;
            }
        }
    }
}

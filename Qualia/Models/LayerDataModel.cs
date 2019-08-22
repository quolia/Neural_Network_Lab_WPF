using ILGPU;
using ILGPU.Runtime;
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
        public ListX<NeuronDataModel> Neurons;

        public LayerDataModel(int id, int neuronsCount, int weightsCount)
        {
            Neurons = new ListX<NeuronDataModel>(neuronsCount);
            Id = id;
            Range.For(neuronsCount, n => Neurons.Add(new NeuronDataModel(n, weightsCount)));
        }

        MemoryBuffer2D<double> _neurons;
        public MemoryBuffer2D<double> GetNeurons()
        {
            if (_neurons != null)
                return _neurons;

            var neurons = GPU.Instance.Accelerator.Allocate<double>(1 + 1 + 1 + Neurons[0].Weights.Count, Neurons.Count); // 1 activation + 1 is bias + 1 is bias connected
            for (int y = 0; y < Neurons.Count; ++y)
            {
                neurons.CopyFrom(Neurons[y].Activation, new Index2(0, y));

                neurons.CopyFrom(Neurons[y].IsBias ? 1 : 0, new Index2(1, y));
                neurons.CopyFrom(Neurons[y].IsBiasConnected ? 1 : 0, new Index2(2, y));

                for (int x = 0; x < Neurons[0].Weights.Count; ++x)
                {
                    neurons.CopyFrom(Neurons[y].Weights[x].Weight, new Index2(3 + x, y));
                }               
            }

            _neurons = neurons;
            return neurons;
        }

        public int Height => Neurons.Count;
        public int BiasCount => Neurons.Count(n => n.IsBias);
    }
}

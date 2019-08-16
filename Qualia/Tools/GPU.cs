using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;
using Qualia;

namespace Tools
{
    public class GPU : IDisposable
    {
        public static GPU Instance = new GPU();

        Context Context;
        Accelerator Accelerator;

        Dictionary<int, List<MemoryBuffer<double>>> Buffers;

        Action<Index, ArrayView<double>, ArrayView<double>, ArrayView<double>> KernelProduct;
        Action<Index, ArrayView<double>, ArrayView<double>> KernelSum;

        public GPU()
        {
            Buffers = new Dictionary<int, List<MemoryBuffer<double>>>();

            Context = new Context();
            var acceleratorId = Accelerator.Accelerators.First(a => a.AcceleratorType == AcceleratorType.Cuda);
            Accelerator = Accelerator.Create(Context, acceleratorId);

            KernelProduct = Accelerator.LoadAutoGroupedStreamKernel<Index, ArrayView<double>, ArrayView<double>, ArrayView<double>>(Product);
            KernelSum = Accelerator.LoadAutoGroupedStreamKernel<Index, ArrayView<double>, ArrayView<double>>(Sum);

        }

        private MemoryBuffer<double> GetBuffer(int size)
        {
            if (!Buffers.ContainsKey(size))
            {
                Buffers.Add(size, new List<MemoryBuffer<double>>());
                Buffers[size].Add(Accelerator.Allocate<double>(size));
            }

            var list = Buffers[size];
            MemoryBuffer<double> buffer;
            if (list.Count == 0)
            {
                buffer = Accelerator.Allocate<double>(size);
            }
            else
            {
                buffer = list.First();
                list.RemoveAt(0);
            }

            return buffer;
        }

        private void ReleaseBuffer(MemoryBuffer<double> buffer)
        {
            Buffers[buffer.Length].Add(buffer);
        }

        public void Dispose()
        {
            Accelerator.Dispose();
            Context.Dispose();

            foreach (var key in Buffers.Keys)
            {
                var list = Buffers[key];
                foreach (var buff in list)
                {
                    buff.Dispose();
                }
            }
        }

        private static void Product(Index index, ArrayView<double> a, ArrayView<double> w, ArrayView<double> s)
        {
            s[index] = a[index] * w[index];
        }

        private static void Sum(Index index, ArrayView<double> s, ArrayView<double> r)
        {
            double sum = 0;
            for (int i = 0; i < s.Length; ++i)
            {
                sum += s[i];
            }
            r[0] = sum;
        }

        public double SumActivation(ListX<NeuronDataModel> neurons, NeuronDataModel nextNeuron)
        {
            var a = GetBuffer(neurons.Count);
            var w = GetBuffer(neurons.Count);
            var s = GetBuffer(neurons.Count);
            var r = GetBuffer(1);

            var neuron = neurons.First();
            while (neuron != null)
            {
                a.CopyFrom(neuron.Activation, neuron.Id);
                w.CopyFrom(neuron.WeightTo(nextNeuron).Weight, neuron.Id);
                neuron = neuron.Next;
            }

            KernelProduct(a.Length, a.View, w.View, s.View);
            Accelerator.Synchronize();

            KernelSum(1, s.View, r.View);
            Accelerator.Synchronize();

            r.CopyTo(out double sum, 0);
            return sum;
        }
    }
}

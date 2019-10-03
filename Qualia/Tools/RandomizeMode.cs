using Qualia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Tools
{
    public static class RandomizeMode
    {
        public static void FlatRandom(NetworkDataModel network, double? a)
        {
            foreach (var layer in network.Layers)
                foreach (var neuron in layer.Neurons)
                    foreach (var weight in neuron.Weights)
                    {
                        weight.Weight = Rand.GetFlatRandom(a.HasValue ? a.Value : 1);
                    }
        }

        public static void GaussRandom(NetworkDataModel network, double? a)
        {
            foreach (var layer in network.Layers)
                foreach (var neuron in layer.Neurons)
                    foreach (var weight in neuron.Weights)
                    {
                        weight.Weight = Rand.GaussianRand.NextGaussian(0, a.HasValue ? a.Value : 1);
                    }
        }

        public static void AbsGaussRandom(NetworkDataModel network, double? a)
        {
            foreach (var layer in network.Layers)
                foreach (var neuron in layer.Neurons)
                    foreach (var weight in neuron.Weights)
                    {
                        weight.Weight = Math.Abs(Rand.GaussianRand.NextGaussian(0, a.HasValue ? a.Value : 1));
                    }
        }

        public static void Centered(NetworkDataModel network, double? a)
        {
            if (!a.HasValue)
                a = 1;

            foreach (var layer in network.Layers)
                foreach (var neuron in layer.Neurons)
                    foreach (var weight in neuron.Weights)
                    {
                        weight.Weight = -a.Value / 2 + a.Value * Rand.GetFlatRandom();
                    }
        }

        public static void WaveProgress(NetworkDataModel network, double? a)
        {
            if (!a.HasValue)
                a = 1;

            foreach (var layer in network.Layers)
                foreach (var neuron in layer.Neurons)
                    foreach (var weight in neuron.Weights)
                    {
                        weight.Weight = a.Value * InitializeMode.Centered(layer.Id + 1) * Math.Cos(weight.Id / Math.PI) * Math.Cos(neuron.Id / Math.PI);
                    }
        }

        public static void Xavier(NetworkDataModel network, double? a)
        {
            if (!a.HasValue)
                a = 1;

            foreach (var layer in network.Layers)
                foreach (var neuron in layer.Neurons)
                    foreach (var weight in neuron.Weights)
                    {
                        if (layer.Previous == null)
                        {
                            weight.Weight = Rand.GetFlatRandom();
                        }
                        else
                        {
                            weight.Weight = Rand.GetFlatRandom(a.Value) * Math.Sqrt(1 / (double)layer.Previous.Neurons.Count);
                        }
                    }
        }

        public static void GaussXavier(NetworkDataModel network, double? a)
        {
            // Xavier initialization works better for layers with sigmoid activation.

            if (!a.HasValue)
                a = 1;

            foreach (var layer in network.Layers)
                foreach (var neuron in layer.Neurons)
                    foreach (var weight in neuron.Weights)
                    {
                        if (layer.Previous == null)
                        {
                            weight.Weight = Rand.GaussianRand.NextGaussian(0, a.Value);
                        }
                        else
                        {
                            weight.Weight = Rand.GaussianRand.NextGaussian(0, a.Value) * Math.Sqrt(1 / (double)layer.Previous.Neurons.Count);
                        }
                    }
        }

        public static void HeEtAl(NetworkDataModel network, double? a)
        {
            if (!a.HasValue)
                a = 1;

            foreach (var layer in network.Layers)
                foreach (var neuron in layer.Neurons)
                    foreach (var weight in neuron.Weights)
                    {
                        if (layer.Previous == null)
                        {
                            weight.Weight = Rand.GetFlatRandom();
                        }
                        else
                        {
                            weight.Weight = Rand.GetFlatRandom(a.Value) * Math.Sqrt(2 / (double)layer.Previous.Neurons.Count);
                        }
                    }
        }

        public static void GaussHeEtAl(NetworkDataModel network, double? a)
        {
            // He initialization works better for layers with ReLu(s) activation.

            if (!a.HasValue)
                a = 1;

            foreach (var layer in network.Layers)
                foreach (var neuron in layer.Neurons)
                    foreach (var weight in neuron.Weights)
                    {
                        if (layer.Previous == null)
                        {
                            weight.Weight = Rand.GaussianRand.NextGaussian(0, a.Value);
                        }
                        else
                        {
                            weight.Weight = Rand.GaussianRand.NextGaussian(0, a.Value) * Math.Sqrt(2 / (double)layer.Previous.Neurons.Count);
                        }
                    }
        }

        public static void Constant(NetworkDataModel network, double? a)
        {
            if (!a.HasValue)
            {
                a = 0;
            }

            foreach (var layer in network.Layers)
            {
                foreach (var neuron in layer.Neurons)
                {
                    foreach (var weight in neuron.Weights)
                    {
                        weight.Weight = a.Value;
                    }
                }
            }
        }

        public static class Helper
        {
            public static string[] GetItems()
            {
                return typeof(RandomizeMode).GetMethods().Where(r => r.IsStatic).Select(r => r.Name).ToArray();
            }

            public static void Invoke(string name, NetworkDataModel N, double? a)
            {
                var method = typeof(RandomizeMode).GetMethod(name);
                method.Invoke(null, new object[] { N, a });
            }

            public static void FillComboBox(ComboBox cb, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(RandomizeMode.Helper), cb, config, cb.Name, defaultValue);
            }
        }
    }
}
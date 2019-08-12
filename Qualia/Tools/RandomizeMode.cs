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
        public static void Random(NetworkDataModel network, double? a)
        {
            if (!a.HasValue)
            {
                a = 1;
            }

            foreach (var layer in network.Layers)
            {
                foreach (var neuron in layer.Neurons)
                {
                    foreach (var weight in neuron.Weights)
                    {
                        weight.Weight = a.Value * Rand.GetFlatRandom();
                    }
                }
            }
        }

        public static void Centered(NetworkDataModel network, double? a)
        {
            if (!a.HasValue)
            {
                a = 1;
            }

            foreach (var layer in network.Layers)
            {
                foreach (var neuron in layer.Neurons)
                {
                    foreach (var weight in neuron.Weights)
                    {
                        weight.Weight = -a.Value / 2 + a.Value * Rand.GetFlatRandom();
                    }
                }
            }
        }

        public static void WaveProgress(NetworkDataModel network, double? a)
        {
            if (!a.HasValue)
            {
                a = 6;
            }

            foreach (var layer in network.Layers)
            {
                foreach (var neuron in layer.Neurons)
                {
                    foreach (var weight in neuron.Weights)
                    {

                        weight.Weight = InitializeMode.Centered(layer.Id + 1) * Math.Cos(weight.Id / Math.PI) * Math.Cos(neuron.Id / Math.PI);

                    }
                }
            }
        }

        public static void Xavier(NetworkDataModel network, double? a)
        {
            if (!a.HasValue)
            {
                a = 1;
            }

            foreach (var layer in network.Layers)
            {
                foreach (var neuron in layer.Neurons)
                {
                    foreach (var weight in neuron.Weights)
                    {
                        if (layer.Previous == null)
                        {
                            weight.Weight = Rand.GetFlatRandom();
                        }
                        else
                        {
                            weight.Weight = a.Value * Rand.GetFlatRandom() * Math.Sqrt(1 / (double)layer.Previous.Neurons.Count);
                        }
                    }
                }
            }
        }

        public static void He_et_al(NetworkDataModel network, double? a)
        {
            if (!a.HasValue)
            {
                a = 1;
            }

            foreach (var layer in network.Layers)
            {
                foreach (var neuron in layer.Neurons)
                {
                    foreach (var weight in neuron.Weights)
                    {
                        if (layer.Previous == null)
                        {
                            weight.Weight = Rand.GetFlatRandom();
                        }
                        else
                        {
                            weight.Weight = a.Value * Rand.GetFlatRandom() * Math.Sqrt(2 / (double)layer.Previous.Neurons.Count);
                        }
                    }
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
using Qualia;
using System;
using System.Linq;
using System.Windows.Controls;

namespace Tools
{
    public static class RandomizeMode
    {
        public static void FlatRandom(NetworkDataModel networkModel, double? param)
        {
            param = param ?? 1;

            var layer = networkModel.Layers.First;
            while (layer != null)
            {
                var neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    var weight = neuron.Weights.First;
                    while (weight != null)
                    {
                        weight.Weight = Rand.GetFlatRandom(param.Value);
                        weight = weight.Next;
                    }

                    neuron = neuron.Next;
                }

                layer = layer.Next;
            }
        }

        public static void GaussRandom(NetworkDataModel networkModel, double? param)
        {
            param = param ?? 1;

            var layer = networkModel.Layers.First;
            while (layer != null)
            {
                var neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    var weight = neuron.Weights.First;
                    while (weight != null)
                    {
                        weight.Weight = Rand.GaussianRand.NextGaussian(0, param.Value);
                        weight = weight.Next;
                    }

                    neuron = neuron.Next;
                }

                layer = layer.Next;
            }
        }

        public static void AbsGaussRandom(NetworkDataModel networkModel, double? param)
        {
            param = param ?? 1;

            var layer = networkModel.Layers.First;
            while (layer != null)
            {
                var neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    var weight = neuron.Weights.First;
                    while (weight != null)
                    {
                        weight.Weight = QMath.Abs(Rand.GaussianRand.NextGaussian(0, param.Value));
                        weight = weight.Next;
                    }

                    neuron = neuron.Next;
                }

                layer = layer.Next;
            }
        }

        public static void Centered(NetworkDataModel networkModel, double? param)
        {
            param = param ?? 1;

            var layer = networkModel.Layers.First;
            while (layer != null)
            {
                var neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    var weight = neuron.Weights.First;
                    while (weight != null)
                    {
                        weight.Weight = -param.Value / 2 + param.Value * Rand.GetFlatRandom();
                        weight = weight.Next;
                    }

                    neuron = neuron.Next;
                }

                layer = layer.Next;
            }
        }

        public static void WaveProgress(NetworkDataModel networkModel, double? param)
        {
            param = param ?? 1;

            var layer = networkModel.Layers.First;
            while (layer != null)
            {
                var neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    var weight = neuron.Weights.First;
                    while (weight != null)
                    {
                        weight.Weight = param.Value * InitializeMode.Centered(layer.Id + 1) * Math.Cos(weight.Id / Math.PI) * Math.Cos(neuron.Id / Math.PI);
                        weight = weight.Next;
                    }

                    neuron = neuron.Next;
                }

                layer = layer.Next;
            }
        }

        public static void Xavier(NetworkDataModel networkModel, double? param)
        {
            param = param ?? 1;

            var layer = networkModel.Layers.First;
            while (layer != null)
            {
                var neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    var weight = neuron.Weights.First;
                    while (weight != null)
                    {
                        if (layer.Previous == null)
                        {
                            weight.Weight = Rand.GetFlatRandom();
                        }
                        else
                        {
                            weight.Weight = Rand.GetFlatRandom(param.Value) * Math.Sqrt(1 / (double)layer.Previous.Neurons.Count);
                        }

                        weight = weight.Next;
                    }

                    neuron = neuron.Next;
                }

                layer = layer.Next;
            }
        }

        public static void GaussXavier(NetworkDataModel networkModel, double? param)
        {
            // Xavier initialization works better for layers with sigmoid activation.

            param = param ?? 1;

            var layer = networkModel.Layers.First;
            while (layer != null)
            {
                var neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    var weight = neuron.Weights.First;
                    while (weight != null)
                    {
                        if (layer.Previous == null)
                        {
                            weight.Weight = Rand.GaussianRand.NextGaussian(0, param.Value);
                        }
                        else
                        {
                            weight.Weight = Rand.GaussianRand.NextGaussian(0, param.Value) * Math.Sqrt(1 / (double)layer.Previous.Neurons.Count);
                        }

                        weight = weight.Next;
                    }

                    neuron = neuron.Next;
                }

                layer = layer.Next;
            }
        }

        public static void HeEtAl(NetworkDataModel networkModel, double? param)
        {
            param = param ?? 1;

            var layer = networkModel.Layers.First;
            while (layer != null)
            {
                var neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    var weight = neuron.Weights.First;
                    while (weight != null)
                    {
                        if (layer.Previous == null)
                        {
                            weight.Weight = Rand.GetFlatRandom();
                        }
                        else
                        {
                            weight.Weight = Rand.GetFlatRandom(param.Value) * Math.Sqrt(2 / (double)layer.Previous.Neurons.Count);
                        }

                        weight = weight.Next;
                    }

                    neuron = neuron.Next;
                }

                layer = layer.Next;
            }
        }

        public static void GaussHeEtAl(NetworkDataModel networkModel, double? param)
        {
            // He initialization works better for layers with ReLu(s) activation.

            param = param ?? 1;

            var layer = networkModel.Layers.First;
            while (layer != null)
            {
                var neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    var weight = neuron.Weights.First;
                    while (weight != null)
                    {
                        if (layer.Previous == null)
                        {
                            weight.Weight = Rand.GaussianRand.NextGaussian(0, param.Value);
                        }
                        else
                        {
                            weight.Weight = Rand.GaussianRand.NextGaussian(0, param.Value) * Math.Sqrt(2 / (double)layer.Previous.Neurons.Count);
                        }

                        weight = weight.Next;
                    }

                    neuron = neuron.Next;
                }

                layer = layer.Next;
            }
        }

        public static void Constant(NetworkDataModel networkModel, double? param)
        {
            param = param ?? 0;

            var layer = networkModel.Layers.First;
            while (layer != null)
            {
                var neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    var weight = neuron.Weights.First;
                    while (weight != null)
                    {
                        weight.Weight = param.Value;
                        weight = weight.Next;
                    }

                    neuron = neuron.Next;
                }

                layer = layer.Next;
            }
        }

        public static class Helper
        {
            public static string[] GetItems()
            {
                return typeof(RandomizeMode).GetMethods().Where(methodInfo => methodInfo.IsStatic).Select(methodInfo => methodInfo.Name).ToArray();
            }

            public static void Invoke(string methodName, NetworkDataModel networkModel, double? param)
            {
                var method = typeof(RandomizeMode).GetMethod(methodName);
                method.Invoke(null, new object[] { networkModel, param });
            }

            public static void FillComboBox(ComboBox comboBox, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(Helper), comboBox, config, comboBox.Name, defaultValue);
            }
        }
    }
}
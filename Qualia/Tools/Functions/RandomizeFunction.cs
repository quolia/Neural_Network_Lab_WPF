using Qualia.Model;
using System;
using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    unsafe public class RandomizeFunction : BaseFunction<RandomizeFunction>
    {
        public readonly delegate*<NetworkDataModel, double, void> Do;

        public RandomizeFunction(delegate*<NetworkDataModel, double, void> doFunc)
            : base(defaultValue: nameof(FlatRandom))
        {
            Do = doFunc;
        }

        unsafe sealed public class FlatRandom
        {
            public static readonly string Description = "weigth(a) = a * random.flat[0, 1), (a=1 -> max value)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double a = 1)
            {
                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = Rand.RandomFlat.NextDouble() * a;
                            weight = weight.Next;
                        }

                        neuron = neuron.Next;
                    }

                    layer = layer.Next;
                }
            }
        }

        unsafe sealed public class GaussNormal
        {
            public static readonly string Description = "weigth(a) => " + InitializeFunction.GaussNormal.Description;

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double a = 0.17)
            {
                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = Rand.Gauss.GetNormal(0, a);
                            weight = weight.Next;
                        }

                        neuron = neuron.Next;
                    }

                    layer = layer.Next;
                }
            }
        }

        unsafe sealed public class GaussNormalModule
        {
            public static readonly string Description = "weigth(a) = |random.gauss.normal(0, a)|, (a=0.17 -> sigma)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double a = 0.17)
            {
                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = MathX.Abs(Rand.Gauss.GetNormal(0, a));
                            weight = weight.Next;
                        }

                        neuron = neuron.Next;
                    }

                    layer = layer.Next;
                }
            }
        }

        unsafe sealed public class Centered
        {
            public static readonly string Description = "weigth(a) = -a / 2 + a * random.flat[0, 1), (a=1 -> max value)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double a = 1)
            {
                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = -a / 2 + a * Rand.RandomFlat.NextDouble();
                            weight = weight.Next;
                        }

                        neuron = neuron.Next;
                    }

                    layer = layer.Next;
                }
            }
        }

        unsafe sealed public class WaveProgress
        {
            public static readonly string Description = "weigth(a) = a * (centered(layer.id + 1) * cos(weight.id / pi) * cos(neuron.id / pi)), (a=1 -> max value)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double a = 1)
            {
                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = a * InitializeFunction.Centered.Do(layer.Id + 1) * Math.Cos(weight.Id / Math.PI) * Math.Cos(neuron.Id / Math.PI);
                            weight = weight.Next;
                        }

                        neuron = neuron.Next;
                    }

                    layer = layer.Next;
                }
            }
        }

        unsafe sealed public class Xavier
        {
            public static readonly string Description = "weigth(a) = a * random.flat[0, 1) * sqrt(1 / layer.previous.neurons.count), (a=1 -> max value)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double a = 1)
            {
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
                                weight.Weight = Rand.RandomFlat.NextDouble();
                            }
                            else
                            {
                                weight.Weight = a * Rand.RandomFlat.NextDouble() * Math.Sqrt(1 / (double)layer.Previous.Neurons.Count);
                            }

                            weight = weight.Next;
                        }

                        neuron = neuron.Next;
                    }

                    layer = layer.Next;
                }
            }
        }

        unsafe sealed public class GaussXavier
        {
            public static readonly string Description = "weigth(a) = random.gauss.normal(0, a) * sqrt(1 / layer.previous.neurons.count), (a=0.17 -> sigma)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double a = 0.17)
            {
                // Xavier initialization works better for layers with sigmoid activation.

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
                                weight.Weight = Rand.Gauss.GetNormal(0, a);
                            }
                            else
                            {
                                weight.Weight = Rand.Gauss.GetNormal(0, a) * Math.Sqrt(1 / (double)layer.Previous.Neurons.Count);
                            }

                            weight = weight.Next;
                        }

                        neuron = neuron.Next;
                    }

                    layer = layer.Next;
                }
            }
        }

        unsafe sealed public class HeEtAl
        {
            public static readonly string Description = "weigth(a) = a * random.flat[0, 1) * sqrt(2 / layer.previous.neurons.count), (a=1 -> max value)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double a = 1)
            {
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
                                weight.Weight = Rand.RandomFlat.NextDouble();
                            }
                            else
                            {
                                weight.Weight = a * Rand.RandomFlat.NextDouble() * Math.Sqrt(2 / (double)layer.Previous.Neurons.Count);
                            }

                            weight = weight.Next;
                        }

                        neuron = neuron.Next;
                    }

                    layer = layer.Next;
                }
            }
        }

        unsafe sealed public class GaussHeEtAl
        {
            public static readonly string Description = "weigth(a) = random.gauss.normal(0, a) * sqrt(2 / layer.previous.neurons.count), (a=0.17 -> sigma)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double a = 0.17)
            {
                // He initialization works better for layers with ReLu(s) activation.

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
                                weight.Weight = Rand.Gauss.GetNormal(0, a);
                            }
                            else
                            {
                                weight.Weight = Rand.Gauss.GetNormal(0, a) * Math.Sqrt(2 / (double)layer.Previous.Neurons.Count);
                            }

                            weight = weight.Next;
                        }

                        neuron = neuron.Next;
                    }

                    layer = layer.Next;
                }
            }
        }

        unsafe sealed public class Constant
        {
            public static readonly string Description = "weigth(a) = a, (a=1 -> constant)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double a = 1)
            {
                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = a;
                            weight = weight.Next;
                        }

                        neuron = neuron.Next;
                    }

                    layer = layer.Next;
                }
            }
        }
    }
}
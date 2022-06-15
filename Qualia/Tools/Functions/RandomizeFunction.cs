using Qualia.Model;
using System;
using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    unsafe public class RandomizeFunction : BaseFunction<RandomizeFunction>
    {
        public readonly delegate*<NetworkDataModel, double?, void> Do;

        public RandomizeFunction(delegate*<NetworkDataModel, double?, void> doFunc)
            : base(defaultValue: nameof(FlatRandom))
        {
            Do = doFunc;
        }

        unsafe sealed public class FlatRandom
        {
            public static readonly string Description = "Weigth = random[0, a), (a = 1)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? upperBound = 1)
            {
                upperBound ??= 1;

                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = Rand.GetFlatRandom(upperBound.Value);
                            weight = weight.Next;
                        }

                        neuron = neuron.Next;
                    }

                    layer = layer.Next;
                }
            }
        }

        unsafe sealed public class GaussRandom
        {
            public static readonly string Description = "Weigth = gaussian.random(0, a), (a = sigma = 0.17)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? sigma = 0.17)
            {
                sigma ??= 0.17;

                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = Rand.GaussianRand.NextGaussian(0, sigma.Value);
                            weight = weight.Next;
                        }

                        neuron = neuron.Next;
                    }

                    layer = layer.Next;
                }
            }
        }

        unsafe sealed public class AbsGaussRandom
        {
            public static readonly string Description = "Weigth = |gaussian.random(0, a)|, (a = sigma = 0.17)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? sigma = 0.17)
            {
                sigma ??= 0.17;

                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = MathX.Abs(Rand.GaussianRand.NextGaussian(0, sigma.Value));
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
            public static readonly string Description = "Weight = -a / 2 + a * random[0, 1), (a = 1)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? width = 1)
            {
                width ??= 1;

                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = -width.Value / 2 + width.Value * Rand.GetFlatRandom();
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
            public static readonly string Description = "Weight = a * (centered(layerId + 1) * cos(weightId / pi) * cos(neuronId / pi)), (a = 1)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? multyplier = 1)
            {
                multyplier ??= 1;

                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = multyplier.Value * InitializeFunction.Centered.Do(layer.Id + 1) * Math.Cos(weight.Id / Math.PI) * Math.Cos(neuron.Id / Math.PI);
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
            public static readonly string Description = "Weight = random[0, a) * sqrt(1 / layer.Previous.Neurons.Count), (a = 1)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? upperBound = 1)
            {
                upperBound ??= 1;

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
                                weight.Weight = Rand.GetFlatRandom(upperBound.Value) * Math.Sqrt(1 / (double)layer.Previous.Neurons.Count);
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
            public static readonly string Description = "Weight = gaussian.random(0, a) * sqrt(1 / layer.Previous.Neurons.Count), (a = sigms = 0.17)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? sigma = 0.17)
            {
                // Xavier initialization works better for layers with sigmoid activation.

                sigma ??= 0.17;

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
                                weight.Weight = Rand.GaussianRand.NextGaussian(0, sigma.Value);
                            }
                            else
                            {
                                weight.Weight = Rand.GaussianRand.NextGaussian(0, sigma.Value) * Math.Sqrt(1 / (double)layer.Previous.Neurons.Count);
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
            public static readonly string Description = "Weight = random[0, a) * sqrt(2 / layer.Previous.Neurons.Count), (a = 1)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? upperBound = 1)
            {
                upperBound ??= 1;

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
                                weight.Weight = Rand.GetFlatRandom(upperBound.Value) * Math.Sqrt(2 / (double)layer.Previous.Neurons.Count);
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
            public static readonly string Description = "Weight = gaussian.random[0, a) * sqrt(2 / layer.Previous.Neurons.Count), (a = sigma = 0.17)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? sigma = 0.17)
            {
                // He initialization works better for layers with ReLu(s) activation.

                sigma ??= 0.17 ;

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
                                weight.Weight = Rand.GaussianRand.NextGaussian(0, sigma.Value);
                            }
                            else
                            {
                                weight.Weight = Rand.GaussianRand.NextGaussian(0, sigma.Value) * Math.Sqrt(2 / (double)layer.Previous.Neurons.Count);
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
            public static readonly string Description = "Weight = a, (a = 1)";

            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? value = 1)
            {
                value ??= 1;

                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = value.Value;
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
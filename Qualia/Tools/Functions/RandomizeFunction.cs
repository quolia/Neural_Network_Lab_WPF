using Qualia.Model;
using System;
using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    unsafe public class RandomizeFunction : BaseFunction<RandomizeFunction>
    {
        public delegate*<NetworkDataModel, double?, void> Do;

        public RandomizeFunction(delegate*<NetworkDataModel, double?, void> doFunc)
        {
            Do = doFunc;
        }

        unsafe sealed public class FlatRandom
        {
            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? param)
            {
                param ??= 1;

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
        }

        unsafe sealed public class GaussRandom
        {
            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? param)
            {
                param ??= 1;

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
        }

        unsafe sealed public class AbsGaussRandom
        {
            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? param)
            {
                param ??= 1;

                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = MathX.Abs(Rand.GaussianRand.NextGaussian(0, param.Value));
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
            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? param)
            {
                param ??= 1;

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
        }

        unsafe sealed public class WaveProgress
        {
            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? param)
            {
                param ??= 1;

                var layer = networkModel.Layers.First;
                while (layer != null)
                {
                    var neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        var weight = neuron.Weights.First;
                        while (weight != null)
                        {
                            weight.Weight = param.Value * InitializeFunction.Centered.Do(layer.Id + 1) * Math.Cos(weight.Id / Math.PI) * Math.Cos(neuron.Id / Math.PI);
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
            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? param)
            {
                param ??= 1;

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
        }

        unsafe sealed public class GaussXavier
        {
            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? param)
            {
                // Xavier initialization works better for layers with sigmoid activation.

                param ??= 1;

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
        }

        unsafe sealed public class HeEtAl
        {
            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? param)
            {
                param ??= 1;

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
        }

        unsafe sealed public class GaussHeEtAl
        {
            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? param)
            {
                // He initialization works better for layers with ReLu(s) activation.

                param ??= 1;

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
        }

        unsafe sealed public class Constant
        {
            public static readonly RandomizeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, double? param)
            {
                param ??= 0;

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
        }
    }
}
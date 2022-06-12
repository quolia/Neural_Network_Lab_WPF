using Qualia.Model;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Qualia.Tools
{
    unsafe public class RandomizeFunction
    {
        public delegate*<NetworkDataModel, double?, void> Do;

        public RandomizeFunction(delegate*<NetworkDataModel, double?, void> doFunc)
        {
            Do = doFunc;
        }
    }

    public static class RandomizeFunctionList
    {
        unsafe sealed public class FlatRandom : RandomizeFunction
        {
            public static readonly FlatRandom Instance = new();

            private FlatRandom()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void _do(NetworkDataModel networkModel, double? param)
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

        unsafe sealed public class GaussRandom : RandomizeFunction
        {
            public static readonly GaussRandom Instance = new();

            private GaussRandom()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void _do(NetworkDataModel networkModel, double? param)
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

        unsafe sealed public class AbsGaussRandom : RandomizeFunction
        {
            public static readonly AbsGaussRandom Instance = new();

            private AbsGaussRandom()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void _do(NetworkDataModel networkModel, double? param)
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

        unsafe sealed public class Centered : RandomizeFunction
        {
            public static readonly Centered Instance = new();

            private Centered()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void _do(NetworkDataModel networkModel, double? param)
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

        unsafe sealed public class WaveProgress : RandomizeFunction
        {
            public static readonly WaveProgress Instance = new();

            private WaveProgress()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void _do(NetworkDataModel networkModel, double? param)
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
                            weight.Weight = param.Value * InitializeFunctionList.Centered.Instance.Do(layer.Id + 1) * Math.Cos(weight.Id / Math.PI) * Math.Cos(neuron.Id / Math.PI);
                            weight = weight.Next;
                        }

                        neuron = neuron.Next;
                    }

                    layer = layer.Next;
                }
            }
        }

        unsafe sealed public class Xavier : RandomizeFunction
        {
            public static readonly Xavier Instance = new();

            private Xavier()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void _do(NetworkDataModel networkModel, double? param)
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

        unsafe sealed public class GaussXavier : RandomizeFunction
        {
            public static readonly GaussXavier Instance = new();

            private GaussXavier()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void _do(NetworkDataModel networkModel, double? param)
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

        unsafe sealed public class HeEtAl : RandomizeFunction
        {
            public static readonly HeEtAl Instance = new();

            private HeEtAl()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void _do(NetworkDataModel networkModel, double? param)
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

        unsafe sealed public class GaussHeEtAl : RandomizeFunction
        {
            public static readonly GaussHeEtAl Instance = new();

            private GaussHeEtAl()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void _do(NetworkDataModel networkModel, double? param)
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

        unsafe sealed public class Constant : RandomizeFunction
        {
            public static readonly Constant Instance = new();

            private Constant()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void _do(NetworkDataModel networkModel, double? param)
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

        public static string[] GetItems()
        {
            return typeof(RandomizeFunctionList)
                .GetNestedTypes()
                .Where(type => typeof(RandomizeFunction).IsAssignableFrom(type))
                .Select(type => type.Name)
                .ToArray();
        }

        public static RandomizeFunction GetInstance(string functionName)
        {
            return (RandomizeFunction)typeof(RandomizeFunctionList)
                .GetNestedTypes()
                .Where(type => type.Name == functionName)
                .First()
                .GetField("Instance")
                .GetValue(null);
        }
    }
}
using Qualia.Controls;
using Qualia.Model;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Qualia.Tools
{
    public interface INetworkTask : IConfigParam
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Control GetVisualControl();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetInputCount();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        List<string> GetClasses();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ApplyChanges();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsGridSnapAdjustmentAllowed();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetPointsRearrangeSnap();
    }

    public interface INetworkTaskChanged
    {
        void TaskChanged();
        void TaskParameterChanged();
    }

    unsafe public class TaskFunction : BaseFunction<TaskFunction>
    {
        public delegate*<NetworkDataModel, void> Do;
        public INetworkTask NetworkTask;

        public TaskFunction(delegate*<NetworkDataModel, void> doFunc, INetworkTask networkTask)
        {
            Do = doFunc;
            NetworkTask = networkTask;
        }

        sealed public class CountDots : INetworkTask
        {
            public static readonly TaskFunction Instance = new(&Do, new CountDots());

            private static readonly CountDotsControl s_control = new();

            private static bool _isGaussianDistribution;
            private static int _minNumber;
            private static int _maxNumber;
            private static double _median;

            public Control GetVisualControl() => s_control;

            public int GetPointsRearrangeSnap() => 10;

            public bool IsGridSnapAdjustmentAllowed() => true;

            public void ApplyChanges()
            {
                _isGaussianDistribution = s_control.IsGaussianDistribution;
                _minNumber = s_control.MinNumber;
                _maxNumber = s_control.MaxNumber;
                _median = ((double)_maxNumber + _minNumber) / 2;
            }

            public void SetConfig(Config config)
            {
                s_control.SetConfig(config);
            }

            public void LoadConfig()
            {
                s_control.LoadConfig();
                ApplyChanges();
            }

            public int GetInputCount() => s_control.InputCount;

            public void SaveConfig() => s_control.SaveConfig();

            public List<string> GetClasses()
            {
                List<string> classes = new();
                for (int number = s_control.MinNumber; number <= s_control.MaxNumber; ++number)
                {
                    classes.Add(number.ToString());
                }

                return classes;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel)
            {
                int randNumber;

                if (!_isGaussianDistribution)
                {
                    randNumber = Rand.Flat.Next(0, _maxNumber + 1 - _minNumber);
                }
                else
                {
                    randNumber = (int)MathX.Round(Rand.GaussianRand.NextGaussian(_median, (_median - 2) / 2));

                    if (randNumber < _minNumber)
                    {
                        randNumber = _minNumber;
                    }
                    else if (randNumber > _maxNumber)
                    {
                        randNumber = _maxNumber;
                    }
                }

                networkModel.TargetOutput = randNumber;

                var neurons = networkModel.Layers.First.Neurons;
                var neuron = neurons.First;

                while (neuron != null)
                {
                    if (!neuron.IsBias)
                    {
                        neuron.Activation = networkModel.InputInitial0;
                    }

                    neuron = neuron.Next;
                }

                while (randNumber > 0)
                {
                    var active = neurons[Rand.Flat.Next(neurons.Count)];

                    while (active.Activation == networkModel.InputInitial1 || active.IsBias)
                    {
                        active = active.Next;
                        if (active == null)
                        {
                            active = neurons.First;
                        }
                    }

                    active.Activation = networkModel.InputInitial1;
                    --randNumber;
                }

                neuron = networkModel.Layers.Last.Neurons.First;
                while (neuron != null)
                {
                    neuron.Target = (neuron.Id == networkModel.TargetOutput) ? 1 : 0;
                    neuron = neuron.Next;
                }
            }

            public void VanishConfig() => s_control.VanishConfig();

            public bool IsValid() => s_control.IsValid();

            public void SetChangeEvent(Action onChanged) => s_control.SetChangeEvent(onChanged);

            public void InvalidateValue() => throw new InvalidOperationException();
        }

        sealed public class MNIST : INetworkTask
        {
            public static readonly TaskFunction Instance = new(&Do, new MNIST());

            private static readonly MNISTControl s_control = new();

            public Control GetVisualControl() => s_control;

            public int GetPointsRearrangeSnap() => 28;

            public bool IsGridSnapAdjustmentAllowed() => false;

            public void ApplyChanges()
            {
                //
            }

            public void SetConfig(Config config) => s_control.SetConfig(config);

            public void LoadConfig()
            {
                s_control.LoadConfig();
                ApplyChanges();
            }

            public void SaveConfig() => s_control.SaveConfig();

            public int GetInputCount() => 28 * 28;

            public List<string> GetClasses()
            {
                List<string> classes = new();
                for (int number = s_control.MinNumber; number <= s_control.MaxNumber; ++number)
                {
                    classes.Add(number.ToString());
                }

                return classes;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel)
            {
                var image = s_control.Images[Rand.Flat.Next(s_control.Images.Count)];
                var count = networkModel.Layers.First.Neurons.Count;

                for (int i = 0; i < count; ++i)
                {
                    networkModel.Layers.First.Neurons[i].Activation = networkModel.InputInitial1 * image.Image[i];
                }

                var neuron = networkModel.Layers.Last.Neurons.First;
                while (neuron != null)
                {
                    neuron.Target = (neuron.Id == image.Label) ? 1 : 0;
                    neuron = neuron.Next;
                }

                networkModel.TargetOutput = image.Label;
            }

            public void VanishConfig() => s_control.VanishConfig();

            public bool IsValid() => s_control.IsValid();

            public void SetChangeEvent(Action onChanged) => s_control.SetChangeEvent(onChanged);

            public void InvalidateValue() => throw new InvalidOperationException();
        }
    }
}

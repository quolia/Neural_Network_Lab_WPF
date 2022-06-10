using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Qualia;
using Qualia.Controls;

namespace Tools
{
    public interface INetworkTask : IConfigParam
    {
        void Do(NetworkDataModel networkModel);
        Control GetVisualControl();
        int GetInputCount();
        List<string> GetClasses();
        void ApplyChanges();
        bool IsGridSnapAdjustmentAllowed();
        int GetPointsRearrangeSnap();
    }

    public interface INetworkTaskChanged
    {
        void TaskChanged();
        void TaskParameterChanged();
    }

    public static class NetworkTask
    {
        sealed public class CountDots : INetworkTask
        {
            private Config _config;

            public static INetworkTask Instance = new CountDots();
            
            private static readonly CountDotsControl s_control = new CountDotsControl();

            private bool _isGaussianDistribution;
            private int _minNumber;
            private int _maxNumber;

            public Control GetVisualControl()
            {
                return s_control;
            }

            public int GetPointsRearrangeSnap()
            {
                return 10;
            }

            public bool IsGridSnapAdjustmentAllowed()
            {
                return true;
            }

            public void ApplyChanges()
            {
                _isGaussianDistribution = s_control.IsGaussianDistribution;
                _minNumber = s_control.MinNumber;
                _maxNumber = s_control.MaxNumber;
            }

            public void SetConfig(Config config)
            {
                _config = config;
                s_control.SetConfig(config);
            }

            public void LoadConfig()
            {
                s_control.LoadConfig();
                ApplyChanges();
            }

            public void SaveConfig()
            {
                s_control.SaveConfig();
            }

            public int GetInputCount()
            {
                return s_control.InputCount;
            }

            public List<string> GetClasses()
            {
                var classes = new List<string>();
                for (int number = s_control.MinNumber; number <= s_control.MaxNumber; ++number)
                {
                    classes.Add(number.ToString());
                }

                return classes;
            }

            public void Do(NetworkDataModel networkModel)
            {
                var shuffled = networkModel.Layers.First.GetShuffledNeurons();

                int randNumber;

                if (_isGaussianDistribution)
                {
                    double median = ((double)_maxNumber + _minNumber) / 2;
                    randNumber = (int)Math.Round(Rand.GaussianRand.NextGaussian(median, (median - 2) / 2));

                    if (randNumber < _minNumber)
                    {
                        randNumber = _minNumber;
                    }
                    if (randNumber > _maxNumber)
                    {
                        randNumber = _maxNumber;
                    }
                }
                else
                {
                    randNumber = Rand.Flat.Next(_minNumber, _maxNumber + 1);
                }

                for (int ind = 0; ind < shuffled.Length; ++ind)
                {
                    shuffled[ind].Activation = ind < randNumber ? networkModel.InputInitial1 : networkModel.InputInitial0;
                }

                var neuron = networkModel.Layers.Last.Neurons.First;
                while (neuron != null)
                {
                    neuron.Target = (neuron.Id == randNumber - _minNumber) ? 1 : 0;
                    neuron = neuron.Next;
                }

                networkModel.TargetOutput = randNumber - _minNumber;
            }

            public void VanishConfig()
            {
                s_control.VanishConfig();
            }

            public bool IsValid()
            {
                return s_control.IsValid();
            }

            public void SetChangeEvent(Action onChanged)
            {
                s_control.SetChangeEvent(onChanged);
            }

            public void InvalidateValue() => throw new InvalidOperationException();
        }

        sealed public class MNIST : INetworkTask
        {
            public static INetworkTask Instance = new MNIST();
            
            private static MNISTControl s_control = new MNISTControl();

            public Control GetVisualControl()
            {
                return s_control;
            }

            public int GetPointsRearrangeSnap()
            {
                return 28;
            }

            public bool IsGridSnapAdjustmentAllowed()
            {
                return false;
            }

            public void ApplyChanges()
            {
                //
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

            public void SaveConfig()
            {
                s_control.SaveConfig();
            }

            public int GetInputCount()
            {
                return 28 * 28;
            }

            public List<string> GetClasses()
            {
                var classes = new List<string>();
                for (int number = s_control.MinNumber; number <= s_control.MaxNumber; ++number)
                {
                    classes.Add(number.ToString());
                }

                return classes;
            }

            public void Do(NetworkDataModel networkModel)
            {
                var image = s_control.Images[Rand.Flat.Next(s_control.Images.Count)];

                for (int ind = 0; ind < networkModel.Layers[0].Neurons.Count; ++ind)
                {
                    networkModel.Layers[0].Neurons[ind].Activation = networkModel.InputInitial1 * image.Image[ind];
                }

                var neuron = networkModel.Layers.Last.Neurons[0];
                while (neuron != null)
                {
                    neuron.Target = (neuron.Id == image.Label) ? 1 : 0;
                    neuron = neuron.Next;
                }

                networkModel.TargetOutput = image.Label;
            }

            public void VanishConfig()
            {
                s_control.VanishConfig();
            }

            public bool IsValid() => s_control.IsValid();

            public void SetChangeEvent(Action onChanged)
            {
                s_control.SetChangeEvent(onChanged);
            }

            public void InvalidateValue() => throw new InvalidOperationException();
        }
        public static class Helper
        {
            public static string[] GetItems()
            {
                return typeof(NetworkTask).GetNestedTypes().Where(task => typeof(INetworkTask).IsAssignableFrom(task)).Select(task => task.Name).ToArray();
            }

            public static INetworkTask GetInstance(string taskName)
            {
                return (INetworkTask)typeof(NetworkTask).GetNestedTypes().Where(c => c.Name == taskName).First().GetField("Instance").GetValue(null);
            }

            public static void FillComboBox(ComboBox comboBox, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(Helper), comboBox, config, comboBox.Name, defaultValue);
            }
        }
    }

    public static class NetworkTaskResult
    {
        public static class Helper
        {
            public static double Invoke(string methodName, double param)
            {
                var method = typeof(NetworkTaskResult).GetMethod(methodName);
                return (double)method.Invoke(null, new object[] { param });
            }
        }
    }
}

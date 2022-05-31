using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Qualia;
using Qualia.Controls;

namespace Tools
{
    public interface INetworkTask : IConfigValue
    {
        void Do(NetworkDataModel model);
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
        public class CountDots : INetworkTask
        {
            private Config _config;

            public static INetworkTask Instance = new CountDots();
            static readonly CountDotsControl s_control = new CountDotsControl();

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
                for (int i = s_control.MinNumber; i <= s_control.MaxNumber; ++i)
                {
                    classes.Add(i.ToString());
                }
                return classes;
            }

            public void Do(NetworkDataModel model)
            {
                var shuffled = model.Layers[0].ShuffledNeurons;
                shuffled.Shuffle();

                if (_isGaussianDistribution)
                {
                    double median = ((double)_minNumber + _maxNumber) / 2;

                    var number = (int)Math.Round(Rand.GaussianRand.NextGaussian(median, median / 2));
                    if (number < _minNumber)
                    {
                        number = _minNumber;
                    }
                    if (number > _maxNumber)
                    {
                        number = _maxNumber;
                    }

                    for (int i = 0; i < shuffled.Count; ++i)
                    {
                        shuffled[i].Activation = i < number ? model.InputInitial1 : model.InputInitial0;
                    }

                    var neuron = model.Layers.Last().Neurons[0];
                    while (neuron != null)
                    {
                        neuron.Target = (neuron.Id == number - _minNumber) ? 1 : 0;
                        neuron = neuron.Next;
                    }

                    model.TargetOutput = number - _minNumber;
                }
                else
                {
                    var number = Rand.Flat.Next(_minNumber, _maxNumber + 1);

                    for (int i = 0; i < shuffled.Count; ++i)
                    {
                        shuffled[i].Activation = i < number ? model.InputInitial1 : model.InputInitial0;
                    }

                    var neuron = model.Layers.Last().Neurons[0];
                    while (neuron != null)
                    {
                        neuron.Target = (neuron.Id == number - _minNumber) ? 1 : 0;
                        neuron = neuron.Next;
                    }

                    model.TargetOutput = number - _minNumber;
                }
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

            public void InvalidateValue()
            {
                throw new NotImplementedException();
            }
        }

        public class MNIST : INetworkTask
        {
            private Config _config;

            public static INetworkTask Instance = new MNIST();
            
            private static MNISTControl s_control = new MNISTControl();

            private int _minNumber;
            private int _maxNumber;

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
                return 28 * 28;
            }

            public List<string> GetClasses()
            {
                var classes = new List<string>();
                for (int i = s_control.MinNumber; i <= s_control.MaxNumber; ++i)
                {
                    classes.Add(i.ToString());
                }

                return classes;
            }

            public void Do(NetworkDataModel model)
            {
                var image = s_control.Images[Rand.Flat.Next(s_control.Images.Count)];

                for (int i = 0; i < model.Layers[0].Neurons.Count; ++i)
                {
                    model.Layers[0].Neurons[i].Activation = model.InputInitial1 * image.Image[i];
                }

                var neuron = model.Layers.Last().Neurons[0];
                while (neuron != null)
                {
                    neuron.Target = (neuron.Id == image.Label) ? 1 : 0;
                    neuron = neuron.Next;
                }

                model.TargetOutput = image.Label;
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

            public void InvalidateValue()
            {
                throw new NotImplementedException();
            }
        }
        public static class Helper
        {
            public static string[] GetItems()
            {
                return typeof(NetworkTask).GetNestedTypes().Where(c => typeof(INetworkTask).IsAssignableFrom(c)).Select(c => c.Name).ToArray();
            }

            public static INetworkTask GetInstance(string name)
            {
                return (INetworkTask)typeof(NetworkTask).GetNestedTypes().Where(c => c.Name == name).First().GetField("Instance").GetValue(null);
            }

            public static void FillComboBox(ComboBox cb, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(NetworkTask.Helper), cb, config, cb.Name, defaultValue);
            }
        }
    }

    public static class NetworkTaskResult
    {
        public static class Helper
        {
            public static double Invoke(string name, double a)
            {
                var method = typeof(NetworkTaskResult).GetMethod(name);
                return (double)method.Invoke(null, new object[] { a });
            }
        }
    }
}

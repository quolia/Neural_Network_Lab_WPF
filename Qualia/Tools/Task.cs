using Qualia;
using Qualia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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
            Config Config;

            public static INetworkTask Instance = new CountDots();
            static CountDotsControl Control = new CountDotsControl();

            bool IsGaussianDistribution;
            int MinNumber;
            int MaxNumber;

            public Control GetVisualControl()
            {
                return Control;
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
                IsGaussianDistribution = Control.IsGaussianDistribution;
                MinNumber = Control.MinNumber;
                MaxNumber = Control.MaxNumber;
            }

            public void SetConfig(Config config)
            {
                Config = config;
                Control.SetConfig(config);
            }

            public void LoadConfig()
            {
                Control.LoadConfig();
                ApplyChanges();
            }

            public void SaveConfig()
            {
                Control.SaveConfig();
            }

            public int GetInputCount()
            {
                return Control.InputCount;
            }

            public List<string> GetClasses()
            {
                var classes = new List<string>();
                for (int i = Control.MinNumber; i <= Control.MaxNumber; ++i)
                {
                    classes.Add(i.ToString());
                }
                return classes;
            }

            public void Do(NetworkDataModel model)
            {
                var shuffled = model.Layers[0].ShuffledNeurons;
                shuffled.Shuffle();

                if (IsGaussianDistribution)
                {
                    double median = ((double)MinNumber + MaxNumber) / 2;

                    var number = (int)Math.Round(Rand.GaussianRand.NextGaussian(median, median / 2));
                    if (number < MinNumber)
                    {
                        number = MinNumber;
                    }
                    if (number > MaxNumber)
                    {
                        number = MaxNumber;
                    }

                    for (int i = 0; i < shuffled.Count; ++i)
                    {
                        shuffled[i].Activation = i < number ? model.InputInitial1 : model.InputInitial0;
                    }

                    var neuron = model.Layers.Last().Neurons[0];
                    while (neuron != null)
                    {
                        neuron.Target = (neuron.Id == number - MinNumber) ? 1 : 0;
                        neuron = neuron.Next;
                    }

                    model.TargetOutput = number - MinNumber;
                }
                else
                {
                    var number = Rand.Flat.Next(MinNumber, MaxNumber + 1);

                    for (int i = 0; i < shuffled.Count; ++i)
                    {
                        shuffled[i].Activation = i < number ? model.InputInitial1 : model.InputInitial0;
                    }

                    var neuron = model.Layers.Last().Neurons[0];
                    while (neuron != null)
                    {
                        neuron.Target = (neuron.Id == number - MinNumber) ? 1 : 0;
                        neuron = neuron.Next;
                    }

                    model.TargetOutput = number - MinNumber;
                }
            }

            public void VanishConfig()
            {
                Control.VanishConfig();
            }

            public bool IsValid()
            {
                return Control.IsValid();
            }

            public void SetChangeEvent(Action onChanged)
            {
                Control.SetChangeEvent(onChanged);
            }

            public void InvalidateValue()
            {
                throw new NotImplementedException();
            }
        }

        public class MNIST : INetworkTask
        {
            Config Config;

            public static INetworkTask Instance = new MNIST();
            static MNISTControl Control = new MNISTControl();

            int MinNumber;
            int MaxNumber;

            public Control GetVisualControl()
            {
                return Control;
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
                MinNumber = Control.MinNumber;
                MaxNumber = Control.MaxNumber;
            }

            public void SetConfig(Config config)
            {
                Config = config;
                Control.SetConfig(config);
            }

            public void LoadConfig()
            {
                Control.LoadConfig();
                ApplyChanges();
            }

            public void SaveConfig()
            {
                Control.SaveConfig();
            }

            public int GetInputCount()
            {
                return 28 * 28;
            }

            public List<string> GetClasses()
            {
                var classes = new List<string>();
                for (int i = Control.MinNumber; i <= Control.MaxNumber; ++i)
                {
                    classes.Add(i.ToString());
                }
                return classes;
            }

            public void Do(NetworkDataModel model)
            {
                /*
                if (IsGaussianDistribution)
                {
                    var shuffled = model.Layers[0].ShuffledNeurons;
                    shuffled.Shuffle();

                    double median = ((double)MinNumber + MaxNumber) / 2;

                    var number = (int)Math.Round(Rand.GaussianRand.NextGaussian(median, median / 2));
                    if (number < MinNumber)
                    {
                        number = MinNumber;
                    }
                    if (number > MaxNumber)
                    {
                        number = MaxNumber;
                    }

                    for (int i = 0; i < shuffled.Count; ++i)
                    {
                        shuffled[i].Activation = i < number ? model.InputInitial1 : model.InputInitial0;
                    }

                    var neuron = model.Layers.Last().Neurons[0];
                    while (neuron != null)
                    {
                        neuron.Target = (neuron.Id == number - MinNumber) ? 1 : 0;
                        neuron = neuron.Next;
                    }

                    model.TargetOutput = number - MinNumber;
                }
                */
            }

            public void VanishConfig()
            {
                Control.VanishConfig();
            }

            public bool IsValid()
            {
                return Control.IsValid();
            }

            public void SetChangeEvent(Action onChanged)
            {
                Control.SetChangeEvent(onChanged);
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

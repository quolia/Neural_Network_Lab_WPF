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
    }

    public interface INetworkTaskChanged
    {
        void TaskChanged();
        void TaskParameterChanged();
    }

    public static class NetworkTask
    {
        public class CountDotsAsymmetric : INetworkTask
        {
            public static INetworkTask Instance = new CountDotsAsymmetric();
            static CountDotsControl Control = new CountDotsControl();

            bool IsSymmetric;
            int MinNumber;
            int MaxNumber;

            public Control GetVisualControl()
            {
                return Control;
            }

            public bool IsGridSnapAdjustmentAllowed()
            {
                return true;
            }

            public void ApplyChanges()
            {
                IsSymmetric = Control.IsSymmetric;
                MinNumber = Control.MinNumber;
                MaxNumber = Control.MaxNumber;
            }

            public void Load(Config config)
            {
                Control.Load(config);
                ApplyChanges();
            }

            public void Save(Config config)
            {
                Control.Save(config);
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
                if (IsSymmetric)
                {
                    var shuffled = model.Layers.First().Neurons.Where(n => !n.IsBias).OrderBy(n => Rand.Flat.Next()).ToList();
                    var number = Rand.Flat.Next(MinNumber, MaxNumber + 1);
                    
                    for (int i = 0; i < shuffled.Count; ++i)
                    {
                        shuffled[i].Activation = i < number ? model.InputInitial1 : model.InputInitial0;
                    }

                    for (int i = 0; i < model.TargetValues.Length; ++i)
                    {
                        model.TargetValues[i] = (i == number - MinNumber) ? 1 : 0;
                    }

                    model.TargetOutput = number - MinNumber;
                }
                else
                {
                    foreach (var neuron in model.Layers.First().Neurons)
                    {
                        if (!neuron.IsBias)
                        {
                            neuron.Activation = model.InputInitial0;
                        }
                    }

                    var number = Rand.Flat.Next(MinNumber, MaxNumber + 1);
                    for (int i = 0; i < number; ++i)
                    {
                        model.Layers.First().Neurons.RandomElementTrimEnd(model.Layers.First().BiasCount).Activation = model.InputInitial1;
                    }

                    number = model.GetNumberOfFirstLayerActiveNeurons();
                    for (int i = 0; i < model.TargetValues.Length; ++i)
                    {
                        model.TargetValues[i] = (i == number - MinNumber) ? 1 : 0;
                    }

                    model.TargetOutput = number - MinNumber;
                }
            }

            public void Vanish(Config config)
            {
                Control.Vanish(config);
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

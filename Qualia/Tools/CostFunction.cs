using Qualia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Tools
{
    public interface ICostFunction
    {
        double Do(NetworkDataModel model);
        double Derivative(NetworkDataModel model, NeuronDataModel neuron);
    }

    public static class CostFunction
    {
        public class MSE : ICostFunction
        {
            public static ICostFunction Instance = new MSE();

            public double Do(NetworkDataModel model)
            {
                double s = 0;
                var neuron = model.Layers.Last().Neurons[0];
                while (neuron != null)
                {
                    s += Math.Pow(neuron.Target - neuron.Activation, 2);
                    neuron = neuron.Next;
                }
                return s;
            }

            public double Derivative(NetworkDataModel model, NeuronDataModel neuron)
            {
                return neuron.Target - neuron.Activation;
            }
        }

        public static class Helper
        {
            public static string[] GetItems()
            {
                return typeof(CostFunction).GetNestedTypes().Where(c => typeof(ICostFunction).IsAssignableFrom(c)).Select(c => c.Name).ToArray();
            }

            public static ICostFunction GetInstance(string name)
            {
                return (ICostFunction)typeof(CostFunction).GetNestedTypes().Where(c => c.Name == name).First().GetField("Instance").GetValue(null);
            }

            public static void FillComboBox(ComboBox cb, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(Helper), cb, config, cb.Name, defaultValue);
            }
        }
    }
}

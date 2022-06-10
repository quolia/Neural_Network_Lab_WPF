using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Qualia;

namespace Tools
{
    public delegate double CostFunctionDoDelegate (NetworkDataModel network);
    public delegate double CostFunctionDerivativeDelegate(NetworkDataModel network, NeuronDataModel neuron);

    public static class CostFunction
    {
        public static class MSE
        {
            public static double Do(NetworkDataModel network)
            {
                double sum = 0;
                var neuronModel = network.Layers.Last.Neurons.First;

                while (neuronModel != null)
                {
                    var diff = neuronModel.Target - neuronModel.Activation;
                    sum += diff * diff;

                    neuronModel = neuronModel.Next;
                }

                return sum;
            }

            public static double Derivative(NetworkDataModel _, NeuronDataModel neuron)
            {
                return neuron.Target - neuron.Activation;
            }
        }

        public static class Helper
        {
            public static string[] GetItems()
            {
                return typeof(CostFunction).GetNestedTypes().Select(c => c.Name).Where(c => c != "Helper").ToArray();
            }

            public static CostFunctionDoDelegate GetDo(string costFunctionName)
            {
                return Get<CostFunctionDoDelegate>(costFunctionName, "Do");
            }

            public static CostFunctionDerivativeDelegate GetDerivative(string costFunctionName)
            {
                return Get<CostFunctionDerivativeDelegate>(costFunctionName, "Derivative");
            }
            private static T Get<T>(string costFunctionName, string methodName) where T : Delegate
            {
                return (T)typeof(CostFunction).GetNestedTypes().Where(c => c.Name == costFunctionName).First().GetMethod(methodName).CreateDelegate(typeof(T));
            }


            public static void FillComboBox(ComboBox comboBox, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(Helper), comboBox, config, comboBox.Name, defaultValue);
            }
        }
    }
}

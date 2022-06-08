using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Qualia;

namespace Tools
{
    public interface ICostFunction
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        double Do(NetworkDataModel model);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        double Derivative(NetworkDataModel model, NeuronDataModel neuron);
    }

    public static class CostFunction
    {
        sealed public class MSE : ICostFunction
        {
            public static ICostFunction Instance = new MSE();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Do(NetworkDataModel networkModel)
            {
                double sum = 0;
                var neuronModel = networkModel.Layers.Last.Neurons[0];

                while (neuronModel != null)
                {
                    sum += Math.Pow(neuronModel.Target - neuronModel.Activation, 2);
                    neuronModel = neuronModel.Next;
                }

                return sum;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Derivative(NetworkDataModel networkModel, NeuronDataModel neuronModel)
            {
                return neuronModel.Target - neuronModel.Activation;
            }
        }

        public static class Helper
        {
            public static string[] GetItems()
            {
                return typeof(CostFunction).GetNestedTypes().Where(c => typeof(ICostFunction).IsAssignableFrom(c)).Select(c => c.Name).ToArray();
            }

            public static ICostFunction GetInstance(string costFunctionName)
            {
                return (ICostFunction)typeof(CostFunction).GetNestedTypes().Where(c => c.Name == costFunctionName).First().GetField("Instance").GetValue(null);
            }

            public static void FillComboBox(ComboBox comboBox, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(Helper), comboBox, config, comboBox.Name, defaultValue);
            }
        }
    }
}

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Qualia;

namespace Tools
{
    unsafe public class CostFunction
    {
        public delegate*<NetworkDataModel, double> Do;
        public delegate*<NetworkDataModel, NeuronDataModel, double> Derivative;

        public CostFunction(delegate*<NetworkDataModel, double> doFunc, delegate*<NetworkDataModel, NeuronDataModel, double> doDerivative)
        {
            Do = doFunc;
            Derivative = doDerivative;
        }
    }

    public static class CostFunctionList
    {
        unsafe sealed public class MSE : CostFunction
        {
            public static readonly MSE Instance = new MSE();

            private MSE()
                : base(&_do, &_derivative)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(NetworkDataModel networkModel)
            {
                double sum = 0;
                var neuronModel = networkModel.Layers.Last.Neurons.First;

                while (neuronModel != null)
                {
                    var diff = neuronModel.Target - neuronModel.Activation;
                    sum += diff * diff;

                    neuronModel = neuronModel.Next;
                }

                return sum;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _derivative(NetworkDataModel networkModel, NeuronDataModel neuronModel)
            {
                return neuronModel.Target - neuronModel.Activation;
            }
        }

        public static class Helper
        {
            public static string[] GetItems()
            {
                return typeof(CostFunctionList).GetNestedTypes().Where(c => typeof(CostFunction).IsAssignableFrom(c)).Select(c => c.Name).ToArray();
            }

            public static CostFunction GetInstance(string costFunctionName)
            {
                return (CostFunction)typeof(CostFunctionList).GetNestedTypes().Where(c => c.Name == costFunctionName).First().GetField("Instance").GetValue(null);
            }

            public static void FillComboBox(ComboBox comboBox, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(Helper), comboBox, config, comboBox.Name, defaultValue);
            }
        }
    }
}

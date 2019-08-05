using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Tools
{
    public interface IActivationFunction
    {
        double Do(double x, double? a);
    }

    public static class ActivationFunction
    {
        public class None : IActivationFunction
        {
            public static IActivationFunction Instance = new None();

            public double Do(double x, double? a)
            {
                return x;
            }
        }

        public class LogisticSigmoid : IActivationFunction
        {
            public static IActivationFunction Instance = new LogisticSigmoid();

            public double Do(double x, double? a)
            {
                return 1 / (1 + Math.Exp(-x));           //2/​(1+​exp(​(‑x)*​4))-​1
            }
        }

        public static class Helper
        {
            public static string[] GetItems()
            {
                return typeof(ActivationFunction).GetNestedTypes().Where(c => typeof(IActivationFunction).IsAssignableFrom(c)).Select(c => c.Name).ToArray();
            }

            public static IActivationFunction GetInstance(string name)
            {
                return (IActivationFunction)typeof(ActivationFunction).GetNestedTypes().Where(c => c.Name == name).First().GetField("Instance").GetValue(null);
            }

            public static void FillComboBox(ComboBox cb, Config config, Const.Param param, string defaultValue)
            {
                Initializer.FillComboBox(typeof(ActivationFunction.Helper), cb, config, param, defaultValue);
            }
        }
    }

    public static class ActivationDerivative
    {
        public class None : IActivationFunction
        {
            public static IActivationFunction Instance = new None();

            public double Do(double x, double? a)
            {
                return x;
            }
        }

        public class LogisticSigmoid : IActivationFunction
        {
            public static IActivationFunction Instance = new LogisticSigmoid();

            public double Do(double x, double? a)
            {
                return x * (1 - x);
            }
        }

        public static class Helper
        {
            public static IActivationFunction GetInstance(string name)
            {
                return (IActivationFunction)typeof(ActivationDerivative).GetNestedTypes().Where(c => c.Name == name).First().GetField("Instance").GetValue(null);
            }
        }
    }
}

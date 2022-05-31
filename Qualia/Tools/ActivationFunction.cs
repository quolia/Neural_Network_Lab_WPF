using System;
using System.Linq;
using System.Windows.Controls;

namespace Tools
{
    public interface IActivationFunction
    {
        double Do(double x, double? a);
        double Derivative(double x, double? a);
    }

    public static class ActivationFunction
    {
        public delegate void ActivationFunctionDelegate(double x, double? a);

        public class None : IActivationFunction
        {
            public static IActivationFunction Instance = new None();

            public double Do(double x, double? a) => x;

            public double Derivative(double x, double? a) => x;
        }

        public class LogisticSigmoid : IActivationFunction
        {
            public static IActivationFunction Instance = new LogisticSigmoid();

            public double Do(double x, double? a) => 1 / (1 + Math.Exp(-x));           

            public double Derivative(double x, double? a) => x * (1 - x);
        }

        public class SymmetricSigmoid : IActivationFunction
        {
            public static IActivationFunction Instance = new SymmetricSigmoid();

            public double Do(double x, double? a) => 2 / (1 + Math.Exp(-x)) - 1; 

            public double Derivative(double x, double? a) => 2 * LogisticSigmoid.Instance.Do(x, null) * (1 - LogisticSigmoid.Instance.Do(x, null));
        }

        public class Softsign : IActivationFunction
        {
            public static IActivationFunction Instance = new Softsign();

            public double Do(double x, double? a) => x / (1 + Math.Abs(x));

            public double Derivative(double x, double? a) => throw new NotImplementedException();
        }

        public class Tanh : IActivationFunction
        {
            public static IActivationFunction Instance = new Tanh();

            public double Do(double x, double? a) => 2 / (1 + Math.Exp(-2 * x)) - 1;

            public double Derivative(double x, double? a) => x * (2 - x);
        }

        public class ReLu : IActivationFunction
        {
            public static IActivationFunction Instance = new ReLu();

            public double Do(double x, double? a)
            {
                if (!a.HasValue)
                    a = 1;

                return x > 0 ? x * a.Value : 0;
            }

            public double Derivative(double x, double? a)
            {
                if (!a.HasValue)
                    a = 1;

                return x > 0 ? a.Value : 0;
            }
        }

        public class StepConst : IActivationFunction
        {
            public static IActivationFunction Instance = new StepConst();

            public double Do(double x, double? a)
            {
                if (!a.HasValue)
                    a = 1;

                return x > 0 ? a.Value : -a.Value;
            }

            public double Derivative(double x, double? a) => 0;
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

            public static void FillComboBox(ComboBox cb, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(ActivationFunction.Helper), cb, config, cb.Name, defaultValue);
            }

            public static ActivationFunctionDelegate GetDelegate()
            {
                return (ActivationFunctionDelegate)Delegate.CreateDelegate(typeof(ActivationFunctionDelegate), typeof(ActivationFunction).GetMethod("None1"));
            }
        }
    }
}

using System;
using System.Linq;
using System.Runtime.CompilerServices;
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

        sealed public class None : IActivationFunction
        {
            public static IActivationFunction Instance = new None();

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Do(double x, double? a) => x;

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Derivative(double x, double? a) => x;
        }

        sealed public class LogisticSigmoid : IActivationFunction
        {
            public static IActivationFunction Instance = new LogisticSigmoid();

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Do(double x, double? a) => 1 / (1 + Math.Exp(-x));

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Derivative(double x, double? a) => x * (1 - x);
        }

        sealed public class SymmetricSigmoid : IActivationFunction
        {
            public static IActivationFunction Instance = new SymmetricSigmoid();

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Do(double x, double? a) => 2 / (1 + Math.Exp(-x)) - 1;

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Derivative(double x, double? a) => 2 * LogisticSigmoid.Instance.Do(x, null) * (1 - LogisticSigmoid.Instance.Do(x, null));
        }

        sealed public class Softsign : IActivationFunction
        {
            public static IActivationFunction Instance = new Softsign();

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Do(double x, double? a) => x / (1 + Math.Abs(x));

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Derivative(double x, double? a) => throw new InvalidOperationException();
        }

        sealed public class Tanh : IActivationFunction
        {
            public static IActivationFunction Instance = new Tanh();

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Do(double x, double? a) => 2 / (1 + Math.Exp(-2 * x)) - 1;

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Derivative(double x, double? a) => x * (2 - x);
        }

        sealed public class ReLu : IActivationFunction
        {
            public static IActivationFunction Instance = new ReLu();

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Do(double x, double? a)
            {
                if (!a.HasValue)
                {
                    a = 1;
                }

                return x > 0 ? x * a.Value : 0;
            }

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Derivative(double x, double? a)
            {
                if (!a.HasValue)
                {
                    a = 1;
                }

                return x > 0 ? a.Value : 0;
            }
        }

        sealed public class StepConst : IActivationFunction
        {
            public static IActivationFunction Instance = new StepConst();

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Do(double x, double? a)
            {
                if (!a.HasValue)
                {
                    a = 1;
                }

                return x > 0 ? a.Value : -a.Value;
            }

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Derivative(double x, double? a) => 0;
        }

        internal static class Helper
        {
            public static string[] GetItems()
            {
                return typeof(ActivationFunction).GetNestedTypes().Where(c => typeof(IActivationFunction).IsAssignableFrom(c)).Select(c => c.Name).ToArray();
            }

            public static IActivationFunction GetInstance(string activationFunctionName)
            {
                return (IActivationFunction)typeof(ActivationFunction).GetNestedTypes().Where(c => c.Name == activationFunctionName).First().GetField("Instance").GetValue(null);
            }

            public static void FillComboBox(ComboBox comboBox, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(Helper), comboBox, config, comboBox.Name, defaultValue);
            }

            public static ActivationFunctionDelegate GetDelegate()
            {
                return (ActivationFunctionDelegate)Delegate.CreateDelegate(typeof(ActivationFunctionDelegate), typeof(ActivationFunction).GetMethod("None1"));
            }
        }
    }
}

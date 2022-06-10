using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Tools
{
    public delegate double ActivationFunctionDelegate(double x, double? a);

    public class IActivationFunction
    {
        public ActivationFunctionDelegate Do;
        public ActivationFunctionDelegate Derivative;

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public double Do(double x, double? a) => _do(x, a);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //;public double Derivative(double x, double? a) => _derivative(x, a);
    }

    public static class ActivationFunction
    {
        sealed public class None : IActivationFunction
        {
            public static readonly None Instance = new None();

            private None()
            {
                base.Do = None.Do;
                base.Derivative = None.Derivative;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? a) => x;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? a) => x;
        }

        sealed public class LogisticSigmoid : IActivationFunction
        {
            public static readonly LogisticSigmoid Instance = new LogisticSigmoid();

            private LogisticSigmoid()
            {
                base.Do = LogisticSigmoid.Do;
                base.Derivative = LogisticSigmoid.Derivative;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? a) => 1 / (1 + Math.Exp(-x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? a) => x * (1 - x);
        }

        sealed public class SymmetricSigmoid : IActivationFunction
        {
            public static readonly SymmetricSigmoid Instance = new SymmetricSigmoid();

            private SymmetricSigmoid()
            {
                base.Do = SymmetricSigmoid.Do;
                base.Derivative = SymmetricSigmoid.Derivative;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? a) => 2 / (1 + Math.Exp(-x)) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? a) => 2 * LogisticSigmoid.Do(x, null) * (1 - LogisticSigmoid.Do(x, null));
        }

        sealed public class Softsign : IActivationFunction
        {
            public static readonly Softsign Instance = new Softsign();

            private Softsign()
            {
                base.Do = Softsign.Do;
                base.Derivative = Softsign.Derivative;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? a) => x / (1 + Math.Abs(x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? a) => throw new InvalidOperationException();
        }

        sealed public class Tanh : IActivationFunction
        {
            public static readonly Tanh Instance = new Tanh();

            private Tanh()
            {
                base.Do = Tanh.Do;
                base.Derivative = Tanh.Derivative;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? a) => 2 / (1 + Math.Exp(-2 * x)) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? a) => x * (2 - x);
        }

        sealed public class ReLu : IActivationFunction
        {
            public static readonly ReLu Instance = new ReLu();

            private ReLu()
            {
                base.Do = ReLu.Do;
                base.Derivative = ReLu.Derivative;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? a)
            {
                if (!a.HasValue)
                {
                    a = 1;
                }

                return x > 0 ? x * a.Value : 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? a)
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
            public static readonly StepConst Instance = new StepConst();

            private StepConst()
            {
                base.Do = StepConst.Do;
                base.Derivative = StepConst.Derivative;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? a)
            {
                if (!a.HasValue)
                {
                    a = 1;
                }

                return x > 0 ? a.Value : -a.Value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? a) => 0;
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

            public static ActivationFunctionDelegate GetDelegate(string methodName)
            {
                return (ActivationFunctionDelegate)Delegate.CreateDelegate(typeof(ActivationFunctionDelegate), typeof(ActivationFunction).GetMethod(methodName));
            }
        }
    }
}

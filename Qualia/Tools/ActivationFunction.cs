using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Tools
{
    unsafe public delegate double ActivationFunctionDelegate(double x, double? a);

    unsafe public class ActivationFunction
    {
        unsafe public readonly ActivationFunctionDelegate Do;
        unsafe public readonly ActivationFunctionDelegate Derivative;

        public ActivationFunction(ActivationFunctionDelegate doFunc, ActivationFunctionDelegate derivativeFunc)
        {
            Do = doFunc;
            Derivative = derivativeFunc;
        }
    }

    unsafe public static class ActivationFunctionList
    {
        sealed public class None : ActivationFunction
        {
            public static readonly None Instance = new None();

            private None()
                :base(_do, _derivative)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _do(double x, double? a) => x;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _derivative(double x, double? a) => x;
        }

        unsafe sealed public class LogisticSigmoid : ActivationFunction
        {
            public static readonly LogisticSigmoid Instance = new LogisticSigmoid();

            private LogisticSigmoid()
                : base(_do, _derivative)
            {
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            unsafe private static double _do(double x, double? a) => 1 / (1 + Math.Exp(-x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            unsafe private static double _derivative(double x, double? a) => x * (1 - x);
        }

        unsafe sealed public class SymmetricSigmoid : ActivationFunction
        {
            public static readonly SymmetricSigmoid Instance = new SymmetricSigmoid();

            private SymmetricSigmoid()
                : base(_do, _derivative)
            {
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _do(double x, double? a) => 2 / (1 + Math.Exp(-x)) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _derivative(double x, double? a) => 2 * LogisticSigmoid.Instance.Do(x, null) * (1 - LogisticSigmoid.Instance.Do(x, null));
        }

        unsafe sealed public class Softsign : ActivationFunction
        {
            public static readonly Softsign Instance = new Softsign();

            private Softsign()
                : base(_do, _derivative)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _do(double x, double? a) => x / (1 + Math.Abs(x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _derivative(double x, double? a) => throw new InvalidOperationException();
        }

        unsafe sealed public class Tanh : ActivationFunction
        {
            public static readonly Tanh Instance = new Tanh();

            private Tanh()
                : base(_do, _derivative)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _do(double x, double? a) => 2 / (1 + Math.Exp(-2 * x)) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _derivative(double x, double? a) => x * (2 - x);
        }

        unsafe sealed public class ReLu : ActivationFunction
        {
            public static readonly ReLu Instance = new ReLu();

            private ReLu()
                : base(_do, _derivative)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _do(double x, double? a)
            {
                if (!a.HasValue)
                {
                    a = 1;
                }

                return x > 0 ? x * a.Value : 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _derivative(double x, double? a)
            {
                if (!a.HasValue)
                {
                    a = 1;
                }

                return x > 0 ? a.Value : 0;
            }
        }

        unsafe sealed public class StepConst : ActivationFunction
        {
            public static readonly StepConst Instance = new StepConst();

            private StepConst()
                : base(_do, _derivative)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _do(double x, double? a)
            {
                if (!a.HasValue)
                {
                    a = 1;
                }

                return x > 0 ? a.Value : -a.Value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _derivative(double x, double? a) => 0;
        }

        internal static class Helper
        {
            public static string[] GetItems()
            {
                return typeof(ActivationFunctionList).GetNestedTypes().Where(c => typeof(ActivationFunction).IsAssignableFrom(c)).Select(c => c.Name).ToArray();
            }

            public static ActivationFunction GetInstance(string activationFunctionName)
            {
                return (ActivationFunction)typeof(ActivationFunctionList).GetNestedTypes().Where(c => c.Name == activationFunctionName).First().GetField("Instance").GetValue(null);
            }

            public static void FillComboBox(ComboBox comboBox, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(Helper), comboBox, config, comboBox.Name, defaultValue);
            }
        }
    }
}

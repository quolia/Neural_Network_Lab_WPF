using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Qualia.Tools
{
    unsafe public class ActivationFunction
    {
        public delegate*<double, double?, double> Do;
        public delegate*<double, double?, double> Derivative;

        public ActivationFunction(delegate*<double, double?, double> doFunc, delegate*<double, double?, double> derivativeFunc)
        {
            Do = doFunc;
            Derivative = derivativeFunc;
        }
    }

    public static class ActivationFunctionList
    {
        unsafe sealed public class None : ActivationFunction
        {
            public static readonly None Instance = new();

            private None()
                : base(&_do, &_derivative)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _do(double x, double? param) => x;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double _derivative(double x, double? param) => x;
        }

        unsafe sealed public class LogisticSigmoid : ActivationFunction
        {
            public static readonly LogisticSigmoid Instance = new();

            private LogisticSigmoid()
                : base(&_do, &_derivative)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(double x, double? param) => 1 / (1 + Math.Exp(-x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _derivative(double x, double? param) => x * (1 - x);
        }

        unsafe sealed public class SymmetricSigmoid : ActivationFunction
        {
            public static readonly SymmetricSigmoid Instance = new();

            private SymmetricSigmoid()
                : base(&_do, &_derivative)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(double x, double? param) => 2 / (1 + Math.Exp(-x)) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _derivative(double x, double? param) => 2 * LogisticSigmoid.Instance.Do(x, null) * (1 - LogisticSigmoid.Instance.Do(x, null));
        }

        unsafe sealed public class Softsign : ActivationFunction
        {
            public static readonly Softsign Instance = new();

            private Softsign()
                : base(&_do, &_derivative)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(double x, double? param) => x / (1 + MathX.Abs(x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _derivative(double x, double? param) => throw new InvalidOperationException();
        }

        unsafe sealed public class Tanh : ActivationFunction
        {
            public static readonly Tanh Instance = new();

            private Tanh()
                : base(&_do, &_derivative)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(double x, double? param) => 2 / (1 + Math.Exp(-2 * x)) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _derivative(double x, double? param) => x * (2 - x);
        }

        unsafe sealed public class ReLu : ActivationFunction
        {
            public static readonly ReLu Instance = new();

            private ReLu()
                : base(&_do, &_derivative)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(double x, double? param)
            {
                param ??= 1;
                return x > 0 ? x * param.Value : 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _derivative(double x, double? param)
            {
                param ??= 1;
                return x > 0 ? param.Value : 0;
            }
        }

        unsafe sealed public class StepConst : ActivationFunction
        {
            public static readonly StepConst Instance = new();

            private StepConst()
                : base(&_do, &_derivative)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(double x, double? param)
            {
                param ??= 1;
                return x > 0 ? param.Value : -param.Value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _derivative(double x, double? param) => 0;
        }

        public static string[] GetItems()
        {
            return typeof(ActivationFunctionList)
                .GetNestedTypes()
                .Where(type => typeof(ActivationFunction).IsAssignableFrom(type))
                .Select(type => type.Name)
                .ToArray();
        }

        public static ActivationFunction GetInstance(string functionName)
        {
            return (ActivationFunction)typeof(ActivationFunctionList)
                .GetNestedTypes()
                .Where(type => type.Name == functionName)
                .First()
                .GetField("Instance")
                .GetValue(null);
        }
    }
}

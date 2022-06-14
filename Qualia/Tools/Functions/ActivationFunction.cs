using System;
using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    unsafe public class ActivationFunction : BaseFunction<ActivationFunction>
    {
        public delegate*<double, double?, double> Do;
        public delegate*<double, double?, double> Derivative;

        public ActivationFunction(delegate*<double, double?, double> doFunc, delegate*<double, double?, double> derivativeFunc)
            : base(nameof(LogisticSigmoid))
        {
            Do = doFunc;
            Derivative = derivativeFunc;
        }

        unsafe sealed public class None
        {
            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double Do(double x, double? param) => x;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double Derivative(double x, double? param) => x;
        }

        unsafe sealed public class LogisticSigmoid
        {
            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? param) => 1 / (1 + Math.Exp(-x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? param) => x * (1 - x);
        }

        unsafe sealed public class SymmetricSigmoid
        {
            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? param) => 2 / (1 + Math.Exp(-x)) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? param) => 2 * LogisticSigmoid.Instance.Do(x, null) * (1 - LogisticSigmoid.Instance.Do(x, null));
        }

        unsafe sealed public class Softsign
        {
            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? param) => x / (1 + MathX.Abs(x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? param) => throw new InvalidOperationException();
        }

        unsafe sealed public class Tanh
        {
            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? param) => 2 / (1 + Math.Exp(-2 * x)) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? param) => x * (2 - x);
        }

        unsafe sealed public class ReLu
        {
            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? param)
            {
                param ??= 1;
                return x > 0 ? x * param.Value : 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? param)
            {
                param ??= 1;
                return x > 0 ? param.Value : 0;
            }
        }

        unsafe sealed public class StepConst
        {
            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? param)
            {
                param ??= 1;
                return x > 0 ? param.Value : -param.Value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? param) => 0;
        }
    }
}

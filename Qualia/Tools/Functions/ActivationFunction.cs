using System;
using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    unsafe public class ActivationFunction : BaseFunction<ActivationFunction>
    {
        public readonly delegate*<double, double?, double> Do;
        public readonly delegate*<double, double?, double> Derivative;

        public ActivationFunction(delegate*<double, double?, double> doFunc, delegate*<double, double?, double> derivativeFunc)
            : base(nameof(LogisticSigmoid))
        {
            Do = doFunc;
            Derivative = derivativeFunc;
        }

        unsafe sealed public class None
        {
            public static readonly string Description = "f(x, a) = x * a, (a=1 -> max value)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double Do(double x, double? a) => (a ?? 1) * x;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double Derivative(double x, double? a) => a ?? 1;
        }

        unsafe sealed public class LogisticSigmoid
        {
            public static readonly string Description = "f(x, a) = 1 / (1 + Exp(-x)), (a -> not used)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? a) => 1 / (1 + Math.Exp(-x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? a) => x * (1 - x);
        }

        unsafe sealed public class SymmetricSigmoid
        {
            public static readonly string Description = "f(x, a) = 2 / (1 + Exp(-x)) - 1, (a -> not used)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? a) => 2 / (1 + Math.Exp(-x)) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? a) => 2 * LogisticSigmoid.Instance.Do(x, null) * (1 - LogisticSigmoid.Instance.Do(x, null));
        }

        unsafe sealed public class Softsign
        {
            public static readonly string Description = "f(x, a) = x / (1 + |x|), (a -> not used)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? a) => x / (1 + MathX.Abs(x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? a) => throw new InvalidOperationException();
        }

        unsafe sealed public class Tanh
        {
            public static readonly string Description = "f(x, a) = 2 / (1 + Exp(-2 * x)) - 1, (a -> not used)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? a) => 2 / (1 + Math.Exp(-2 * x)) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? a) => x * (2 - x);
        }

        unsafe sealed public class ReLu
        {
            public static readonly string Description = "f(x, a) = x > 0 ? x * a : 0, (a=1)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? a)
            {
                a ??= 1;
                return x > 0 ? x * a.Value : 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? a)
            {
                a ??= 1;
                return x > 0 ? a.Value : 0;
            }
        }

        unsafe sealed public class StepConst
        {
            public static readonly string Description = "f(x, a) = x > 0 ? a : -a, (a=1)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? a)
            {
                a ??= 1;
                return x > 0 ? a.Value : -a.Value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? a) => 0;
        }
    }
}

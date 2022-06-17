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

        unsafe sealed public class Liner
        {
            public static readonly string Description = "f(x, a) = a * x, (a=1 -> multiplier)";
            public static readonly string DerivativeDescription = "f(x, a)' = a, (a=1 -> multiplier)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double Do(double x, double? param = 1) => (param ?? 1) * x;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double Derivative(double x, double? param = 1) => param ?? 1;
        }

        unsafe sealed public class LogisticSigmoid
        {
            public static readonly string Description = "f(x, a) = 1 / (1 + exp(-x)), (a -> not used)";
            public static readonly string DerivativeDescription = "f(x, a)' = x * (1 - x), (a -> not used)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? param) => 1 / (1 + Math.Exp(-x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? param) => x * (1 - x);
        }

        unsafe sealed public class SymmetricSigmoid
        {
            public static readonly string Description = "f(x, a) = 2 / (1 + exp(-x)) - 1, (a -> not used)";
            public static readonly string DerivativeDescription = "f(x, a)' = 2 * LogisticSigmoid(x) * (1 - LogisticSigmoid(x)), (a -> not used)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? param) => 2 / (1 + Math.Exp(-x)) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? param) => 2 * LogisticSigmoid.Instance.Do(x, null) * (1 - LogisticSigmoid.Instance.Do(x, null));
        }

        unsafe sealed public class Softsign
        {
            public static readonly string Description = "f(x, a) = x / (1 + |x|), (a -> not used)";
            public static readonly string DerivativeDescription = "f(x, a)' = not implemented";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? param) => x / (1 + MathX.Abs(x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? param) => throw new InvalidOperationException();
        }

        unsafe sealed public class Tanh
        {
            public static readonly string Description = "f(x, a) = 2 / (1 + exp(-2 * x)) - 1, (a -> not used)";
            public static readonly string DerivativeDescription = "f(x, a)' = x * (2 - x), (a -> not used)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double? param) => 2 / (1 + Math.Exp(-2 * x)) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double? param) => x * (2 - x);
        }

        unsafe sealed public class ReLu
        {
            public static readonly string Description = "f(x, a) = if (x > 0) => (a * x) else => (0), (a=1 -> multiplier)";
            public static readonly string DerivativeDescription = "f(x, a)' = if (x > 0) => (a) else => (0), (a=1 -> multiplier)";

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
            public static readonly string Description = "f(x, a) = if (x > 0) => (a) else => (-a), (a=1 -> multiplier)";
            public static readonly string DerivativeDescription = "f(x, a)' = 0, (a -> not used)";

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

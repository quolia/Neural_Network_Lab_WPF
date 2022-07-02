using System;
using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    unsafe public class ActivationFunction : BaseFunction<ActivationFunction>
    {
        public readonly delegate*<double, double, double> Do;
        public readonly delegate*<double, double, double, double> Derivative;

        public ActivationFunction(delegate*<double, double, double> doFunc, delegate*<double, double, double, double> derivativeFunc)
            : base(nameof(LogisticSigmoid))
        {
            Do = doFunc;
            Derivative = derivativeFunc;
        }

        unsafe sealed public class LogisticSigmoid
        {
            public static readonly string Description = "f(x) = 1 / (1 + exp(-x))";
            public static readonly string DerivativeDescription = "f(z=f(x))' = z * (1 - z)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => 1 / (1 + Math.Exp(-x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => f * (1 - f);
        }

        unsafe sealed public class SymmetricSigmoid
        {
            public static readonly string Description = "f(x) = 2 * LogisticSigmoid(x) - 1";
            public static readonly string DerivativeDescription = "f(z=f(x))' = 2 * z * (1 - z)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => 2 * LogisticSigmoid.Do(x, 1) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => 2 * f * (1 - f);
        }

        unsafe sealed public class None
        {
            public static readonly string Description = "f(x) = x";
            public static readonly string DerivativeDescription = "f(z=f(x)=x)' = 1";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double Do(double x, double a) => x;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double Derivative(double x, double f, double a) => 1;
        }

        unsafe sealed public class Liner
        {
            public static readonly string Description = "f(x, a) = ax, (a -> multiplier)";
            public static readonly string DerivativeDescription = "f(z=f(x, a)=ax, a)' = a, (a -> multiplier)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double Do(double x, double a) => a * x;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double Derivative(double x, double f, double a) => a;
        }

        unsafe sealed public class Softsign
        {
            public static readonly string Description = "f(x) = x / (1 + |x|)";
            public static readonly string DerivativeDescription = "f(x)' = 1 / (1 + |x|)^2";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => x / (1 + MathX.Abs(x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => 1 / Math.Pow(1 + MathX.Abs(f), 2);
        }

        unsafe sealed public class Tanh
        {
            public static readonly string Description = "f(x, a) = 2 / (1 + exp(-2x)) - 1, (a -> not used)";
            public static readonly string DerivativeDescription = "f(x, a)' = 4 * exp(2x) / ((exp(2x) + 1)^2, (a -> not used)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => 2 / (1 + Math.Exp(-2 * x)) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => 4 * Math.Exp(2 * f) / Math.Pow(Math.Exp(2 * f) + 1, 2);
        }

        unsafe sealed public class ReLu
        {
            public static readonly string Description = "f(x, a) = if (x > 0) => (ax) else => (0), (a -> multiplier)";
            public static readonly string DerivativeDescription = "f(x, a)' = if (x > 0) => (a) else => (0), (a -> multiplier)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a)
            {
                return x > 0 ? a * x : 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a)
            {
                return f > 0 ? a : 0;
            }
        }

        unsafe sealed public class StepConst
        {
            public static readonly string Description = "f(x, a) = if (x > 0) => (a) else => (-a), (a -> multiplier)";
            public static readonly string DerivativeDescription = "f(x, a)' = 0, (a -> not used)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a)
            {
                return x > 0 ? a : -a;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => 0;
        }
    }
}

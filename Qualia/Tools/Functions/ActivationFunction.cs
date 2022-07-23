using System;
using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    unsafe public class ActivationFunction : BaseFunction<ActivationFunction>
    {
        public readonly delegate*<double, double, double> Do; // x, a, f
        public readonly delegate*<double, double, double, double> Derivative; // x, f, a, f'

        public ActivationFunction(delegate*<double, double, double> doFunc,
                                  delegate*<double, double, double, double> derivativeFunc)
            : base(nameof(LogisticSigmoid))
        {
            Do = doFunc;
            Derivative = derivativeFunc;
        }

        unsafe sealed public class LogisticSigmoid // Logistic, sigmoid, or soft step.
        {
            public static readonly string Description = "f(x) = 1 / (1 + exp(-x))";
            public static readonly string DerivativeDescription = "f(x)' = f(x) * (1 - f(x))";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => 1 / (1 + MathX.Exp(-x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => f * (1 - f);
        }

        unsafe sealed public class SymmetricSigmoid
        {
            public static readonly string Description = "f(x) = 2 * sigmoid(x) - 1";
            public static readonly string DerivativeDescription = "f(z=sigmoid(x))' = 2 * z * (1 - z)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => 2 * LogisticSigmoid.Do(x, 1) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a)
            {
                var sigmoid = LogisticSigmoid.Do(x, 1);
                return 2 * sigmoid * (1 - sigmoid);
            }
        }

        unsafe sealed public class Identiy
        {
            public static readonly string Description = "f(x) = x";
            public static readonly string DerivativeDescription = "f(x)' = 1";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double Do(double x, double a) => x;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double Derivative(double x, double f, double a) => 1;
        }

        unsafe sealed public class Linear
        {
            public static readonly string Description = "f(x, a) = ax, (a -> multiplier)";
            public static readonly string DerivativeDescription = "f(x, a)' = a, (a -> multiplier)";

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

        unsafe sealed public class Tanh // Hyperbolic tangent.
        {
            public static readonly string Description = "f(x) = (exp(x) - exp(-x)) / (exp(x) + exp(-x))";
            public static readonly string DerivativeDescription = "f(x)' = 1 - f(x)^2";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => (Math.Exp(x) - Math.Exp(-x)) / (Math.Exp(x) + Math.Exp(-x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => 1 - f * f;
        }

        unsafe sealed public class ReLu // Rectified linear unit.
        {
            public static readonly string Description = "f(x) = ... 0 if x <= 0 ... x if x > 0";
            public static readonly string DerivativeDescription = "f(x)' = ... 0 if x < 0 ... 1 if x > 0 ... undefined(0) if x = 0";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => x <= 0 ? 0 : x;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => x <= 0 ? 0 : 1;
        }

        unsafe sealed public class LeakyReLu // Leaky rectified linear unit.
        {
            public static readonly string Description = "f(x) = ... 0.01 * x if x < 0 ... x if x >= 0";
            public static readonly string DerivativeDescription = "f(x)' = ... 0.01 if x < 0 ... 1 if x >= 0";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => x < 0 ? 0.01 * x : x;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => x < 0 ? 0.01 : 1;
        }

        //Gaussian Error Linear Unit (GELU)[5]

        unsafe sealed public class StepConst
        {
            public static readonly string Description = "f(x, a) = if (x > 0) => (a) else => (-a), (a -> multiplier)";
            public static readonly string DerivativeDescription = "f(x, a)' = 0, (a -> not used)";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => x > 0 ? a : -a;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => 0;
        }

        unsafe sealed public class BinaryStep
        {
            public static readonly string Description = "f(x) = 0 if x < 0 ... 1 if x >= 0";
            public static readonly string DerivativeDescription = "f(x)' = 0 if x <> 0 ... undefined(0) if x = 0";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => x < 0 ? 0 : 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => 0;
        }

        unsafe sealed public class Softplus
        {
            public static readonly string Description = "f(x) = ln(1 + exp(x))";
            public static readonly string DerivativeDescription = "f(x)' = 1 / (1 + exp(-x))";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => Math.Log(1 + Math.Exp(x));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => 1 / (1 + Math.Exp(-x));
        }

        unsafe sealed public class ELU // Exponential linear unit.
        {
            public static readonly string Description = "f(x) = ... (exp(x) - 1) if x <= 0 ... x if x > 0";
            public static readonly string DerivativeDescription = "f(x)' = ... exp(x) if x < 0 ... 1 if x >= 0";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => x > 0 ? x : Math.Exp(x) - 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => x < 0 ? Math.Exp(x) : 1;
        }

        unsafe sealed public class SELU // Scaled exponential linear unit.
        {
            public static readonly string Description = "f(x) = ... 1.0507 * 1.67326 * (exp(x) - 1) if x < 0 ... x if x >= 0";
            public static readonly string DerivativeDescription = "f(x)' = ... 1.0507 * 1.67326 * exp(x) if x < 0 ... 1.0507 if x >= 0";

            public static readonly ActivationFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double x, double a) => x < 0 ? 1.0507 * 1.67326 * (Math.Exp(x) - 1) : x;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(double x, double f, double a) => x < 0 ? 1.0507 * 1.67326 * Math.Exp(x) : 1.0507;
        }
    }
}

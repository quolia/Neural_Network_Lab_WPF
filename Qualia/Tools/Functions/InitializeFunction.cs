using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    unsafe public class InitializeFunction : BaseFunction<InitializeFunction>
    {
        public readonly delegate*<double?, double> Do;

        public InitializeFunction(delegate*<double?, double> doFunc)
            : base(defaultValue: nameof(FlatRandom))
        {
            Do = doFunc;
        }

        public static bool IsSkipValue(double value)
        {
            return double.IsNaN(value);
        }

        unsafe sealed public class None
        {
            public static readonly string Description = "f(a) = none, the value is skipped, (a -> not used)";

            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? a) => Constants.SkipValue;
        }

        unsafe sealed public class Constant
        {
            public static readonly string Description = "f(a) = a, (a=1 -> constant)";

            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? a) => a ?? 1;
        }

        unsafe sealed public class FlatRandom
        {
            public static readonly string Description = "f(a) = a * random[0, 1), (a=1 -> max value)";

            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? a) => (a ?? 1) * Rand.GetFlatRandom();
        }

        unsafe sealed public class Centered
        {
            public static readonly string Description = "f(a) = -a / 2 + a * random[0, 1), (a=1 -> max value)";

            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? a)
            {
                a ??= 1;
                return -a.Value / 2 + a.Value * Rand.GetFlatRandom();
            }
        }

        unsafe sealed public class Gaussian
        {
            public static readonly string Description = "f(a) = random.gaussian(a, sigma), (a=0.5 -> mean value, sigma=0.17)";

            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? a)
            {
                a ??= 0.5;

                var randNumber = Rand.GaussianRand.NextGaussian(a.Value, 0.17);

                if (randNumber < 0)
                {
                    randNumber += a.Value;
                }
                else if (randNumber > Constants.LessThan1)
                {
                    randNumber += a.Value - Constants.LessThan1;
                }

                return randNumber;
            }
        }

        unsafe sealed public class GaussianInverted
        {
            public static readonly string Description = "f(a) = random.gaussian.inverted(a), (a=0.17 -> sigma)";

            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? a)
            {
                a ??= 0.17;

                var s = Rand.Flat.Next() % 2;
                var x = Rand.GaussianRand.NextGaussian(0, a.Value);
                x = s == 0 ? MathX.Abs(x) : Constants.LessThan1 - MathX.Abs(x);

                return x;
            }
        }

        unsafe sealed public class GaussianInverted2
        {
            public static readonly string Description = "f(a) = random.gaussian.inverted(a), (a=0.17 -> sigma)";

            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? a)
            {
                a ??= 0.17;

                var x = Rand.GaussianRand.NextGaussian(0, a.Value);
                x = x < 0 ? -x : Constants.LessThan1 - x;

                return x;
            }
        }
    }
}
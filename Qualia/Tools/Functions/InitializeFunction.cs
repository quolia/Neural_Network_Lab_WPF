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
            public static readonly string Description = "f(a) = none (the value is skipped), (a -> not used)";

            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? a = 1) => Constants.SkipValue;
        }

        unsafe sealed public class Constant
        {
            public static readonly string Description = "f(a) = a, (a=1 -> constant)";

            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? a = 1) => a ?? 1;
        }

        unsafe sealed public class FlatRandom
        {
            public static readonly string Description = "f(a) = a * random.flat[0, 1), (a=1 -> max value)";

            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? a = 1) => (a ?? 1) * Rand.RandomFlat.NextDouble();
        }

        unsafe sealed public class Centered
        {
            public static readonly string Description = "f(a) = -a / 2 + a * random.flat[0, 1), (a=1 -> max value)";

            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? a = 1)
            {
                a ??= 1;
                return -a.Value / 2 + a.Value * Rand.RandomFlat.NextDouble();
            }
        }

        unsafe sealed public class GaussNormal
        {
            public static readonly string Description = "f(a) = [x = random.gauss.normal(a, sigma=0.17)] => if (x < 0) => (x + a) else => if (x >= 1) => (x + a - 1) else => (x), (a=0.5 -> mean value)";

            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? a = 0.5)
            {
                a ??= 0.5;

                var randNumber = Rand.Gauss.GetNormal(a.Value, 0.17);

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

        unsafe sealed public class GaussNormalInverted
        {
            public static readonly string Description = "f(a) => [x = random.gauss.normal(0, a)] => if (x < 0) => (-x) else => if (x >= 0) => (1 - x)) else => (x), (a=0.17 -> sigma)";

            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? a = 0.17)
            {
                var gauss = Rand.Gauss.GetNormal(0, a ?? 0.17);

                if (gauss < 0)
                {
                    return -gauss;
                }
                else if (gauss >= 0)
                {
                    return Constants.LessThan1 - gauss;
                }

                return gauss;
            }
        }
    }
}
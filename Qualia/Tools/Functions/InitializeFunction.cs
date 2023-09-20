using System.Runtime.CompilerServices;

namespace Qualia.Tools.Functions;

public unsafe class InitializeFunction : BaseFunction<InitializeFunction>
{
    public readonly delegate*<double, double> Do;

    public InitializeFunction(delegate*<double, double> doFunc)
        : base(defaultFunction: nameof(FlatRandom))
    {
        Do = doFunc;
    }

    public static bool IsSkipValue(double value)
    {
        return double.IsNaN(value);
    }

    public sealed unsafe class Skip
    {
        public static readonly string Description = "f(a) = SkipValue (initialization is skipped)";

        public static readonly InitializeFunction Instance = new(&Do);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Do(double a) => Constants.SkipValue;
    }

    public sealed unsafe class None
    {
        public static readonly string Description = "f(a) = 0";

        public static readonly InitializeFunction Instance = new(&Do);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Do(double a) => 0;
    }

    public sealed unsafe class Constant
    {
        public static readonly string Description = "f(a) = a, (a -> constant)";

        public static readonly InitializeFunction Instance = new(&Do);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Do(double a) => a;
    }

    public sealed unsafe class FlatRandom
    {
        public static readonly string Description = "f(a) = a * random.flat[0, 1), (a -> max value)";

        public static readonly InitializeFunction Instance = new(&Do);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Do(double a) => a * Rand.RandomFlat.NextDouble();
    }

    public sealed unsafe class Centered
    {
        public static readonly string Description = "f(a) = -a / 2 + a * random.flat[0, 1), (a -> centered range width)";

        public static readonly InitializeFunction Instance = new(&Do);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Do(double a)
        {
            return -a / 2 + a * Rand.RandomFlat.NextDouble();
        }
    }

    public sealed unsafe class GaussNormal
    {
        public static readonly string Description = "f(a) => [x = random.gauss.normal(a, sigma=0.17)] => if (x < 0) => (x + a) else => if (x >= 1) => (x + a - 1) else => (x), (a -> mean value)";

        public static readonly InitializeFunction Instance = new(&Do);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Do(double a)
        {
            var randNumber = Rand.Gauss.GetNormal(a, 0.17);

            if (randNumber < 0)
            {
                randNumber += a;
            }
            else if (randNumber > Constants.LessThan1)
            {
                randNumber += a - Constants.LessThan1;
            }

            return randNumber;
        }
    }

    public sealed unsafe class GaussNormalInverted
    {
        public static readonly string Description = "f(a) => [x = random.gauss.normal(0, a)] => if (x < 0) => (-x) else => if (x >= 0) => (1 - x)) else => (x), (a -> sigma)";

        public static readonly InitializeFunction Instance = new(&Do);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Do(double a)
        {
            var s = Rand.RandomFlat.Next() % 2;
            var x = Rand.Gauss.GetNormal(0, a);
            x = s == 0 ? MathX.Abs(x) : Constants.LessThan1 - MathX.Abs(x);

            return x;
        }
    }

    public sealed unsafe class GaussianInverted2
    {
        public static readonly string Description = "f(a) = gaussian.random.inverted(a), (a -> sigma)";

        public static readonly InitializeFunction Instance = new(&Do);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Do(double a)
        {
            var x = Rand.Gauss.GetNormal(0, a);
            x = x < 0 ? -x : Constants.LessThan1 - x;

            return x;
        }
    }
}
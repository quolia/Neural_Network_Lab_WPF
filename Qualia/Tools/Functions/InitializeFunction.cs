using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    unsafe public class InitializeFunction : BaseFunction<InitializeFunction>
    {
        public delegate*<double?, double> Do;

        public InitializeFunction(delegate*<double?, double> doFunc)
            : base(nameof(FlatRandom))
        {
            Do = doFunc;
        }

        public static bool IsSkipValue(double value)
        {
            return double.IsNaN(value);
        }

        unsafe sealed public class None
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? param) => Constants.SkipValue;
        }

        unsafe sealed public class Constant
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? param) => param ?? 1;
        }

        unsafe sealed public class FlatRandom
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? param) => (param ?? 1) * Rand.GetFlatRandom();
        }

        unsafe sealed public class Centered
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? param)
            {
                param ??= 1;
                return -param.Value / 2 + param.Value * Rand.GetFlatRandom();
            }
        }

        unsafe sealed public class Gaussian
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? param)
            {
                param ??= 1;

                var randNumber = Rand.GaussianRand.NextGaussian(0.5, 0.17);

                if (randNumber < 0)
                {
                    randNumber += 0.5;
                }
                else if (randNumber > Constants.LessThan1)
                {
                    randNumber += 0.5 - Constants.LessThan1;
                }

                return randNumber * param.Value;
            }
        }

        unsafe sealed public class GaussianInverted
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? param)
            {
                var s = Rand.Flat.Next() % 2;
                var x = Rand.GaussianRand.NextGaussian(0, 0.17);
                x = s == 0 ? MathX.Abs(x) : Constants.LessThan1 - MathX.Abs(x);

                return x;
            }
        }

        unsafe sealed public class GaussianInverted2
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? param)
            {
                var x = Rand.GaussianRand.NextGaussian(0, 0.17);
                x = x < 0 ? -x : Constants.LessThan1 - x;

                return x;
            }
        }
    }
}
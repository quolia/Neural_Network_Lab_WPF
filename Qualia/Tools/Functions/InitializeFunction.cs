using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    unsafe public class InitializeFunction : BaseFunction<InitializeFunction>
    {
        public delegate*<double?, double> Do;

        public InitializeFunction(delegate*<double?, double> doFunc)
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
            public static double Do(double? param) => param ?? 0;
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

                var median = 0.5;

                var randNumber = Rand.GaussianRand.NextGaussian(median, (median - 2) / 2);

                if (randNumber < 0)
                {
                    randNumber = 0;
                }
                else if (randNumber > 1)
                {
                    randNumber = 1;
                }

                return randNumber * param.Value;
            }
        }

        unsafe sealed public class GaussianRevert
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? param)
            {
                return 1 - Gaussian.Do(param);
            }
        }
    }
}
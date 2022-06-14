using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    unsafe public class InitializeFunction : BaseFunction<InitializeFunction>
    {
        private const double LESS_THAN_1 = 1 - 0.000000000000001D;

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

                var randNumber = Rand.GaussianRand.NextGaussian(0.5, 0.17);

                if (randNumber < 0)
                {
                    randNumber = 0.5 - randNumber ;
                }
                else if (randNumber > LESS_THAN_1)
                {
                    randNumber = randNumber - 0.5;
                }

                return randNumber * param.Value;
            }
        }

        unsafe sealed public class GaussianInvert
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? param)
            {
                var s = Rand.Flat.Next() % 2;
                var x = Rand.GaussianRand.NextGaussian(0, 0.17);
                x = s == 0 ? MathX.Abs(x) : LESS_THAN_1 - MathX.Abs(x);

                return x;
            }
        }
    }
}
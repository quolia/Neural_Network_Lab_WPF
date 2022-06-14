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
            public static double Do(double? value = 1) => value ?? 1;
        }

        unsafe sealed public class FlatRandom
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? maxValue = 1) => (maxValue ?? 1) * Rand.GetFlatRandom();
        }

        unsafe sealed public class Centered
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? widthWithZeroInTheMiddle = 1)
            {
                var param = widthWithZeroInTheMiddle;

                param ??= 1;
                return -param.Value / 2 + param.Value * Rand.GetFlatRandom();
            }
        }

        unsafe sealed public class Gaussian
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? meanValue = 0.5)
            {
                meanValue ??= 0.5;

                var randNumber = Rand.GaussianRand.NextGaussian(meanValue.Value, 0.17);

                if (randNumber < 0)
                {
                    randNumber += meanValue.Value;
                }
                else if (randNumber > Constants.LessThan1)
                {
                    randNumber += meanValue.Value - Constants.LessThan1;
                }

                return randNumber;
            }
        }

        unsafe sealed public class GaussianInverted
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? sigma = 0.17)
            {
                sigma ??= 0.17;

                var s = Rand.Flat.Next() % 2;
                var x = Rand.GaussianRand.NextGaussian(0, sigma.Value);
                x = s == 0 ? MathX.Abs(x) : Constants.LessThan1 - MathX.Abs(x);

                return x;
            }
        }

        unsafe sealed public class GaussianInverted2
        {
            public static readonly InitializeFunction Instance = new(&Do);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(double? sigma = 0.17)
            {
                sigma ??= 0.17;

                var x = Rand.GaussianRand.NextGaussian(0, sigma.Value);
                x = x < 0 ? -x : Constants.LessThan1 - x;

                return x;
            }
        }
    }
}
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

        unsafe sealed public class SimpleRandom
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
    }
}
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Tools
{
    unsafe public class InitializeMode
    {
        public delegate*<double?, double> Do;

        public InitializeMode(delegate*<double?, double> doFunc)
        {
            Do = doFunc;
        }
    }

    public static class InitializeModeList
    {
        unsafe sealed public class None : InitializeMode
        {
            public static readonly None Instance = new();

            private None()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(double? param) => Constants.SkipValue;
        }

        unsafe sealed public class Constant : InitializeMode
        {
            public static readonly Constant Instance = new();

            private Constant()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(double? param) => param ?? 0;
        }

        unsafe sealed public class SimpleRandom : InitializeMode
        {
            public static readonly SimpleRandom Instance = new();

            private SimpleRandom()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(double? param) => (param ?? 1) * Rand.GetFlatRandom();
        }

        unsafe sealed public class Centered : InitializeMode
        {
            public static readonly Centered Instance = new();

            private Centered()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(double? param)
            {
                param ??= 1;
                return -param.Value / 2 + param.Value * Rand.GetFlatRandom();
            }
        }

        internal static class Helper
        {
            public static bool IsSkipValue(double value)
            {
                return double.IsNaN(value);
            }

            public static string[] GetItems()
            {
                return typeof(InitializeModeList)
                    .GetNestedTypes()
                    .Where(type => typeof(InitializeMode).IsAssignableFrom(type))
                    .Select(type => type.Name)
                    .ToArray();
            }

            public static InitializeMode GetInstance(string functionName)
            {
                return (InitializeMode)typeof(InitializeModeList)
                    .GetNestedTypes()
                    .Where(type => type.Name == functionName)
                    .First()
                    .GetField("Instance")
                    .GetValue(null);
            }

            public static void FillComboBox(ComboBox comboBox, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(Helper), comboBox, config, comboBox.Name, defaultValue);
            }
        }
    }
}
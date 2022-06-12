using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Qualia.Tools
{
    unsafe public class InitializeFunction
    {
        public delegate*<double?, double> Do;

        public InitializeFunction(delegate*<double?, double> doFunc)
        {
            Do = doFunc;
        }
    }

    public static class InitializeFunctionList
    {
        unsafe sealed public class None : InitializeFunction
        {
            public static readonly None Instance = new();

            private None()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(double? param) => Constants.SkipValue;
        }

        unsafe sealed public class Constant : InitializeFunction
        {
            public static readonly Constant Instance = new();

            private Constant()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(double? param) => param ?? 0;
        }

        unsafe sealed public class SimpleRandom : InitializeFunction
        {
            public static readonly SimpleRandom Instance = new();

            private SimpleRandom()
                : base(&_do)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double _do(double? param) => (param ?? 1) * Rand.GetFlatRandom();
        }

        unsafe sealed public class Centered : InitializeFunction
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
                return typeof(InitializeFunctionList)
                    .GetNestedTypes()
                    .Where(type => typeof(InitializeFunction).IsAssignableFrom(type))
                    .Select(type => type.Name)
                    .ToArray();
            }

            public static InitializeFunction GetInstance(string functionName)
            {
                return (InitializeFunction)typeof(InitializeFunctionList)
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
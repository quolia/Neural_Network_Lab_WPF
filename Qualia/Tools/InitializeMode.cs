using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Tools
{
    public static class InitializeMode
    {
        public static double None(double? param) => Const.SkipValue;

        public static double Constant(double? param)
        {
            if (!param.HasValue)
            {
                param = 0;
            }

            return param.Value;
        }

        public static double SimpleRandom(double? param)
        {
            if (!param.HasValue)
            {
                param = 1;
            }

            return param.Value * Rand.GetFlatRandom();
        }

        public static double Centered(double? param)
        {
            if (!param.HasValue)
            {
                param = 1;
            }

            return -param.Value / 2 + param.Value * Rand.GetFlatRandom();
        }

        internal static class Helper
        {
            public static bool IsSkipValue(double value)
            {
                return double.IsNaN(value);
            }

            public static string[] GetItems()
            {
                return typeof(InitializeMode).GetMethods().Where(methodInfo => methodInfo.IsStatic).Select(methodInfo => methodInfo.Name).ToArray();
            }

            public static double Invoke(string methodName, double? param)
            {
                var method = typeof(InitializeMode).GetMethod(methodName);
                return (double)method.Invoke(null, new object[] { param });
            }

            public static void FillComboBox(ComboBox comboBox, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(Helper), comboBox, config, comboBox.Name, defaultValue);
            }
        }
    }
}
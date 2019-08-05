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
        public static double None(double? a)
        {
            return Const.SkipValue;
        }

        public static double Constant(double? a)
        {
            if (!a.HasValue)
            {
                a = 0;
            }
            return a.Value;
        }

        public static double SimpleRandom(double? a)
        {
            if (!a.HasValue)
            {
                a = 1;
            }
            return a.Value * Rand.GetFlatRandom();
        }

        public static double Centered(double? a)
        {
            if (!a.HasValue)
            {
                a = 1;
            }
            return -a.Value / 2 + a.Value * Rand.GetFlatRandom();
        }

        public static class Helper
        {
            public static bool IsSkipValue(double d)
            {
                return double.IsNaN(d);
            }

            public static string[] GetItems()
            {
                return typeof(InitializeMode).GetMethods().Where(r => r.IsStatic).Select(r => r.Name).ToArray();
            }

            public static double Invoke(string name, double? a)
            {
                var method = typeof(InitializeMode).GetMethod(name);
                return (double)method.Invoke(null, new object[] { a });
            }

            public static void FillComboBox(ComboBox cb, Config config, Const.Param param, string defaultValue)
            {
                Initializer.FillComboBox(typeof(InitializeMode.Helper), cb, config, param, defaultValue);
            }
        }
    }
}
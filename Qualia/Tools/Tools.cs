using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tools
{
    public static class Extension
    {
        public static void Dispatch(this UIElement c, Action action)
        {
            c.Dispatcher.BeginInvoke(action);
        }

        public static void Visible(this UIElement e, bool visible)
        {
            e.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public static TabItem Tab(this TabControl tab, int index)
        {
            return (tab.Items[index] as TabItem);
        }

        public static TabItem SelectedTab(this TabControl tab)
        {
            return (tab.Items[tab.SelectedIndex] as TabItem);
        }

        public static Color GetColor(this Brush brush)
        {
            return ((SolidColorBrush)brush).Color;
        }

        //public static SelectedN
        //CtlTabs.SelectedTab.Controls[0]
    }

    public static class Culture
    {
        static CultureInfo CurrentCulture;
        public static CultureInfo Current
        {
            get
            {
                if (CurrentCulture == null)
                {
                    CurrentCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
                    CurrentCulture.NumberFormat.NumberDecimalSeparator = ".";
                }

                return CurrentCulture;
            }
        }
    }

    public static class UniqId
    {
        static long _prevId;
        public static long GetId(long existingId)
        {
            if (existingId > Const.UnknownId)
            {
                return existingId;
            }

            long id;
            do
            {
                id = DateTime.Now.Ticks;
                Thread.Sleep(0);
            }
            while (id <= _prevId);

            _prevId = id;
            return id;
        }
    }

    public class Statistic
    {
        public long Rounds;
        public long CorrectRounds;
        public double Percent => Rounds == 0 ? 0 : 100 * (double)CorrectRounds / (double)Rounds;
        public double AverageCost;

        public int LastBadOutput;
        public double LastBadOutputActivation;
        public int LastBadInput;
        public double LastBadCost;

        public int LastGoodOutput;
        public double LastGoodOutputActivation;
        public int LastGoodInput;
        public double LastGoodCost;

        public Statistic(bool init)
        {
            Rounds = 0;
            CorrectRounds = 0;
            AverageCost = 1;

            LastBadOutput = -1;
            LastBadOutputActivation = 0;
            LastBadInput = 0;
            LastBadCost = 0;

            LastGoodOutput = -1;
            LastGoodOutputActivation = 0;
            LastGoodInput = 0;
            LastGoodCost = 0;
        }
    }

    public class DynamicStatistic
    {
        public PlotPoints PercentData = new PlotPoints();
        public PlotPoints CostData = new PlotPoints();

        public void Add(double percent, double cost)
        {
            var now = DateTime.Now;
            PercentData.Add(new PlotPoint(percent, now));
            CostData.Add(new PlotPoint(cost, now));
        }

        public class PlotPoint : Tuple<double, DateTime>
        {
            public PlotPoint(double v, DateTime t)
                : base(v, t)
            {
                //
            }
        }

        public class PlotPoints : List<PlotPoint>
        {
            //
        }
    }

    public class ErrorMatrix
    {
        public long[] Input = new long[11];
        public long[] Output = new long[11];
        public long[,] Matrix = new long[11, 11];

        public long Count
        {
            get;
            private set;
        }

        public void AddData(int input, int output)
        {
            ++Input[input];
            ++Output[output];
            ++Matrix[input, output];
            ++Count;
        }

        public void ClearData()
        {
            Array.Clear(Input, 0, Input.Length);
            Array.Clear(Output, 0, Output.Length);
            for (int y = 0; y < Input.Length; ++y)
            {
                for (int x = 0; x < Output.Length; ++x)
                {
                    Matrix[x, y] = 0;
                }
            }
            Count = 0;
        }
    }

    public class InvalidValueException : Exception
    {
        public InvalidValueException(Const.Param param, string value)
            : base($"Invalid value {param.ToString()} = '{value}'.")
        {
            //
        }
    }

    public static class Initializer
    {
        public static void FillComboBox(Type helper, ComboBox cb, Config config, Const.Param param, string defaultValue)
        {
            cb.Items.Clear();
            var items = (string[])helper.GetMethod("GetItems").Invoke(null, null);
            foreach (var i in items)
            {
                cb.Items.Add(i);
            }
            var item = config.GetString(param, !String.IsNullOrEmpty(defaultValue) ? defaultValue : items.Any() ? items[0] : null);
            if (items.Any())
            {
                if (!items.Any(r => r == item))
                {
                    item = items[0];
                }
            }
            else
            {
                item = null;
            }

            if (!String.IsNullOrEmpty(item))
            {
                cb.SelectedItem = item;
            }
        }
    }

    public static class Converter
    {
        public static int? TextToInt(string text, int? defaultValue = null)
        {
            return String.IsNullOrEmpty(text) ? defaultValue : int.TryParse(text, out int a) ? a : defaultValue;
        }

        public static int TextToInt(string text, int defaultValue)
        {
            return String.IsNullOrEmpty(text) ? defaultValue : int.TryParse(text, out int a) ? a : defaultValue;
        }

        public static bool TryTextToInt(string text, out int? result, int? defaultValue = null)
        {
            if (String.IsNullOrEmpty(text))
            {
                result = defaultValue;
                return true;
            }

            if (int.TryParse(text, out int d))
            {
                result = d;
                return true;
            }

            result = null;
            return false;
        }

        public static string IntToText(int? d)
        {
            return d.HasValue ? d.Value.ToString() : null;
        }

        public static double? TextToDouble(string text, double? defaultValue = null)
        {
            return String.IsNullOrEmpty(text) ? defaultValue : double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Culture.Current, out double a) ? a : defaultValue;
        }

        public static double TextToDouble(string text, double defaultValue)
        {
            return String.IsNullOrEmpty(text) ? defaultValue : double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Culture.Current, out double a) ? a : defaultValue;
        }

        public static bool TryTextToDouble(string text, out double? result, double? defaultValue = null)
        {
            if (String.IsNullOrEmpty(text))
            {
                result = defaultValue;
                return true;
            }

            if (double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Culture.Current, out double d))
            {
                result = d;
                return true;
            }

            result = null;
            return false;
        }

        static char[] _0 = new[] { '0' };
        static char[] _S = new[] { Culture.Current.NumberFormat.NumberDecimalSeparator[0] };

        public static string DoubleToText(double? d, string format = "F99")
        {
            if (!d.HasValue)
            {
                return null;
            }
            else
            {
                var result = d.Value.ToString(format, Culture.Current);
                if (result.Contains(Culture.Current.NumberFormat.NumberDecimalSeparator))
                {
                    result = result.TrimEnd(_0).TrimEnd(_S);
                }
                return result;
            }
        }
    }
}

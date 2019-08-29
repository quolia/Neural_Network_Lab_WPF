using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tools
{
    public unsafe class Ref : ListNode<Ref>
    {
        readonly double* V;

        public double Value
        {
            get => *V;
            set => *V = value;
        }
        
        public Ref(ref double val)
        {
            fixed (double* p = &val)
            {
                V = p;
            }
        }
    }

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

        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : class
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (child as T);
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(this ContentControl depObj) where T : class
        {
            if (depObj != null)
            {
                if (depObj.Content != null && depObj.Content is T)
                {
                    yield return depObj.Content as T;
                }

                if (depObj.Content is ContentControl)
                {
                    foreach (T childOfChild in FindVisualChildren<T>(depObj.Content as ContentControl))
                    {
                        yield return childOfChild;
                    }
                }
                else if (depObj.Content is DependencyObject)
                {
                    foreach (T childOfChild in FindVisualChildren<T>(depObj.Content as DependencyObject))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static T GetParentOfType<T>(this FrameworkElement depObj) where T : class
        {
            if (depObj.Parent == null)
            {
                return null;
            }
            
            if (depObj.Parent is T)
            {
                return depObj.Parent as T;
            }
            else if (depObj.Parent is FrameworkElement)
            {
                return (depObj.Parent as FrameworkElement).GetParentOfType<T>();
            }
            else
            {
                return null;
            }
        }

        public static long TotalMicroseconds(this TimeSpan span)
        {
            return (long)(span.TotalMilliseconds * 1000);
        }
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
                id = DateTime.UtcNow.Ticks;
                Thread.Sleep(0);
            }
            while (id <= _prevId);

            _prevId = id;
            return id;
        }
    }

    public class Statistics
    {
        public long Rounds;
        public long CorrectRounds;
        public double Percent => Rounds == 0 ? 0 : 100 * (double)CorrectRounds / (double)Rounds;
        public double AverageCost;

        public string LastBadOutput;
        public double LastBadOutputActivation;
        public string LastBadInput;
        public double LastBadCost;

        public string LastGoodOutput;
        public double LastGoodOutputActivation;
        public string LastGoodInput;
        public double LastGoodCost;

        public Statistics()
        {
            Rounds = 0;
            CorrectRounds = 0;
            AverageCost = 1;

            LastBadOutput = null;
            LastBadOutputActivation = 0;
            LastBadInput = null;
            LastBadCost = 0;

            LastGoodOutput = null;
            LastGoodOutputActivation = 0;
            LastGoodInput = null;
            LastGoodCost = 0;
        }

        public Statistics Copy()
        {
            return (Statistics)MemberwiseClone();
        }
    }

    public class DynamicStatistics
    {
        public PlotPoints PercentData = new PlotPoints();
        public PlotPoints CostData = new PlotPoints();

        public void Add(double percent, double cost)
        {
            var now = DateTime.UtcNow.Ticks;
            PercentData.Add(new PlotPoint(percent, now));
            CostData.Add(new PlotPoint(cost, now));
        }

        public class PlotPoint : Tuple<double, long>
        {
            public PlotPoint(double v, long t)
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

    public static class RenderTime
    {
        public static long Network;
        public static long Plotter;
        public static long Statistics;
        public static long Data;
        public static long ErrorMatrix;
    }

    public class InvalidValueException : Exception
    {
        public InvalidValueException(Const.Param param, string value)
            : base($"Invalid value {param.ToString()} = '{value}'.")
        {
            //
        }

        public InvalidValueException(string param, string value)
            : base($"Invalid value {(param.StartsWith("Ctl", StringComparison.InvariantCultureIgnoreCase) ? param.Substring(3) : param)} = '{value}'.")
        {
            //
        }
    }

    public static class Initializer
    {
        private static string CutName(string name)
        {
            return name.StartsWith("Ctl", StringComparison.InvariantCultureIgnoreCase) ? name.Substring(3) : name;
        }

        public static void FillComboBox(Type helper, ComboBox cb, Config config, string param, string defaultValue)
        {
            param = CutName(param);

            cb.Items.Clear();
            var items = (string[])helper.GetMethod("GetItems").Invoke(null, null);
            foreach (var i in items)
            {
                cb.Items.Add(i);
            }
            var item = config.GetString(param, !String.IsNullOrEmpty(defaultValue) ? defaultValue : items.Length > 0 ? items[0] : null);
            if (items.Length > 0)
            {
                if (!items.Contains(item))
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
        public static long TicksToMicroseconds(long ticks)
        {
            return (long)(TimeSpan.FromTicks(ticks).TotalMilliseconds * 1000);
        }
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

        public static string DoubleToText(double? d, string format = "F20")
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

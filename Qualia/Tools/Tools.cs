using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Qualia;

namespace Tools
{
    public class LoopsLimit
    {
        public int CurrentLimit;
        public readonly int OriginalLimit;

        public static int Min(ref LoopsLimit[] array)
        {
            int min = int.MaxValue;

            for (int i = 0; i < array.Length; i++)
            {
                var loop = array[i];
                if (loop.CurrentLimit < min)
                {
                    min = loop.CurrentLimit;
                }
            }

            return min;
        }

        public LoopsLimit(int limit)
        {
            CurrentLimit = limit;
            OriginalLimit = limit;
        }
    }

    unsafe public static class UnsafeTools
    {
        public static IntPtr AddressOf<T>(ref T t)
        {
            TypedReference tr = __makeref(t);
            return *(IntPtr*)&tr;
        }
    }
    public unsafe struct Pointer<T>
    {
        private readonly void* _value;

        public Pointer(void* v)
        {
            _value = v;
        }

        public T Value
        {
            get => Unsafe.Read<T>(_value);
            set => Unsafe.Write(_value, value);
        }

        public static implicit operator Pointer<T>(void* v)
        {
            return new Pointer<T>(v);
        }

        public static implicit operator Pointer<T>(IntPtr p)
        {
            return new Pointer<T>(p.ToPointer());
        }
    }

    public unsafe class Ref : ListXNode<Ref>
    {
        private readonly Pointer<double> _ptr;
        
        public Ref(ref double val)
        {
            _ptr = UnsafeTools.AddressOf(ref val);
        }

        public double Value => _ptr.Value;
    }

    public static class Extension
    {
        public static bool All<T>(this List<T> list, Func<T, bool> predicate)
        {
            return list.AsEnumerable().All(predicate);
        }

        public static DispatcherOperation Dispatch(this UIElement element, Action action, DispatcherPriority priority)
        {
            return element.Dispatcher.BeginInvoke(action, priority);
        }

        public static void Visible(this UIElement element, bool visible)
        {
            element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
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

        public static T GetParentOfType<T>(this FrameworkElement element) where T : class
        {
            if (element.Parent == null)
            {
                return null;
            }
            
            if (element.Parent is T)
            {
                return element.Parent as T;
            }
            else if (element.Parent is FrameworkElement)
            {
                return (element.Parent as FrameworkElement).GetParentOfType<T>();
            }

            return null;
        }

        public static long TotalMicroseconds(this TimeSpan span)
        {
            return (long)(span.TotalMilliseconds * 1000);
        }
    }

    public static class Culture
    {
        private static CultureInfo s_currentCulture;

        public static CultureInfo Current
        {
            get
            {
                if (s_currentCulture == null)
                {
                    s_currentCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
                    s_currentCulture.NumberFormat.NumberDecimalSeparator = ".";
                }

                return s_currentCulture;
            }
        }

        public static string TimeFormat => @"hh\:mm\:ss";
    }

    public static class UniqId
    {
        private static long s_prevId;

        public static long GetNextId(long existingId)
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
            while (id <= s_prevId);

            s_prevId = id;
            return id;
        }
    }

    public class Statistics
    {
        public long Rounds;
        public long CorrectRoundsTotal;
        public long CorrectRounds;
        public double Percent;
        public double CostSum;
        public double CostSumTotal;
        public double CostAvg;

        public long TotalTicksElapsed;
        public double CurrentPureRoundsPerSecond;
        public double MaxPureRoundsPerSecond;
        public double CurrentLostRoundsPerSecond;
        public double MaxLostRoundsPerSecond;

        public string LastBadOutput;
        public double LastBadOutputActivation;
        public string LastBadInput;
        public double LastBadCost;
        public double LastBadTick;

        public string LastGoodOutput;
        public double LastGoodOutputActivation;
        public string LastGoodInput;
        public double LastGoodCost;

        public Statistics Copy()
        {
            return (Statistics)MemberwiseClone();
        }
    }

    public class DynamicStatistics
    {
        public DynamicStatistics CopyForRender;

        public PlotPoints PercentData;
        public PlotPoints CostData;

        public DynamicStatistics()
        {
            PercentData = new PlotPoints();
            CostData = new PlotPoints();
        }

        public DynamicStatistics(DynamicStatistics from)
        {
            PercentData = from.PercentData.Copy();
            CostData = from.CostData.Copy();
        }

        public void Add(double percent, double cost)
        {
            var now = DateTime.UtcNow.Ticks;
            PercentData.Add(new PlotPoint(percent, now));
            CostData.Add(new PlotPoint(cost, now));
        }

        public class PlotPoint : Tuple<double, long>
        {
            public PlotPoint(double value, long timeTicks)
                : base(value, timeTicks)
            {
                //
            }

            public double Value => Item1;
            public long TimeTicks => Item2;
        }

        public class PlotPoints : List<PlotPoint>
        {
            public PlotPoint Last()
            {
                return base[Count - 1];
            }

            public bool Any()
            {
                return Count > 0;
            }

            public PlotPoints Copy()
            {
                return (PlotPoints)MemberwiseClone();
            }
        }
    }

    public static class RenderTime
    {
        public static long Network;
        public static long Statistics;
        public static long ErrorMatrix;
    }

    public class InvalidValueException : Exception
    {
        public InvalidValueException(Const.Param paramName, string value)
            : base($"Invalid value {paramName} = '{value}'.")
        {
            //
        }

        public InvalidValueException(string paramName, string value)
            : base($"Invalid value {(paramName.StartsWith("Ctl", StringComparison.InvariantCultureIgnoreCase) ? paramName.Substring(3) : paramName)} = '{value}'.")
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

        public static void FillComboBox(Type helper, ComboBox cb, Config config, string paramName, string defaultValue)
        {
            paramName = CutName(paramName);

            cb.Items.Clear();

            var items = (string[])helper.GetMethod("GetItems").Invoke(null, null);
            foreach (var i in items)
            {
                cb.Items.Add(i);
            }

            var item = config.GetString(paramName, !string.IsNullOrEmpty(defaultValue) ? defaultValue : items.Length > 0 ? items[0] : null);
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

            if (!string.IsNullOrEmpty(item))
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

        public static long? TextToInt(string text, long? defaultValue = null)
        {
            return string.IsNullOrEmpty(text) ? defaultValue : long.TryParse(text, out long a) ? a : defaultValue;
        }

        public static long TextToInt(string text, long defaultValue)
        {
            return string.IsNullOrEmpty(text) ? defaultValue : long.TryParse(text, out long a) ? a : defaultValue;
        }

        public static bool TryTextToInt(string text, out long? result, long? defaultValue = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                result = defaultValue;
                return true;
            }

            if (long.TryParse(text, out long d))
            {
                result = d;
                return true;
            }

            result = null;
            return false;
        }

        public static string IntToText(long? d)
        {
            return d.HasValue ? d.Value.ToString() : null;
        }

        public static double? TextToDouble(string text, double? defaultValue = null)
        {
            return string.IsNullOrEmpty(text) ? defaultValue : double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Culture.Current, out double a) ? a : defaultValue;
        }

        public static double TextToDouble(string text, double defaultValue)
        {
            return string.IsNullOrEmpty(text) ? defaultValue : double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Culture.Current, out double a) ? a : defaultValue;
        }

        public static bool TryTextToDouble(string text, out double? result, double? defaultValue = null)
        {
            if (string.IsNullOrEmpty(text))
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

        private static readonly char[] _0 = new[] { '0' };
        private static readonly char[] _S = new[] { Culture.Current.NumberFormat.NumberDecimalSeparator[0] };

        public static string DoubleToText(double? d, string format = "F20", bool trim = true)
        {
            if (!d.HasValue)
            {
                return null;
            }

            var result = d.Value.ToString(format, Culture.Current);
            if (trim && result.Contains(Culture.Current.NumberFormat.NumberDecimalSeparator))
            {
                result = result.TrimEnd(_0).TrimEnd(_S);
            }

            return result;
        }
    }
}

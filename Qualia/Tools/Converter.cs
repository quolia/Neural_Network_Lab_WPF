using System;
using System.Globalization;

namespace Qualia.Tools
{
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

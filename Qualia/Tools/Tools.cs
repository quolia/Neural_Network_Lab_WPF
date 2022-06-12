using System;
using System.Globalization;

namespace Qualia.Tools
{
    sealed public class LoopsLimit
    {
        public int CurrentLimit;
        public readonly int OriginalLimit;

        public static int Min(in LoopsLimit[] array)
        {
            int min = int.MaxValue;

            for (int i = 0; i < array.Length; ++i)
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

    sealed public class InvalidValueException : Exception
    {
        public InvalidValueException(Constants.Param paramName, string value)
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
}

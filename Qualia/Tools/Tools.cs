using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Qualia.Tools
{
    sealed public class LoopsLimit(int limit)
    {
        public const int ERROR_MATRIX = 0;
        public const int NETWORK = 1;
        public const int STATISTICS = 2;

        public int CurrentLimit = limit;
        public readonly int OriginalLimit = limit;

        public bool IsLimitReached => CurrentLimit <= 0;

        public void Reset() => CurrentLimit = OriginalLimit;


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

    public static class SystemTools
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [Flags]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        public static bool SetNoSleepMode(bool yes)
        {
            try
            {
                var prev = SetThreadExecutionState(yes
                                                   ? EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS
                                                   : EXECUTION_STATE.ES_CONTINUOUS);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class Notifier : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

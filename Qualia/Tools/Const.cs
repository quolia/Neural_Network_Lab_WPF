
namespace Qualia.Tools
{
    public static class Constants
    {
        public const int UnknownId = -1;
        public const int NewId = UnknownId - 1;
        public const int InputLayerId = 0;
        public const int OutputLayerId = 1;
        public const double SkipValue = double.NaN;

        public const int DefaultInputNeuronsCount = 100;
        public const int DefaultOutputNeuronsCount = 11; // 0 1 2 3 4 5 6 7 8 9 10

        public const int SquareRoot = 32;
        public const int SquareRootMNIST = 28;

        public const double LessThan1 = 1 - 0.000000000000001D;

        public const double InvalidDouble = double.NaN;
        public const long InvalidLong = 8888888888777777777L;

        public static bool IsInvalid(long value)
        {
            return value == InvalidLong;
        }

        public static bool IsInvalid(double value)
        {
            return double.IsNaN(value);
        }

        public enum Param
        {
            ScreenWidth,
            ScreenHeight,
            ScreenLeft,
            ScreenTop,
            OnTop,

            PointSize,
            DataWidth,
            NetworkHeight,

            NetworksManagerName,
            Networks,
            SelectedNetworkIndex,
            SelectedLayerIndex,
            Layers,
            Neurons,
            Color
        }
    }

    public static class Notification
    {
        public enum ParameterChanged
        {
            Unknown = 0,

            Settings,
            DynamicSettings,
            PreventComputerFromSleep,
            DisablePreventComputerFromSleep,

            TaskFunction,
            TaskParameter,
            TaskDistributionFunction,
            TaskDistributionFunctionParam,
            IsPreventRepetition,

            Structure,
            NeuronsCount
        }
    }
}

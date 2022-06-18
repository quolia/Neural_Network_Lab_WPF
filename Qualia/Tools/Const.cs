
namespace Qualia.Tools
{
    public static class Constants
    {
        public const int UnknownId = -1;
        public const int NewId = UnknownId - 1;
        public const string UnknownIdStr = null;
        public const int InputLayerId = 0;
        public const int OutputLayerId = 1;
        public const double SkipValue = double.NaN;

        public const int DefaultInputNeuronsCount = 100;
        public const int DefaultOutputNeuronsCount = 11; // 0 1 2 3 4 5 6 7 8 9 10

        public const double LessThan1 = 1 - 0.000000000000001D;

        public enum Toggle
        {
            On,
            Off
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
            Randomizer,
            Structure,
            NeuronsCount
        }
    }
}

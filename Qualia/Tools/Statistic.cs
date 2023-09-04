using System.Collections.Generic;

namespace Qualia.Tools
{
    public class Statistics
    {
        public long Rounds;
        public long CorrectRoundsTotal;
        public long CorrectRounds;
        public double Percent;
        public double MaxPercent;
        public double CostSum;
        public double CostSumTotal;
        public double CostAvg;

        public long TotalTicksElapsed;
        public long MicrosecondsPerPureRound;
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

        public long First100PercentOnTick;
        public long Last100PercentOnTick;

        public long First100PercentOnRound;
        public long Last100PercentOnRound;

        public long LastInput;
        public long LastOutput;


        public long BlockedWeights;

        public Statistics Copy()
        {
            return (Statistics)MemberwiseClone();
        }
    }

    sealed public class PlotterStatistics
    {
        public PlotterStatistics CopyForRender;

        public readonly PlotPointsList PercentData;
        public readonly PlotPointsList CostData;

        public PlotterStatistics()
        {
            PercentData = new();
            CostData = new();
        }

        public PlotterStatistics(PlotterStatistics from)
        {
            PercentData = from.PercentData.Copy();
            CostData = from.CostData.Copy();
        }

        public void Add(double percent, double cost, long currentTick)
        {
            PercentData.Add(new(percent, currentTick));
            CostData.Add(new(cost, currentTick));
        }

        sealed public class PlotPoint(double value, long timeTicks)
        {
            public readonly double Value = value;
            public readonly long TimeTicks = timeTicks;
        }

        public class PlotPointsList : List<PlotPoint>
        {
            private readonly List<PlotPoint> _pointsToRemove = new();

            public int PointsToRemoveCount => _pointsToRemove.Count;

            public PlotPoint Last()
            {
                return base[Count - 1];
            }

            public bool Any()
            {
                return Count > 0;
            }

            public void AddToRemove(PlotPoint point)
            {
                _pointsToRemove.Add(point);
            }

            public bool CommitRemove()
            {
                if (_pointsToRemove.Count == 0)
                {
                    return false;
                }

                for (int i = 0; i < _pointsToRemove.Count; ++i)
                {
                    Remove(_pointsToRemove[i]);
                }

                _pointsToRemove.Clear();

                return true;
            }

            public PlotPointsList Copy()
            {
                return (PlotPointsList)MemberwiseClone();
            }
        }
    }

    public class RendererStatistics
    {
        public static RendererStatistics Instance = new();

        public long NetworkRenderTime;
        public long NetworkRenderTimeMax;
        public long NetworkFrames;
        public long NetworkFramesLost;

        public long StatisticsRenderTime;
        public long StatisticsRenderTimeMax;
        public long StatisticsFrames;
        public long StatisticsFramesLost;

        public long ErrorMatrixRenderTime;
        public long ErrorMatrixRenderTimeMax;
        public long ErrorMatrixFrames;
        public long ErrorMatrixFramesLost;

        public int NetworkFramesLostPercent() => (int)(NetworkFrames == 0 ? 0 : 100 * NetworkFramesLost / (double)NetworkFrames);
        public int StatisticsFramesLostPercent() => (int)(StatisticsFrames == 0 ? 0 : 100 * StatisticsFramesLost / (double)StatisticsFrames);
        public int ErrorMatrixFramesLostPercent() => (int)(ErrorMatrixFrames == 0 ? 0 : 100 * ErrorMatrixFramesLost / (double)ErrorMatrixFrames);

        public static void Reset()
        {
            Instance = new RendererStatistics();
        }

        public RendererStatistics Copy()
        {
            return (RendererStatistics)MemberwiseClone();
        }
    }
}

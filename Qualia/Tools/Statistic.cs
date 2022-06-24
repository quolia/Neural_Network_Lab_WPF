using System;
using System.Collections.Generic;

namespace Qualia.Tools
{
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
        public long MicroSecondsPerPureRound;
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

    sealed public class DynamicStatistics
    {
        public DynamicStatistics CopyForRender;

        public readonly PlotPointsList PercentData;
        public readonly PlotPointsList CostData;

        public DynamicStatistics()
        {
            PercentData = new();
            CostData = new();
        }

        public DynamicStatistics(DynamicStatistics from)
        {
            PercentData = from.PercentData.Copy();
            CostData = from.CostData.Copy();
        }

        public void Add(double percent, double cost)
        {
            var now = DateTime.UtcNow.Ticks;
            PercentData.Add(new(percent, now));
            CostData.Add(new(cost, now));
        }

        sealed public class PlotPoint
        {
            public readonly double Value;
            public readonly long TimeTicks;

            public PlotPoint(double value, long timeTicks)
            {
                Value = value;
                TimeTicks = timeTicks;
            }
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

    public static class RenderTime
    {
        public static long Network;
        public static long Statistics;
        public static long ErrorMatrix;
    }
}

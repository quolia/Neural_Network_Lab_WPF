using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public partial class StatisticsPresenter : BaseUserControl
    {
        private static readonly Typeface s_font = new(new("Tahoma"),
                                                      FontStyles.Normal,
                                                      FontWeights.Bold,
                                                      FontStretches.Normal);

        private static readonly StringBuilder s_stringBuilder = new();

        public StatisticsPresenter()
        {
            InitializeComponent();
        }

        public Dictionary<string, string> DrawStatistics(Statistics statistics, RendererStatistics statisticsAboutRender, double learningRate, TimeSpan startTimeElapsed)
        {
            if (statistics == null)
            {
                Draw(null);
                return null;
            }

            Dictionary<string, string> stat = new(30);

            var remainingTime = "...";

            if (statistics.Percent > 0)
            {
                var linerRemains = (long)((double)statistics.TotalTicksElapsed * 100 / statistics.Percent) - statistics.TotalTicksElapsed;
                remainingTime = TimeSpan.FromTicks(linerRemains).ToString(Culture.TimeFormat, Culture.Current);
            }

            stat.Add("Time / remaining",
                     startTimeElapsed.ToString(Culture.TimeFormat, Culture.Current) + " / " + remainingTime);

            stat.Add("Learning rate",
                     Converter.DoubleToText(learningRate));

            stat.Add("1", null);

            if (statistics.LastGoodOutput != null)
            {
                stat.Add("Last good output",
                         $"{statistics.LastGoodInput}={statistics.LastGoodOutput} " +
                         $"({Converter.DoubleToText(100 * statistics.LastGoodOutputActivation, "N4")} %)");

                stat.Add("Last good cost",
                         Converter.DoubleToText(statistics.LastGoodCost, "N6"));
            }
            else
            {
                stat.Add("Last good output", "none");
                stat.Add("Last good cost", "none");
            }

            stat.Add("2", null);

            if (statistics.LastBadOutput != null)
            {
                stat.Add("Last bad output",
                         $"{statistics.LastBadInput}={statistics.LastBadOutput} " +
                         $"({Converter.DoubleToText(100 * statistics.LastBadOutputActivation, "N4")} %)");

                stat.Add("Last bad cost",
                         Converter.DoubleToText(statistics.LastBadCost, "N6"));
            }
            else
            {
                stat.Add("Last bad output", "none");
                stat.Add("Last bad cost", "none");
            }

            stat.Add("3", null);

            stat.Add("Average cost",
                     Converter.DoubleToText(statistics.CostAvg, "N6"));

            stat.Add("4", null);

            stat.Add("Rounds",
                     Converter.RoundsToString(statistics.Rounds));

            stat.Add("Percent / Max",
                     Converter.DoubleToText(statistics.Percent, "N6")
                     + " / "
                     + Converter.DoubleToText(statistics.MaxPercent, "N6")
                     + " %");

            stat.Add("4.5", null);

            stat.Add("First 100%, time (round)",
                      statistics.First100PercentOnTick > 0
                      ? TimeSpan.FromTicks(statistics.First100PercentOnTick).ToString(Culture.TimeFormat, Culture.Current)
                                                                             + " ("
                                                                             + Converter.RoundsToString(statistics.First100PercentOnRound)
                                                                             + ")"
                      : "...");

            string currentPeriod;

            if (statistics.Last100PercentOnTick == 0)
            {
                currentPeriod = "...";
            }
            else
            {
                var current100PercentPeriodTicks = statistics.TotalTicksElapsed - statistics.Last100PercentOnTick;

                if (current100PercentPeriodTicks < TimeSpan.FromSeconds(1).Ticks)
                {
                    currentPeriod = (int)TimeSpan.FromTicks(current100PercentPeriodTicks).TotalMilliseconds
                                    + " msec"
                                    + " ("
                                    + Converter.RoundsToString(statistics.Last100PercentOnRound)
                                    + ")";
                }
                else
                {
                    currentPeriod = TimeSpan.FromTicks(current100PercentPeriodTicks).ToString(Culture.TimeFormat, Culture.Current)
                                                                                     + " ("
                                                                                     + Converter.RoundsToString(statistics.Last100PercentOnRound)
                                                                                     + ")";
                }
            }

            stat.Add("Current 100% period, time (from round)", currentPeriod);

            stat.Add("5", null);

            double totalRoundsPerSecond = statistics.Rounds / TimeSpan.FromTicks(statistics.TotalTicksElapsed).TotalSeconds;
            stat.Add("Total rounds/sec",
                     string.Format(Culture.Current,
                                   Converter.IntToText((long)totalRoundsPerSecond)));

            stat.Add("Microseconds / pure round",
                     Converter.IntToText(statistics.MicrosecondsPerPureRound));

            stat.Add("Current / Max pure rounds/sec",
                     string.Format(Culture.Current,
                                   $"{(int)statistics.CurrentPureRoundsPerSecond} / {(int)statistics.MaxPureRoundsPerSecond}"));

            stat.Add("Current / Max lost rounds/sec",
                     string.Format(Culture.Current,
                                   $"{(int)statistics.CurrentLostRoundsPerSecond} / {(int)statistics.MaxLostRoundsPerSecond}"));

            stat.Add("6", null);

            stat.Add("Render time, mcsec / Max / Frames lost, %", string.Empty);
            stat.Add("Network & Data",
                     Converter.IntToText(TimeSpan.FromTicks(statisticsAboutRender.NetworkRenderTime).TotalMicroseconds())
                     + " / "
                     + Converter.IntToText(TimeSpan.FromTicks(statisticsAboutRender.NetworkRenderTimeMax).TotalMicroseconds())
                     + " / "
                     + Converter.IntToText(statisticsAboutRender.NetworkFramesLostPercent()));

            stat.Add("Statistics & Plotter",
                     Converter.IntToText(TimeSpan.FromTicks(statisticsAboutRender.StatisticsRenderTime).TotalMicroseconds())
                     + " / "
                     + Converter.IntToText(TimeSpan.FromTicks(statisticsAboutRender.StatisticsRenderTimeMax).TotalMicroseconds())
                     + " / "
                     + Converter.IntToText(statisticsAboutRender.StatisticsFramesLostPercent()));

            stat.Add("Error matrix",
                     Converter.IntToText(TimeSpan.FromTicks(statisticsAboutRender.ErrorMatrixRenderTime).TotalMicroseconds())
                     + " / "
                     + Converter.IntToText(TimeSpan.FromTicks(statisticsAboutRender.ErrorMatrixRenderTimeMax).TotalMicroseconds())
                     + " / "
                     + Converter.IntToText(statisticsAboutRender.ErrorMatrixFramesLostPercent()));

            Draw(stat);
            return stat;
        }

        public void Draw(Dictionary<string, string> stats)
        {
            CtlCanvas.Clear();

            if (stats == null)
            {
                return;
            }

            s_stringBuilder.Clear();
            foreach (var item in stats)
            {
                if (item.Value == null)
                {
                    s_stringBuilder.AppendLine();
                    continue;
                }

                s_stringBuilder.AppendLine($"{item.Key}: {item.Value}");
            }

            FormattedText formattedText = new(s_stringBuilder.ToString(),
                                              Culture.Current,
                                              FlowDirection.LeftToRight,
                                              s_font,
                                              10,
                                              Brushes.Black,
                                              RenderSettings.PixelsPerDip);

            CtlCanvas.DrawText(formattedText, ref Points.Get(10, 0));

            Width = MathX.Max(ActualWidth, formattedText.WidthIncludingTrailingWhitespace + 10);
        }

        public void Clear()
        {
            Draw(null);
        }
    }
}

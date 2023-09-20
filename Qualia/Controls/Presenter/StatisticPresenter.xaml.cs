using System;
using System.Collections.Generic;
using System.Text;
using Qualia.Controls.Base;
using Qualia.Tools;

namespace Qualia.Controls.Presenter;

public sealed partial class StatisticsPresenter : BaseUserControl
{
    private static readonly StringBuilder s_stringBuilder = new();
    private readonly Dictionary<string, string> _stat = new(30);

    private double _maxWidth;

    public StatisticsPresenter()
        : base (0)
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

        var remainingTime = "...";

        if (statistics.Percent > 0)
        {
            var linerRemains = (long)((double)statistics.TotalTicksElapsed * 100 / statistics.Percent) - statistics.TotalTicksElapsed;
            remainingTime = TimeSpan.FromTicks(linerRemains).ToString(Culture.TimeFormat, Culture.Current);
        }

        _stat["Time / remaining"] = 
            startTimeElapsed.ToString(Culture.TimeFormat, Culture.Current) + " / " + remainingTime;

        _stat["Learning rate"] =
            Converter.DoubleToText(learningRate);

        _stat["1"] = null;

        if (statistics.LastGoodOutput != null)
        {
            _stat["Last good output"] =
                $"{statistics.LastGoodInput}={statistics.LastGoodOutput} " +
                $"({Converter.DoubleToText(100 * statistics.LastGoodOutputActivation, "N4")} %)";

            _stat["Last good cost"] =
                Converter.DoubleToText(statistics.LastGoodCost, "N6");
        }
        else
        {
            _stat["Last good output"] = "none";
            _stat["Last good cost"] = "none";
        }

        _stat["2"] = null;

        if (statistics.LastBadOutput != null)
        {
            _stat["Last bad output"] =
                $"{statistics.LastBadInput}={statistics.LastBadOutput} " +
                $"({Converter.DoubleToText(100 * statistics.LastBadOutputActivation, "N4")} %)";

            _stat["Last bad cost"] =
                Converter.DoubleToText(statistics.LastBadCost, "N6");
        }
        else
        {
            _stat["Last bad output"] = "none";
            _stat["Last bad cost"] = "none";
        }

        _stat["3"] = null;

        _stat["Average cost"] =
            Converter.DoubleToText(statistics.CostAvg, "N6");

        _stat["4"] = null;

        _stat["Rounds"] =
            Converter.RoundsToString(statistics.Rounds);

        _stat["Percent / Max"] =
            Converter.DoubleToText(statistics.Percent, "N6")
            + " / "
            + Converter.DoubleToText(statistics.MaxPercent, "N6")
            + " %";

        _stat["4.5"] = null;

        _stat["First 100%, time (round)"] =
            statistics.First100PercentOnTick > 0
                ? TimeSpan.FromTicks(statistics.First100PercentOnTick).ToString(Culture.TimeFormat, Culture.Current)
                  + " ("
                  + Converter.RoundsToString(statistics.First100PercentOnRound)
                  + ")"
                : "...";

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

        _stat["Current 100% period, time (from round)"] = currentPeriod;

        _stat["5"] = null;

        var totalRoundsPerSecond = statistics.TotalTicksElapsed > 0
            ? statistics.Rounds / TimeSpan.FromTicks(statistics.TotalTicksElapsed).TotalSeconds
            : 0;
        _stat["Total rounds/sec"] =
            string.Format(Culture.Current,
                Converter.IntToText((long)totalRoundsPerSecond));

        _stat["Microseconds / pure round"] =
            Converter.IntToText(statistics.MicrosecondsPerPureRound);

        _stat["Current / Max pure rounds/sec"] =
            string.Format(Culture.Current,
                $"{(int)statistics.CurrentPureRoundsPerSecond} / {(int)statistics.MaxPureRoundsPerSecond}");

        _stat["Current / Max lost rounds/sec"] =
            string.Format(Culture.Current,
                $"{(int)statistics.CurrentLostRoundsPerSecond} / {(int)statistics.MaxLostRoundsPerSecond}");

        _stat["6"] = null;

        _stat["Render time, mcsec / Max / Frames lost, %"] = string.Empty;
        _stat["Network & Data"] =
            Converter.IntToText(TimeSpan.FromTicks(statisticsAboutRender.NetworkRenderTime).TotalMicroseconds())
            + " / "
            + Converter.IntToText(TimeSpan.FromTicks(statisticsAboutRender.NetworkRenderTimeMax).TotalMicroseconds())
            + " / "
            + Converter.IntToText(statisticsAboutRender.NetworkFramesLostPercent());

        _stat["Statistics & Plotter"] =
            Converter.IntToText(TimeSpan.FromTicks(statisticsAboutRender.StatisticsRenderTime).TotalMicroseconds())
            + " / "
            + Converter.IntToText(TimeSpan.FromTicks(statisticsAboutRender.StatisticsRenderTimeMax).TotalMicroseconds())
            + " / "
            + Converter.IntToText(statisticsAboutRender.StatisticsFramesLostPercent());

        _stat["Error matrix"] =
            Converter.IntToText(TimeSpan.FromTicks(statisticsAboutRender.ErrorMatrixRenderTime).TotalMicroseconds())
            + " / "
            + Converter.IntToText(TimeSpan.FromTicks(statisticsAboutRender.ErrorMatrixRenderTimeMax).TotalMicroseconds())
            + " / "
            + Converter.IntToText(statisticsAboutRender.ErrorMatrixFramesLostPercent());

        Draw(_stat);
        return _stat;
    }

    public void Clear()
    {
        Draw(null);
    }
    
    private void Draw(Dictionary<string, string> stats)
    {
        CtlText.Text = string.Empty;

        if (stats == null)
        {
            _maxWidth = 0;
            CtlText.Width = 0;
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

        CtlText.Text = s_stringBuilder.ToString();
        if (CtlText.ActualWidth > _maxWidth)
        {
            _maxWidth = CtlText.ActualWidth;
        }

        CtlText.Width = _maxWidth;
    }
}
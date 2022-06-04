using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Runtime;
using System.Windows.Threading;
using Microsoft.Win32;
using Qualia.Controls;
using Tools;
using static Tools.Threads;

namespace Qualia
{
    public partial class Main : WindowResizeControl, INetworkTaskChanged, IDisposable
    {
        public static object ApplyChangesLocker = new object();

        private Thread _timeThread;
        private Thread _runNetworksThread;
        private CancellationToken _cancellationToken;
        private CancellationTokenSource _cancellationTokenSource;

        private NetworksManager _networksManager;

        private Stopwatch _startTime;
        private long _rounds;

        private readonly List<IConfigValue> _configParams;

        public Main()
        {
            //SetProcessorAffinity(Processor.Proc1);
            //SetThreadPriority(ThreadPriorityLevel.Normal);

            Thread.CurrentThread.CurrentCulture = Culture.Current;
            Logger.Log("Application started.");

            InitializeComponent();

            WindowState = WindowState.Maximized;

            _configParams = new List<IConfigValue>()
            {
                CtlOnlyWeights,
                CtlOnlyChangedWeights,
                CtlHighlightChangedWeights
            };

            Loaded += Main_Load;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            //Dispatcher.BeginInvoke((Action)(() =>
            //{
            //    SetProcessorAffinity(Processor.Proc0);
            //}));

            CreateDirectories();

            CtlNetworkPresenter.SizeChanged += NetworkPresenter_SizeChanged;

            LoadConfig();

            CtlMenuRun.IsEnabled = _networksManager != null && _networksManager.NetworkModels != null && _networksManager.NetworkModels.Any();
        }

        private void NetworkPresenter_SizeChanged(object sender, EventArgs e)
        {
            CtlNetworkPresenter.Height = Math.Max(CtlNetworkPresenter.Height, 400);

            if (_networksManager == null)
            {
                return;
            }

            var ticks = DateTime.UtcNow.Ticks;
            CtlNetworkPresenter.ResizeTicks = ticks;

            CtlNetworkPresenter.Dispatch(() =>
            {
                if (CtlNetworkPresenter.ResizeTicks != ticks)
                {
                    return;
                }

                if (IsRunning)
                {
                    CtlNetworkPresenter.RenderRunning(_networksManager.SelectedNetworkModel, CtlOnlyWeights.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn);
                }
                else
                {
                    CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel);
                }

            }, DispatcherPriority.Render);
        }

        private void LoadConfig()
        {
            Width = Config.Main.GetDouble(Const.Param.ScreenWidth, SystemParameters.PrimaryScreenWidth).Value;
            Height = Config.Main.GetDouble(Const.Param.ScreenHeight, SystemParameters.PrimaryScreenHeight).Value;
            Top = Config.Main.GetDouble(Const.Param.ScreenTop, 0).Value;
            Left = Config.Main.GetDouble(Const.Param.ScreenLeft, 0).Value;
            Topmost = Config.Main.GetBool(Const.Param.OnTop, false);
            DataWidth.Width = new GridLength(Config.Main.GetDouble(Const.Param.DataWidth, 100).Value);
            NetworkHeight.Height = new GridLength(Config.Main.GetDouble(Const.Param.NetworkHeight, 200).Value);

            _configParams.ForEach(p => p.SetConfig(Config.Main));
            _configParams.ForEach(p => p.LoadConfig());

            var fileName = Config.Main.GetString(Const.Param.NetworksManagerName, null);
            LoadNetworksManager(fileName);
            LoadSettings();
        }

        private void LoadSettings()
        {
            CtlSettings.SetConfig(Config.Main);
            CtlSettings.LoadConfig();
            CtlSettings.SetChangeEvent(OnSettingsChanged);
            CtlApplySettingsButton.IsEnabled = false;
            CtlCancelSettingsButton.IsEnabled = false;
        }

        private void OnSettingsChanged()
        {
            CtlApplySettingsButton.IsEnabled = true;
            CtlCancelSettingsButton.IsEnabled = true;
        }

        private bool SaveSettings()
        {
            if (!CtlSettings.IsValid())
            {
                MessageBox.Show("Settings parameter is invalid.", "Error");
                return false;
            }

            CtlSettings.SaveConfig();
            CtlApplySettingsButton.IsEnabled = false;
            CtlCancelSettingsButton.IsEnabled = false;

            return true;
        }

        private Settings Settings => CtlSettings.Settings;

        private void SetTitle(string fileName)
        {
            Title = "Neural Network - " + fileName;
        }

        private void LoadNetworksManager(string fileName)
        {
            if (!StopRequest())
            {
                return;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            if (!File.Exists(fileName))
            {
                fileName = "\\Networks\\" + Path.GetFileName(fileName);
            }

            if (File.Exists(fileName))
            {
                _networksManager = new NetworksManager(CtlTabs, fileName, OnNetworkUIChanged);
                Config.Main.Set(Const.Param.NetworksManagerName, fileName);
                CtlInputDataPresenter.LoadConfig(_networksManager.Config, this);

                ReplaceNetworksManagerControl(_networksManager);
                if (_networksManager.IsValid())
                {
                    SetTitle(fileName);
                    ApplyChangesToStandingNetworks();
                }
                else
                {
                    MessageBox.Show("Network parameter is not valid.", "Error");
                }
            }
            else
            {
                MessageBox.Show($"Network '{fileName}' is not found!", "Error", MessageBoxButton.OK);
                Config.Main.Set(Const.Param.NetworksManagerName, string.Empty);
            }
        }

        private bool SaveConfig()
        {
            Config.Main.Set(Const.Param.ScreenWidth, ActualWidth);
            Config.Main.Set(Const.Param.ScreenHeight, ActualHeight);
            Config.Main.Set(Const.Param.ScreenTop, Top);
            Config.Main.Set(Const.Param.ScreenLeft, Left);
            Config.Main.Set(Const.Param.OnTop, Topmost);
            Config.Main.Set(Const.Param.DataWidth, DataWidth.ActualWidth);
            Config.Main.Set(Const.Param.NetworkHeight, NetworkHeight.ActualHeight);

            _configParams.ForEach(p => p.SaveConfig());

            if (!SaveSettings())
            {
                return false;
            }

            if (_networksManager != null)
            {
                CtlInputDataPresenter.SaveConfig(_networksManager.Config);

                if (!_networksManager.IsValid())
                {
                    MessageBox.Show("Network parameter is invalid", "Error");
                    return false;
                }
                else
                {
                    _networksManager.SaveConfig();
                }
            }

            Config.Main.FlushToDrive();
            if (_networksManager != null)
            {
                _networksManager.Config.FlushToDrive();
            }

            return true;
        }

        private void CreateDirectories()
        {
            if (!Directory.Exists("Networks"))
            {
                Directory.CreateDirectory("Networks");
            }
        }

        private void ToggleApplyChanges(Const.Toggle state)
        {
            if (state == Const.Toggle.On)
            {
                CtlApplyChanges.Background = Brushes.Yellow;
                CtlApplyChanges.IsEnabled = true;
            }
            else
            {
                CtlApplyChanges.Background = Brushes.White;
                CtlApplyChanges.IsEnabled = false;
            }
        }

        private void OnNetworkUIChanged(Notification.ParameterChanged param)
        {
            ToggleApplyChanges(Const.Toggle.On);
            CtlMenuStart.IsEnabled = false;

            if (param == Notification.ParameterChanged.NeuronsCount)
            {
                if (_networksManager != null)
                {
                    if (_networksManager.IsValid())
                    {
                        _networksManager.ResetLayersTabsNames();
                    }
                    else
                    {
                        ToggleApplyChanges(Const.Toggle.Off);
                    }
                }

                if (CtlInputDataPresenter.NetworkTask != null && !CtlInputDataPresenter.NetworkTask.IsValid())
                {
                    ToggleApplyChanges(Const.Toggle.Off);
                }
            }
        }

        private void ApplyChangesToRunningNetworks()
        {
            lock (ApplyChangesLocker)
            {
                CtlInputDataPresenter.NetworkTask.ApplyChanges();
                CtlInputDataPresenter.RearrangeWithNewPointsCount();

                var newModels = _networksManager.CreateNetworksDataModels();
                _networksManager.MergeModels(newModels);

                CtlNetworkPresenter.RenderRunning(_networksManager.SelectedNetworkModel,
                                                  CtlOnlyWeights.IsOn,
                                                  CtlOnlyChangedWeights.IsOn,
                                                  CtlHighlightChangedWeights.IsOn);

                ToggleApplyChanges(Const.Toggle.Off);
            }
        }

        private void ApplyChangesToStandingNetworks()
        {
            lock (ApplyChangesLocker)
            {
                CtlInputDataPresenter.NetworkTask.ApplyChanges();
                CtlInputDataPresenter.RearrangeWithNewPointsCount();

                _networksManager.RefreshNetworksDataModels();

                CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel);

                ToggleApplyChanges(Const.Toggle.Off);

                CtlMenuStart.IsEnabled = true;
                CtlMenuRun.IsEnabled = true;
            }
        }

        private bool IsRunning => CtlMenuStop.IsEnabled;

        private void CtlMenuStart_Click(object sender, RoutedEventArgs e)
        {
            if (SaveConfig())
            {
                ApplyChangesToStandingNetworks();

                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationToken = _cancellationTokenSource.Token;

                CtlMenuStart.IsEnabled = false;
                CtlMenuReset.IsEnabled = false;
                CtlMenuStop.IsEnabled = true;
                CtlMenuDeleteNetwork.IsEnabled = false;

                _networksManager.PrepareModelsForRun();

                _networksManager.PrepareModelsForRound();
                CtlInputDataPresenter.SetInputDataAndDraw(_networksManager.SelectedNetworkModel);
                _networksManager.FeedForward(); // initialize state

                DrawNetwork(_networksManager.SelectedNetworkModel, CtlOnlyWeights.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn);

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
                GC.WaitForFullGCComplete();

                _rounds = 0;
                _startTime = Stopwatch.StartNew();

                _runNetworksThread = new Thread(new ParameterizedThreadStart(RunNetworks))
                {
                    Name = "RunNetworks",
                    Priority = ThreadPriority.Highest,
                    ApartmentState = ApartmentState.STA,
                    IsBackground = false
                };
                _runNetworksThread.Start(new object[] { Processor.Proc2 | Processor.Proc3 } );

                _timeThread = new Thread(new ThreadStart(RunTimer))
                {
                    Name = "RunTimer",
                    Priority = ThreadPriority.Normal,
                    IsBackground = true
                };
                _timeThread.Start();
            }
        }

        unsafe private void RunNetworks(object args)
        {
            var arr = (object[])args;
            var processors = (Processor)arr[0];

            //Thread.BeginThreadAffinity();
            //SetProcessorAffinity(processors);
            SetThreadPriority(ThreadPriorityLevel.TimeCritical);

            var loopLimits = new List<LoopsLimit>()
            {
                new LoopsLimit(Settings.SkipRoundsToDrawErrorMatrix),
                new LoopsLimit(Settings.SkipRoundsToDrawNetworks),
                new LoopsLimit(Settings.SkipRoundsToDrawStatistics)

            }.OrderBy(limit => limit.OriginalLimit).ToList();

            var currentLoopLimit = loopLimits.First();

            bool isErrorMatrixRendering = false;
            bool isNetworksRendering = false;
            bool isStatisticsRendering = false;
            bool isRendering = false;


            bool isErrorMatrixRenderNeeded = false;
            bool isNetworksRenderNeeded = false;
            bool isStatisticsRenderNeeded = false;
            bool isRenderNeeded = false;

            ErrorMatrix errorMatrixToRender = null;
            NetworkDataModel networkModelToRender = null;
            Statistics statisticsToRender = null;

            var swCurrentMiscCodeTime = new Stopwatch();
            var swCurrentPureRoundsPerSecond = new Stopwatch();
            var swRenderTime = new Stopwatch();
            
            var currentMiscCodeTimeSpan = TimeSpan.FromTicks(0);

            const double K1 = 1;
            const double K2 = 0;

            while (!_cancellationToken.IsCancellationRequested)
            {
                //Thread.Sleep(1);

                lock (ApplyChangesLocker)
                {
                    swCurrentPureRoundsPerSecond.Restart();

                    for (int round = 0; round < currentLoopLimit.CurrentLimit; ++round)
                    {
                        _networksManager.PrepareModelsForRound();

                        var networkModel = _networksManager.NetworkModels[0];
                        while (networkModel != null)
                        {
                            if (!networkModel.IsEnabled)
                            {
                                networkModel = networkModel.Next;
                                continue;
                            }

                            networkModel.FeedForward();
                            
                            var output = networkModel.GetMaxActivatedOutputNeuron();
                            var outputId = output.Id;
                            var input = networkModel.TargetOutput;
                            var cost = networkModel.CostFunction.Do(networkModel);
                            var statistics = networkModel.Statistics;

                            if (input == outputId)
                            {
                                ++statistics.CorrectRoundsTotal;
                                ++statistics.CorrectRounds;

                                statistics.LastGoodInput = networkModel.Classes[input];
                                statistics.LastGoodOutput = networkModel.Classes[outputId];
                                statistics.LastGoodOutputActivation = output.Activation;
                                statistics.LastGoodCost = cost;
                            }
                            else
                            {
                                statistics.LastBadInput = networkModel.Classes[input];
                                statistics.LastBadOutput = networkModel.Classes[outputId];
                                statistics.LastBadOutputActivation = output.Activation;
                                statistics.LastBadCost = cost;
                                statistics.LastBadTick = _startTime.Elapsed.Duration().Ticks;
                            }

                            statistics.CostSum += cost;
                            networkModel.ErrorMatrix.AddData(input, outputId);

                            networkModel.BackPropagation();

                            networkModel = networkModel.Next;
                        }
                    }

                    swCurrentPureRoundsPerSecond.Stop();
                    swCurrentMiscCodeTime.Restart();

                    _rounds += currentLoopLimit.CurrentLimit;

                    //if (!isStatisticsRendering && _rounds % Settings.SkipRoundsToDrawStatistics == 0)
                    if (_rounds % Settings.SkipRoundsToDrawStatistics == 0)
                    {
                        //var pureSpeedElapsedSeconds = swCurrentMiscCodeTime.Elapsed.Duration().TotalSeconds;
                        var totalTicksElapsed = _startTime.Elapsed.Duration().Ticks;

                        var networkModel = _networksManager.NetworkModels[0];
                        while (networkModel != null)
                        {
                            if (!networkModel.IsEnabled)
                            {
                                networkModel = networkModel.Next;
                                continue;
                            }

                            var statistics = networkModel.Statistics;

                            statistics.Rounds = _rounds;
                            statistics.TotalTicksElapsed = totalTicksElapsed;
                            statistics.CostSumTotal += statistics.CostSum;

                            //statistics.PureRoundsPerSecond = statistics.Rounds / pureSpeedElapsedSeconds;
                            statistics.CurrentPureRoundsPerSecond = currentLoopLimit.CurrentLimit / swCurrentPureRoundsPerSecond.Elapsed.Duration().TotalSeconds;
                            if (statistics.CurrentPureRoundsPerSecond > statistics.MaxPureRoundsPerSecond)
                            {
                                statistics.MaxPureRoundsPerSecond = statistics.CurrentPureRoundsPerSecond;
                            }

                            var miscCodeSeconds = currentMiscCodeTimeSpan.Duration().TotalSeconds;
                            statistics.CurrentLostRoundsPerSecond = statistics.CurrentPureRoundsPerSecond * miscCodeSeconds;
                            if (statistics.CurrentLostRoundsPerSecond > statistics.MaxLostRoundsPerSecond)
                            {
                                statistics.MaxLostRoundsPerSecond = statistics.CurrentLostRoundsPerSecond;
                            }

                            //double totalRoundsPerSec = statistics.Rounds / _startTime.Elapsed.Duration().TotalSeconds;
                            //if (totalRoundsPerSec > statistics.MaxRoundsPerSecond)
                            //{
                            //    statistics.MaxRoundsPerSecond = totalRoundsPerSec;
                            //}

                            var percent = 100 * (double)statistics.CorrectRounds / Settings.SkipRoundsToDrawStatistics;
                            var percentTotal = 100 * (double)statistics.CorrectRoundsTotal / statistics.Rounds;

                            statistics.Percent = percent * K1 + percentTotal * K2;

                            var costAvg = statistics.CostSum / Settings.SkipRoundsToDrawStatistics;
                            var costAvgTotal = statistics.CostSumTotal / statistics.Rounds;
                            statistics.CostAvg = costAvg * K1 + costAvgTotal * K2;

                            networkModel.DynamicStatistics.Add(statistics.Percent, statistics.CostAvg);

                            statistics.CostSum = 0;
                            statistics.CorrectRounds = 0;
                            
                            networkModel = networkModel.Next;
                        }
                    }

                    int currentLimit = currentLoopLimit.CurrentLimit;

                    foreach (var loopLimit in loopLimits)
                    {
                        loopLimit.CurrentLimit -= currentLimit;
                        if (loopLimit.CurrentLimit <= 0)
                        {
                            loopLimit.CurrentLimit = loopLimit.OriginalLimit;
                        }
                    }

                    loopLimits = loopLimits.OrderBy(limit => limit.CurrentLimit).ToList();
                    currentLoopLimit = loopLimits.First();
                }

                if (isRendering)
                {
                    isErrorMatrixRenderNeeded = false;
                    isNetworksRenderNeeded = false;
                    isStatisticsRenderNeeded = false;
                }
                else
                {
                    isErrorMatrixRenderNeeded = !isErrorMatrixRendering && _rounds % Settings.SkipRoundsToDrawErrorMatrix == 0;
                    isNetworksRenderNeeded = !isNetworksRendering && _rounds % Settings.SkipRoundsToDrawNetworks == 0;
                    isStatisticsRenderNeeded = !isStatisticsRendering && _rounds % Settings.SkipRoundsToDrawStatistics == 0;
                }

                isRenderNeeded = isErrorMatrixRenderNeeded || isNetworksRenderNeeded || isStatisticsRenderNeeded;

                if (isRenderNeeded)
                {
                    isRendering = true;

                    NetworkDataModel selectedNetworkModel = _networksManager.SelectedNetworkModel;
                    double learningRate = 0;

                    if (isErrorMatrixRenderNeeded)
                    {
                        isErrorMatrixRendering = true;

                        //lock (ApplyChangesLocker)
                        {
                            //swRenderTime.Restart();

                            //selectedNetworkModel = _networksManager.SelectedNetworkModel;
                            errorMatrixToRender = selectedNetworkModel.ErrorMatrix;
                            //_networksManager.SelectedNetworkModel.ErrorMatrix = errorMatrix.Next;
                            selectedNetworkModel.ErrorMatrix = errorMatrixToRender.Next;

                            //swRenderTime.Stop();
                            //RenderTime.ErrorMatrix = swRenderTime.Elapsed.Ticks;
                        }
                    }

                    if (isNetworksRenderNeeded)
                    {
                        isNetworksRendering = true;

                        //lock (ApplyChangesLocker)
                        {
                            //swRenderTime.Restart();

                            //selectedNetworkModel = selectedNetworkModel ?? _networksManager.SelectedNetworkModel;
                            networkModelToRender = selectedNetworkModel.GetCopyForRender();
                            CtlInputDataPresenter.SetInputStat(_networksManager.NetworkModels[0]);

                            //swRenderTime.Stop();
                            //RenderTime.Network = swRenderTime.Elapsed.Ticks;
                        }
                    }

                    if (isStatisticsRenderNeeded)
                    {
                        isStatisticsRendering = true;

                        //lock (ApplyChangesLocker)
                        {
                            //swRenderTime.Restart();

                            CtlPlotPresenter.Vanish(_networksManager.NetworkModels);

                            //selectedNetworkModel = selectedNetworkModel ?? _networksManager.SelectedNetworkModel;
                            //if (selectedNetworkModel == null)
                            //{
                            //    isStatisticsRendering = false;
                            //}
                            //else
                            {
                                statisticsToRender = selectedNetworkModel.Statistics.Copy();
                                learningRate = selectedNetworkModel.LearningRate;
                            }

                            //swRenderTime.Stop();
                            //RenderTime.Statistics = swRenderTime.Elapsed.Ticks;
                        }
                    }

                    Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)(() =>
                    {
                        if (isErrorMatrixRendering)
                        {
                            swRenderTime.Restart();

                            /*
                            ErrorMatrix errorMatrix = null;
                            
                            lock (ApplyChangesLocker)
                            {
                                swLock.Restart();

                                errorMatrix = _networksManager.SelectedNetworkModel.ErrorMatrix;
                                _networksManager.SelectedNetworkModel.ErrorMatrix = errorMatrix.Next;

                                swLock.Stop();
                                RenderTime.ErrorMatrix = swLock.Elapsed.Ticks;
                            }
                            */

                            CtlMatrixPresenter.Draw(errorMatrixToRender);
                            errorMatrixToRender.ClearData();

                            swRenderTime.Stop();
                            RenderTime.ErrorMatrix = swRenderTime.Elapsed.Ticks;

                            //isErrorMatrixRendering = false;
                        }

                        if (isNetworksRendering)
                        {
                            swRenderTime.Restart();

                            /*
                            NetworkDataModel networkModelCopy = null;

                            lock (ApplyChangesLocker)
                            {
                                swLock.Restart();

                                networkModelCopy = _networksManager.SelectedNetworkModel.GetCopyForRender();
                                CtlInputDataPresenter.SetInputStat(_networksManager.NetworkModels[0]);

                                swLock.Stop();
                                RenderTime.Network = swLock.Elapsed.Ticks;
                            }
                            */

                            DrawNetwork(networkModelToRender, CtlOnlyWeights.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn);

                            swRenderTime.Stop();
                            RenderTime.Network = swRenderTime.Elapsed.Ticks;

                            //isNetworksRendering = false;
                        }

                        if (isStatisticsRendering)
                        {
                            swRenderTime.Restart();

                            /*
                            NetworkDataModel selectedNetworkModel = null;
                            Statistics statisticsToRender = null;
                            double learningRate;

                            lock (ApplyChangesLocker)
                            {
                                swLock.Restart();

                                CtlPlotPresenter.Vanish(_networksManager.NetworkModels);

                                selectedNetworkModel = _networksManager.SelectedNetworkModel;
                                statisticsToRender = selectedNetworkModel?.Statistics.Copy();
                                learningRate = selectedNetworkModel == null ? 0 : selectedNetworkModel.LearningRate;

                                swLock.Stop();
                                RenderTime.Statistics = swLock.Elapsed.Ticks;
                            }
                            */

                            CtlPlotPresenter.Draw(_networksManager.NetworkModels, selectedNetworkModel);

                            var lastStats = DrawStatistics(statisticsToRender, learningRate);
                            //if (selectedNetworkModel != null)
                            {
                                selectedNetworkModel.LastStatistics = lastStats;
                            }

                            //isStatisticsRendering = false;

                            swRenderTime.Stop();
                            RenderTime.Statistics = swRenderTime.Elapsed.Ticks;
                        }

                        isErrorMatrixRendering = false;
                        isStatisticsRendering = false;
                        isNetworksRendering = false;

                        isRendering = false;

                    }));

                    Thread.Sleep(1);
                }

                swCurrentMiscCodeTime.Stop();
                currentMiscCodeTimeSpan = swCurrentMiscCodeTime.Elapsed;
            }

            _startTime.Stop();
            //Thread.EndThreadAffinity();
        }

        private void RunTimer()
        {
            //SetProcessorAffinity(Processor.Proc6);

            var prevTime = _startTime.Elapsed.Duration();

            while (!_cancellationToken.IsCancellationRequested)
            {
                var now = _startTime.Elapsed.Duration();
                if (now.Subtract(prevTime).TotalSeconds >= 1)
                {
                    prevTime = now;
                    Dispatcher.BeginInvoke((Action)(() => CtlTime.Content = "Time: " + _startTime.Elapsed.Duration().ToString(Culture.TimeFormat)));
                }

                Thread.Sleep(200);
            }
        }

        private void DrawNetwork(NetworkDataModel model, bool isOnlyWeights, bool isOnlyChangedWeights, bool isHighlightChangedWeights)
        {
            CtlNetworkPresenter.RenderRunning(model, isOnlyWeights, isOnlyChangedWeights, isHighlightChangedWeights);
            CtlInputDataPresenter.SetInputDataAndDraw(model);
        }

        private Dictionary<string, string> DrawStatistics(Statistics statistics, double learningRate)
        {
            if (statistics == null)
            {
                CtlStatisticsPresenter.Draw(null);
                return null;
            }

            var stat = new Dictionary<string, string>(20);
            stat.Add("Time", _startTime.Elapsed.Duration().ToString(Culture.TimeFormat));

            if (statistics.Percent > 0)
            {
                var linerRemains = (long)((double)statistics.TotalTicksElapsed * 100 / statistics.Percent) - statistics.TotalTicksElapsed;
                stat.Add("Leaner time remaining", TimeSpan.FromTicks(linerRemains).ToString(Culture.TimeFormat));
            }
            else
            {
                stat.Add("Time remaining", "N/A");
            }

            if (statistics.LastGoodOutput != null)
            {
                stat.Add("Last good output", $"{statistics.LastGoodInput}={statistics.LastGoodOutput} ({Converter.DoubleToText(100 * statistics.LastGoodOutputActivation, "N4")} %)");
                stat.Add("Last good cost", Converter.DoubleToText(statistics.LastGoodCost, "N6"));
            }
            else
            {
                stat.Add("Last good output", "none");
                stat.Add("Last good cost", "none");
            }

            if (statistics.LastBadOutput != null)
            {
                stat.Add("Last bad output", $"{statistics.LastBadInput}={statistics.LastBadOutput} ({Converter.DoubleToText(100 * statistics.LastBadOutputActivation, "N4")} %)");
                stat.Add("Last bad cost", Converter.DoubleToText(statistics.LastBadCost, "N6"));
            }
            else
            {
                stat.Add("Last bad output", "none");
                stat.Add("Last bad cost", "none");
            }

            stat.Add("Average cost", Converter.DoubleToText(statistics.CostAvg, "N6"));
            stat.Add("Percent", Converter.DoubleToText(statistics.Percent, "N6") + " %");
            stat.Add("Learning rate", Converter.DoubleToText(learningRate));
            stat.Add("Rounds", statistics.Rounds.ToString());

            //double totalRoundsPerSec = statistics.Rounds / _startTime.Elapsed.Duration().TotalSeconds;
            //stat.Add("Total / Max rounds/sec", ((int)totalRoundsPerSec).ToString() + " / " + ((int)statistics.MaxRoundsPerSecond).ToString());
            //stat.Add("Total pure rounds/sec", ((int)statistics.PureRoundsPerSecond).ToString());
            stat.Add("Current / Max pure rounds/sec", string.Format($"{(int)statistics.CurrentPureRoundsPerSecond} / {(int)statistics.MaxPureRoundsPerSecond}"));
            stat.Add("Current / Max lost rounds/sec", string.Format($"{(int)statistics.CurrentLostRoundsPerSecond} / {(int)statistics.MaxLostRoundsPerSecond}"));

            stat.Add(string.Empty, string.Empty);
            stat.Add("Render time, mcs", string.Empty);
            stat.Add("Network", ((int)TimeSpan.FromTicks(RenderTime.Network).TotalMicroseconds()).ToString());
            stat.Add("Error matrix", ((int)TimeSpan.FromTicks(RenderTime.ErrorMatrix).TotalMicroseconds()).ToString());
            stat.Add("Statistics", ((int)TimeSpan.FromTicks(RenderTime.Statistics).TotalMicroseconds()).ToString());
                
            //var lostRounds = (int)(statistics.PureRoundsPerSecond - totalRoundsPerSec);
            //stat.Add("Lost on render code rounds/sec", lostRounds.ToString());

            CtlStatisticsPresenter.Draw(stat);

            return stat;
        }

        private void CtlMenuNew_Click(object sender, EventArgs e)
        {
            CreateNetworksManager();
        }

        private void CtlMenuLoad_Click(object sender, EventArgs e)
        {
            LoadNetworksManager();
        }

        private void CtlMenuDeleteNetwork_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Would you really like to delete the network?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                DeleteNetworksManager();
            }
        }

        private bool StopRequest()
        {
            if (!IsRunning)
            {
                return true;
            }

            _runNetworksThread.Priority = ThreadPriority.Lowest;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            if (MessageBox.Show("Would you like to stop the network?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                StopRunning();
                return true;
            }

            return false;
        }

        private void CreateNetworksManager()
        {
            if (!StopRequest())
            {
                return;
            }

            var networksManager = new NetworksManager(CtlTabs, null, OnNetworkUIChanged);
            if (networksManager.Config != null)
            {
                _networksManager = networksManager;
                CtlInputDataPresenter.LoadConfig(_networksManager.Config, this);

                ReplaceNetworksManagerControl(_networksManager);
                if (_networksManager.IsValid())
                {
                    var fileName = Config.Main.GetString(Const.Param.NetworksManagerName, null);
                    SetTitle(fileName);

                    ApplyChangesToStandingNetworks();
                }
                else
                {
                    MessageBox.Show("Network parameter is not valid.", "Error");
                }
            }
        }

        private void LoadNetworksManager()
        {
            var loadDialog = new OpenFileDialog()
            {
                InitialDirectory = Path.GetFullPath("Networks\\"),
                DefaultExt = "txt",
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            };

            if (loadDialog.ShowDialog() == true)
            {
                if (!StopRequest())
                {
                    return;
                }

                LoadNetworksManager(loadDialog.FileName);
            }
        }

        private void DeleteNetworksManager()
        {
            var networksManagerName = Config.Main.GetString(Const.Param.NetworksManagerName);
            if (string.IsNullOrEmpty(networksManagerName))
            {
                return;
            }

            if (!File.Exists(networksManagerName))
            {
                networksManagerName = "\\Networks\\" + Path.GetFileName(networksManagerName);
            }

            if (File.Exists(networksManagerName))
            {
                File.Delete(networksManagerName);
            }

            ReplaceNetworksManagerControl(null);
        }

        private void ReplaceNetworksManagerControl(NetworksManager manager)
        {
            if (manager == null)
            {
                CtlNetworkName.Content = "...";

                CtlMenuStart.IsEnabled = false;
                CtlMenuReset.IsEnabled = false;
                CtlMainMenuSaveAs.IsEnabled = false;
                CtlMenuNetwork.IsEnabled = false;
                CtlNetworkContextMenu.IsEnabled = false;
            }
            else
            {
                CtlNetworkName.Content = Path.GetFileNameWithoutExtension(Config.Main.GetString(Const.Param.NetworksManagerName).Replace("_", "__"));

                CtlMenuStart.IsEnabled = true;
                CtlMenuReset.IsEnabled = true;
                CtlMainMenuSaveAs.IsEnabled = true;
                CtlMenuNetwork.IsEnabled = true;
                CtlNetworkContextMenu.IsEnabled = true;

                CtlPlotPresenter.Clear();
                CtlStatisticsPresenter.Clear();
                CtlMatrixPresenter.Clear();
            }

            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        private void CtlMenuStop_Click(object sender, RoutedEventArgs e)
        {
            StopRunning();
        }

        private void StopRunning()
        {
            _cancellationTokenSource.Cancel();

            if (_timeThread != null)
            {
                _timeThread.Join();
                _timeThread = null;
            }

            if (_runNetworksThread != null)
            {
                _runNetworksThread.Join();
                _runNetworksThread = null;
            }

            CtlMenuStart.IsEnabled = true;
            CtlMenuStop.IsEnabled = false;
            CtlMenuReset.IsEnabled = true;
        }

        private void CtlMenuReset_Click(object sender, RoutedEventArgs e)
        {
            ApplyChangesToStandingNetworks();
        }

        private void CtlApplyChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
                return;
            }

            if (IsRunning)
            {
                if (MessageBox.Show("Would you like running network to apply changes?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ApplyChangesToRunningNetworks();
                }
            }
            else
            {
                if (MessageBox.Show("Would you like network to apply changes?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ApplyChangesToStandingNetworks();
                }
            }
        }

        private void CtlTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // newly selected network must not affect NetworksManager until it saved

            if (_networksManager == null)
            {
                return;
            }

            if (IsRunning)
            {
                lock (ApplyChangesLocker)
                {
                    CtlInputDataPresenter.SetInputDataAndDraw(_networksManager.NetworkModels[0]);
                    CtlNetworkPresenter.RenderRunning(_networksManager.SelectedNetworkModel, CtlOnlyWeights.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn);
                    CtlPlotPresenter.Draw(_networksManager.NetworkModels, _networksManager.SelectedNetworkModel);
                    CtlStatisticsPresenter.Draw(_networksManager.SelectedNetworkModel.LastStatistics);
                }
            }
            else
            {
                CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (StopRequest())
            {
                try
                {
                    SaveConfig();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void CtlMainMenuNew_Click(object sender, RoutedEventArgs e)
        {
            CreateNetworksManager();
        }

        private void CtlMainMenuLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadNetworksManager();
        }

        private void CtlMainMenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (SaveConfig())
            {
                _networksManager.SaveAs();
            }
        }

        private void CtlMainMenuAddNetwork_Click(object sender, RoutedEventArgs e)
        {
            _networksManager.AddNetwork();
            ApplyChangesToStandingNetworks();
        }

        private void CtlMainMenuDeleteNetwork_Click(object sender, RoutedEventArgs e)
        {
            _networksManager.DeleteNetwork();
        }

        private void CtlMainMenuAddLayer_Click(object sender, RoutedEventArgs e)
        {
            _networksManager.SelectedNetworkControl.AddLayer();
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        private void CtlMainMenuDeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            _networksManager.SelectedNetworkControl.DeleteLayer();
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        private void CtlMainMenuAddNeuron_Click(object sender, RoutedEventArgs e)
        {
            _networksManager.SelectedNetworkControl.SelectedLayer.AddNeuron();
        }

        private void CtlApplySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void CtlCancelSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void CtlMenuRun_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            if (_networksManager.SelectedNetworkControl == null && CtlTabs.Items.Count > 1)
            {
                CtlTabs.SelectedIndex = 1;
            }

            CtlMenuStart.IsEnabled = !IsRunning && _networksManager.SelectedNetworkControl != null;
        }

        private void CtlMenuNetwork_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            CtlMainMenuDeleteNetwork.IsEnabled = CtlTabs.SelectedIndex > 0;
            CtlMainMenuAddLayer.IsEnabled = CtlTabs.SelectedIndex > 0;
            CtlMainMenuDeleteLayer.IsEnabled = CtlTabs.SelectedIndex > 0 && (CtlTabs.SelectedContent as NetworkControl).IsSelectedLayerHidden;
            CtlMainMenuAddNeuron.IsEnabled = CtlTabs.SelectedIndex > 0;
        }

        private void CtlNetworkContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            CtlMenuDeleteNetwork.IsEnabled = CtlTabs.SelectedIndex > 0;
        }

        public void TaskChanged()
        {
            CtlInputDataPresenter.NetworkTask.SetConfig(_networksManager.Config);
            CtlInputDataPresenter.NetworkTask.LoadConfig();

            TaskParameterChanged();
        }

        public void TaskParameterChanged()
        {
            _networksManager.RebuildNetworksForTask(CtlInputDataPresenter.NetworkTask);
            _networksManager.ResetLayersTabsNames();

            CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel);
        }

        public void Dispose()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
}

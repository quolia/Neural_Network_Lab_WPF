using Microsoft.Win32;
using Qualia.Model;
using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Qualia.Controls
{
    sealed public partial class Main : WindowResizeControl, IDisposable
    {
        private Thread _timeThread;
        private Thread _runNetworksThread;
        private CancellationToken _cancellationToken;
        private CancellationTokenSource _cancellationTokenSource;

        private NetworksManager _networksManager;

        private Stopwatch _startTime;
        private long _rounds;

        private readonly List<IConfigParam> _configParams;

        public Main()
        {
            Thread.CurrentThread.CurrentCulture = Culture.Current;
            Logger.Log("Application started.");

            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

            InitializeComponent();

            _configParams = new()
            {
                CtlSettings,

                new ConfigParamWrapper(CtlDynamicSettings)
                    .SetUIParam(Notification.ParameterChanged.DynamicSettings)
            };

            Loaded += MainWindow_OnLoaded;
        }

        private void SetTitle(string fileName)
        {
            Title = "Networks - " + Path.GetFileNameWithoutExtension(fileName) + "      " + fileName;
        }

        private void MainWindow_OnLoaded(object sender, EventArgs e)
        {
            FileHelper.InitWorkingDirectories();

            CtlNetworkPresenter.SizeChanged += (sender, e) =>
            {
                CtlNetworkPresenter.OnSizeChanged(_networksManager,
                                                  IsRunning,
                                                  CtlUseWeightsColors.Value,
                                                  CtlOnlyChangedWeights.Value,
                                                  CtlHighlightChangedWeights.Value,
                                                  CtlShowOnlyUnchangedWeights.Value,
                                                  CtlShowActivationLabels.Value);
            };

            LoadConfig();
        }

        private void LoadConfig()
        {
            LoadWindowSettings();
            LoadMainConfigParams();
            LoadNetworks();

            OnConfigLoaded();

            SetOnChangeEvent();

            TurnApplyChangesButton(Constants.State.Off);
        }

        private void LoadWindowSettings()
        {
            Width = Config.Main.Get(Constants.Param.ScreenWidth, SystemParameters.PrimaryScreenWidth);
            Height = Config.Main.Get(Constants.Param.ScreenHeight, SystemParameters.PrimaryScreenHeight);
            Top = Config.Main.Get(Constants.Param.ScreenTop, 0);
            Left = Config.Main.Get(Constants.Param.ScreenLeft, 0);
            Topmost = Config.Main.Get(Constants.Param.OnTop, false);
            DataWidth.Width = new(Config.Main.Get(Constants.Param.DataWidth, 200));
            NetworkHeight.Height = new(Config.Main.Get(Constants.Param.NetworkHeight, 500));

            WindowState = WindowState.Maximized;
        }

        private void LoadMainConfigParams()
        {
            _configParams.ForEach(p => p.SetConfig(Config.Main));
            _configParams.ForEach(p => p.LoadConfig());
        }

        private void LoadNetworks()
        {
            var fileName = Config.Main.Get(Constants.Param.NetworksManagerName, "");
            LoadNetworksManager(fileName);

            if (_networksManager != null)
            {
                TaskParameter_OnChanged();
            }
        }

        private void LoadNetworksManager(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            if (!File.Exists(fileName))
            {
                fileName = App.WorkingDirectory + "Networks" + Path.DirectorySeparatorChar + Path.GetFileName(fileName);
            }

            if (!File.Exists(fileName))
            {
                MessageBox.Show($"Network '{fileName}' is not found!", "Error", MessageBoxButton.OK);
                Config.Main.Set(Constants.Param.NetworksManagerName, fileName);
                return;
            }

            if (!StopRequest())
            {
                return;
            }

            _networksManager = new(CtlTabs, fileName, UI_OnChanged);
            Config.Main.Set(Constants.Param.NetworksManagerName, fileName);
            
            CtlInputDataPresenter.SetConfig(_networksManager.Config);
            CtlInputDataPresenter.LoadConfig();

            ReplaceNetworksManagerControl(_networksManager);
            if (!_networksManager.IsValid())
            {
                MessageBox.Show("Network parameter is not valid.", "Error");
                return;
            }

            SetTitle(fileName);
            ApplyChangesToStandingNetworks();
        }

        private void OnConfigLoaded()
        {
            CtlMenuRun.IsEnabled = _networksManager != null
                                   && _networksManager.NetworkModels != null
                                   && _networksManager.NetworkModels.Any();
        }

        private void SetOnChangeEvent()
        {
            _configParams.ForEach(p => p.SetOnChangeEvent(UI_OnChanged));

            CtlInputDataPresenter.SetOnChangeEvent(UI_OnChanged);
        }

        private bool SaveConfig()
        {
            Config.Main.Set(Constants.Param.ScreenWidth, ActualWidth);
            Config.Main.Set(Constants.Param.ScreenHeight, ActualHeight);
            Config.Main.Set(Constants.Param.ScreenTop, Top);
            Config.Main.Set(Constants.Param.ScreenLeft, Left);
            Config.Main.Set(Constants.Param.OnTop, Topmost);
            Config.Main.Set(Constants.Param.DataWidth, DataWidth.ActualWidth);
            Config.Main.Set(Constants.Param.NetworkHeight, NetworkHeight.ActualHeight);
            
            Config.Main.FlushToDrive();

            if (!_configParams.TrueForAll(p => p.IsValid()))
            {
                return false;
            }

            _configParams.ForEach(p => p.SaveConfig());
            Config.Main.FlushToDrive();

            if (_networksManager != null)
            {
                CtlInputDataPresenter.SaveConfig();

                if (!_networksManager.IsValid())
                {
                    MessageBox.Show("Network parameter is invalid", "Error");
                    return false;
                }

                _networksManager.SaveConfig();
            }
 
            return true;
        }

        private void UI_OnChanged(Notification.ParameterChanged param)
        {
            if (param == Notification.ParameterChanged.DynamicSettings)
            {
                //
            }
            else if (param == Notification.ParameterChanged.PreventComputerFromSleep)
            {
                CtlNoSleepLabel.Visibility = Visibility.Visible;
            }
            else if (param == Notification.ParameterChanged.DisablePreventComputerFromSleep)
            {
                CtlNoSleepLabel.Visibility = Visibility.Collapsed;
            }
            else if (param == Notification.ParameterChanged.NeuronsCount)
            {
                TurnApplyChangesButton(Constants.State.On);
                CtlMenuStart.IsEnabled = false;

                if (_networksManager != null)
                {
                    if (_networksManager.IsValid())
                    {
                        _networksManager.ResetLayersTabsNames();
                    }
                    else
                    {
                        TurnApplyChangesButton(Constants.State.Off);
                    }
                }

                var model = CtlInputDataPresenter.GetModel();

                if (model.TaskFunction != null && !model.TaskFunction.ITaskControl.IsValid())
                {
                    TurnApplyChangesButton(Constants.State.Off);
                }
            }
            else
            {
                TurnApplyChangesButton(Constants.State.On);
                CtlMenuStart.IsEnabled = false;
            }
        }

        private void TurnApplyChangesButton(Constants.State state)
        {
            if (state == Constants.State.On)
            {
                CtlApplyChanges.Background = Brushes.Yellow;
                CtlApplyChanges.IsEnabled = true;

                CtlCancelChanges.Background = Brushes.Yellow;
                CtlCancelChanges.IsEnabled = true;
            }
            else
            {
                CtlApplyChanges.Background = Brushes.White;
                CtlApplyChanges.IsEnabled = false;

                CtlCancelChanges.Background = Brushes.White;
                CtlCancelChanges.IsEnabled = false;
            }
        }

        private void ApplyChanges_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveConfig();
                WorkingModel.Current.RefreshAll(this, _networksManager);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
                return;
            }

            if (IsRunning)
            {
                if (MessageBox.Show("Would you like running networks to apply changes?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ApplyChangesToRunningNetworks();
                }
            }
            else
            {
                if (MessageBoxResult.Yes ==
                        MessageBox.Show("Would you like networks to apply changes?", "Confirm", MessageBoxButton.YesNo))
                {
                    ApplyChangesToStandingNetworks();
                }
            }
        }

        private void CancelChanges_OnClick(object sender, RoutedEventArgs e)
        {
            LoadConfig();
        }

        private void ApplyChangesToRunningNetworks()
        {
            lock (Locker.ApplyChanges)
            {
                var model = CtlInputDataPresenter.GetModel();
                model.TaskFunction.ITaskControl.ApplyChanges();
                CtlInputDataPresenter.RearrangeWithNewPointsCount();

                var newModels = _networksManager.CreateNetworksDataModels();
                _networksManager.MergeModels(newModels);

                CtlNetworkPresenter.ClearCache();
                CtlNetworkPresenter.RenderRunning(_networksManager.SelectedNetworkModel,
                                                  CtlUseWeightsColors.Value,
                                                  CtlOnlyChangedWeights.Value,
                                                  CtlHighlightChangedWeights.Value,
                                                  CtlShowOnlyUnchangedWeights.Value,
                                                  CtlShowActivationLabels.Value);

                TurnApplyChangesButton(Constants.State.Off);
            }
        }

        private void ApplyChangesToStandingNetworks()
        {
            lock (Locker.ApplyChanges)
            {
                var model = CtlInputDataPresenter.GetModel();
                model.TaskFunction.ITaskControl.ApplyChanges();
                CtlInputDataPresenter.RearrangeWithNewPointsCount();

                _networksManager.RefreshNetworksDataModels();
                CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel);

                TurnApplyChangesButton(Constants.State.Off);

                CtlMenuStart.IsEnabled = true;
                CtlMenuRun.IsEnabled = true;
            }
        }

        private bool IsRunning => CtlMenuStop.IsEnabled;

        private void MenuStart_OnClick(object sender, RoutedEventArgs e)
        {
            if (!SaveConfig())
            {
                return;
            }

            if (!_networksManager.NetworkModels.Any(model => model.IsEnabled))
            {
                MessageBox.Show("No one network is enabled.", "Info");
                return;
            }

            ApplyChangesToStandingNetworks();

            _cancellationTokenSource = new();
            _cancellationToken = _cancellationTokenSource.Token;

            CtlMenuStart.IsEnabled = false;
            CtlMenuReset.IsEnabled = false;
            CtlMenuStop.IsEnabled = true;
            CtlMenuDeleteNetwork.IsEnabled = false;

            _networksManager.PrepareModelsForRun();
            _networksManager.PrepareModelsForRound();

            var model = WorkingModel.Current.RefreshAll(this, _networksManager);

            //model.PrepareModelsForRun();
            //model.PrepareModelsForRound();

            CtlInputDataPresenter.SetInputDataAndDraw(_networksManager.SelectedNetworkModel);
            //CtlInputDataPresenter.SetInputDataAndDraw(model.SelectedNetwork);
            _networksManager.FeedForward(); // initialize state
            //model.FeedForward(); // initialize state

            DrawNetworkAndInputData(_networksManager.SelectedNetworkModel,
            //DrawNetworkAndInputData(model.SelectedNetwork,
                                    CtlUseWeightsColors.Value,
                                    CtlOnlyChangedWeights.Value,
                                    CtlHighlightChangedWeights.Value,
                                    CtlShowOnlyUnchangedWeights.Value,
                                    CtlShowActivationLabels.Value);

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForFullGCComplete();

            _rounds = 0;
            _startTime = Stopwatch.StartNew();

            _runNetworksThread = new(new ParameterizedThreadStart(RunNetworks))
            {
                Name = "RunNetworks",
                Priority = ThreadPriority.Highest,
                IsBackground = false
            };
            _runNetworksThread.SetApartmentState(ApartmentState.STA);
            _runNetworksThread.Start(new object[] { Threads.Processor.None });

            _timeThread = new(new ThreadStart(RunTimer))
            {
                Name = "RunTimer",
                Priority = ThreadPriority.Normal,
                IsBackground = true
            };
            _timeThread.Start();
        }

        unsafe private void RunNetworks(object args)
        {
            var arr = (object[])args;
            var processors = arr.Length > 0 ? (Threads.Processor)arr[0] : Threads.Processor.None;

            if (processors != Threads.Processor.None)
            {
                Threads.SetProcessorAffinity(processors);
            }

            Threads.SetThreadPriority(ThreadPriorityLevel.TimeCritical);

            //var model = WorkingModel.Current.RefreshAll(this, _networksManager);
            var settings = WorkingModel.Current.Settings;// CtlSettings.GetModel();

            var loopLimits = new LoopsLimit[3]
            {
                new(settings.SkipRoundsToDrawErrorMatrix),
                new(settings.SkipRoundsToDrawNetworks),
                new(settings.SkipRoundsToDrawStatistics)
            };

            var currentLoopLimit = LoopsLimit.Min(in loopLimits);

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

            SolutionsData solutionsData = null;

            Stopwatch swCurrentMiscCodeTime = new();
            Stopwatch swCurrentPureRoundsPerSecond = new();
            Stopwatch swRenderTime = new();
            
            var currentMiscCodeTimeSpan = TimeSpan.FromTicks(0);

            const double K1 = 1;
            const double K2 = 0;

            NetworkDataModel network;

            while (!_cancellationToken.IsCancellationRequested)
            {
                swCurrentPureRoundsPerSecond.Restart();

                lock (Locker.ApplyChanges)
                {
                    //model = WorkingModel.Current;

                    _networksManager.PrepareModelsForLoop();
                    //model.PrepareModelsForLoop();

                    for (int round = 0; round < currentLoopLimit; ++round)
                    {
                        _networksManager.PrepareModelsForRound();
                        //model.PrepareModelsForRound();

                        network = _networksManager.NetworkModels.First;
                        //network = model.Network;
                        while (network != null)
                        {
                            if (!network.IsEnabled)
                            {
                                network = network.Next;
                                continue;
                            }

                            network.FeedForward();
                            
                            var output = network.GetMaxActivatedOutputNeuron();
                            var outputId = output.Id;
                            var input = network.TargetOutput;
                            var cost = network.CostFunction.Do(network);
                            var statistics = network.Statistics;

                            statistics.LastInput = input;
                            statistics.LastOutput = outputId;

                            if (input == outputId)
                            {
                                ++statistics.CorrectRoundsTotal;
                                ++statistics.CorrectRounds;

                                statistics.LastGoodInput = network.Classes[input];
                                statistics.LastGoodOutput = network.Classes[outputId];
                                statistics.LastGoodOutputActivation = output.Activation;
                                statistics.LastGoodCost = cost;
                            }
                            else
                            {
                                statistics.LastBadInput = network.Classes[input];
                                statistics.LastBadOutput = network.Classes[outputId];
                                statistics.LastBadOutputActivation = output.Activation;
                                statistics.LastBadCost = cost;
                                statistics.LastBadTick = _startTime.Elapsed.Ticks;
                            }

                            statistics.CostSum += cost;
                            network.ErrorMatrix.AddData(input, outputId);

                            network.BackPropagationStrategy.OnError(network, input != outputId);

                            if (network.BackPropagationStrategy.IsBackPropagationNeeded(network))
                            {
                                network.BackPropagation();
                            }

                            network = network.Next;
                        }
                    }

                    network = _networksManager.NetworkModels.First;
                    //network = model.Network;
                    while (network != null)
                    {
                        if (!network.IsEnabled)
                        {
                            network = network.Next;
                            continue;
                        }

                        network.BackPropagationStrategy.OnAfterLoopFinished(network);
                        network = network.Next;
                    }

                    swCurrentPureRoundsPerSecond.Stop();
                    swCurrentMiscCodeTime.Restart();

                    _rounds += currentLoopLimit;

                    if (_rounds % settings.SkipRoundsToDrawStatistics == 0)
                    {
                        var totalTicksElapsed = _startTime.Elapsed.Ticks;

                        network = _networksManager.NetworkModels.First;
                        //network = model.Network;
                        while (network != null)
                        {
                            if (!network.IsEnabled)
                            {
                                network = network.Next;
                                continue;
                            }

                            var statistics = network.Statistics;

                            statistics.Rounds = _rounds;
                            statistics.TotalTicksElapsed = totalTicksElapsed;
                            statistics.CostSumTotal += statistics.CostSum;

                            var pureElapsed = swCurrentPureRoundsPerSecond.Elapsed;
                            statistics.CurrentPureRoundsPerSecond = currentLoopLimit / pureElapsed.TotalSeconds;
                            if (statistics.CurrentPureRoundsPerSecond > statistics.MaxPureRoundsPerSecond)
                            {
                                statistics.MaxPureRoundsPerSecond = statistics.CurrentPureRoundsPerSecond;
                            }
                            statistics.MicrosecondsPerPureRound = pureElapsed.TotalMicroseconds() / currentLoopLimit;

                            statistics.CurrentLostRoundsPerSecond = statistics.CurrentPureRoundsPerSecond * currentMiscCodeTimeSpan.TotalSeconds;
                            if (statistics.CurrentLostRoundsPerSecond > statistics.MaxLostRoundsPerSecond)
                            {
                                statistics.MaxLostRoundsPerSecond = statistics.CurrentLostRoundsPerSecond;
                            }

                            var percent = 100 * (double)statistics.CorrectRounds / settings.SkipRoundsToDrawStatistics;
                            var percentTotal = 100 * (double)statistics.CorrectRoundsTotal / statistics.Rounds;

                            statistics.Percent = percent * K1 + percentTotal * K2;

                            var costAvg = statistics.CostSum / settings.SkipRoundsToDrawStatistics;
                            var costAvgTotal = statistics.CostSumTotal / statistics.Rounds;
                            statistics.CostAvg = costAvg * K1 + costAvgTotal * K2;

                            if (statistics.CorrectRounds == currentLoopLimit)
                            {
                                if (statistics.First100PercentOnTick == 0)
                                {
                                    statistics.First100PercentOnTick = statistics.TotalTicksElapsed;
                                    statistics.First100PercentOnRound = statistics.Rounds;
                                }

                                if (statistics.Last100PercentOnTick == 0)
                                {
                                    statistics.Last100PercentOnTick = statistics.TotalTicksElapsed;
                                    statistics.Last100PercentOnRound = statistics.Rounds;
                                }
                            }
                            else
                            {
                                statistics.Last100PercentOnTick = 0;
                                statistics.Last100PercentOnRound = 0;
                            }

                            network.DynamicStatistics.Add(statistics.Percent, statistics.CostAvg);

                            statistics.CostSum = 0;
                            statistics.CorrectRounds = 0;
                            
                            network = network.Next;
                        }
                    }
                }

                int currentLimit = int.MaxValue;
                for (int i = 0; i < loopLimits.Length; ++i)
                {
                    var loopLimit = loopLimits[i];
                    loopLimit.CurrentLimit -= currentLoopLimit;
                    if (loopLimit.CurrentLimit <= 0)
                    {
                        loopLimit.CurrentLimit = loopLimit.OriginalLimit;
                    }

                    if (loopLimit.CurrentLimit < currentLimit)
                    {
                        currentLimit = loopLimit.CurrentLimit;
                    }
                }

                if (isRendering)
                {
                    isErrorMatrixRenderNeeded = false;
                    isNetworksRenderNeeded = false;
                    isStatisticsRenderNeeded = false;
                }
                else
                {
                    isErrorMatrixRenderNeeded = !isErrorMatrixRendering && _rounds % settings.SkipRoundsToDrawErrorMatrix == 0;
                    isNetworksRenderNeeded = !isNetworksRendering && _rounds % settings.SkipRoundsToDrawNetworks == 0;
                    isStatisticsRenderNeeded = !isStatisticsRendering && _rounds % settings.SkipRoundsToDrawStatistics == 0;
                }

                isRenderNeeded = isErrorMatrixRenderNeeded || isNetworksRenderNeeded || isStatisticsRenderNeeded;

                if (isRenderNeeded)
                {
                    isRendering = true;

                    NetworkDataModel selectedNetworkModel = _networksManager.SelectedNetworkModel;
                    //NetworkDataModel selectedNetworkModel = model.SelectedNetwork;
                    double learningRate = 0;

                    if (isErrorMatrixRenderNeeded)
                    {
                        isErrorMatrixRendering = true;

                        //lock (ApplyChangesLocker)
                        {
                            errorMatrixToRender = selectedNetworkModel.ErrorMatrix;
                            selectedNetworkModel.ErrorMatrix = errorMatrixToRender.Next;
                        }
                    }

                    if (isNetworksRenderNeeded)
                    {
                        isNetworksRendering = true;

                        //lock (ApplyChangesLocker)
                        {
                            //selectedNetworkModel.BlockWeights(networkModelToRender);

                            networkModelToRender = selectedNetworkModel.GetCopyToDraw();
                            CtlInputDataPresenter.SetInputStat(_networksManager.NetworkModels.First);
                            //CtlInputDataPresenter.SetInputStat(model.Network);
                        }
                    }

                    if (isStatisticsRenderNeeded)
                    {
                        isStatisticsRendering = true;

                        //lock (ApplyChangesLocker)
                        {
                            CtlPlotPresenter.OptimizePlotPointsCount(_networksManager.NetworkModels);
                            //CtlPlotPresenter.OptimizePlotPointsCount(model.Network);
                            {
                                statisticsToRender = selectedNetworkModel.Statistics.Copy();
                                learningRate = selectedNetworkModel.LearningRate;

                                solutionsData = WorkingModel.Current.RefreshDataPresenter().Task.SolutionsData;
                            }
                        }
                    }

                    Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        if (isErrorMatrixRendering)
                        {
                            swRenderTime.Restart();

                            CtlMatrixPresenter.DrawErrorMatrix(errorMatrixToRender,
                                                               statisticsToRender.LastInput,
                                                               statisticsToRender.LastOutput);
                            errorMatrixToRender.ClearData();

                            swRenderTime.Stop();
                            RenderTime.ErrorMatrix = swRenderTime.Elapsed.Ticks;
                        }

                        if (isNetworksRendering)
                        {
                            swRenderTime.Restart();

                            DrawNetworkAndInputData(networkModelToRender,
                                                    CtlUseWeightsColors.Value,
                                                    CtlOnlyChangedWeights.Value,
                                                    CtlHighlightChangedWeights.Value,
                                                    CtlShowOnlyUnchangedWeights.Value,
                                                    CtlShowActivationLabels.Value);

                            swRenderTime.Stop();
                            RenderTime.Network = swRenderTime.Elapsed.Ticks;
                        }

                        if (isStatisticsRendering)
                        {
                            swRenderTime.Restart();
                            CtlPlotPresenter.DrawPlot(_networksManager.NetworkModels, selectedNetworkModel);

                            var lastStats = DrawStatistics(statisticsToRender, learningRate);
                            selectedNetworkModel.LastStatistics = lastStats;

                            CtlTaskSolutionsPresenter.ShowSolutionsData(solutionsData);

                            swRenderTime.Stop();
                            RenderTime.Statistics = swRenderTime.Elapsed.Ticks;
                        }

                        isErrorMatrixRendering = false;
                        isStatisticsRendering = false;
                        isNetworksRendering = false;

                        isRendering = false;
                    });

                    Thread.Sleep(1);
                }

                swCurrentMiscCodeTime.Stop();
                currentMiscCodeTimeSpan = swCurrentMiscCodeTime.Elapsed;
            }

            _startTime.Stop();
        }

        private void RunTimer()
        {
            var prevTime = _startTime.Elapsed;

            while (!_cancellationToken.IsCancellationRequested)
            {
                var now = _startTime.Elapsed;
                if (now.Subtract(prevTime).TotalSeconds >= 1)
                {
                    prevTime = now;
                    Dispatcher.BeginInvoke(() =>
                    {
                        CtlTime.Content = "Time: " + _startTime.Elapsed.ToString(Culture.TimeFormat, Culture.Current);
                    });
                }

                Thread.Sleep(200);
            }
        }

        private void DrawNetworkAndInputData(NetworkDataModel model,
                                             bool isUseWeightsColors,
                                             bool isOnlyChangedWeights,
                                             bool isHighlightChangedWeights,
                                             bool isShowOnlyUnchangedWeights,
                                             bool isShowActivationLabels)
        {
            CtlNetworkPresenter.RenderRunning(model,
                                              isUseWeightsColors,
                                              isOnlyChangedWeights,
                                              isHighlightChangedWeights,
                                              isShowOnlyUnchangedWeights,
                                              isShowActivationLabels);

            CtlInputDataPresenter.SetInputDataAndDraw(model);
        }

        private Dictionary<string, string> DrawStatistics(Statistics statistics, double learningRate)
        {
            if (statistics == null)
            {
                CtlStatisticsPresenter.Draw(null);
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
                     _startTime.Elapsed.ToString(Culture.TimeFormat, Culture.Current) + " / " + remainingTime);

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

            stat.Add("Percent",
                     Converter.DoubleToText(statistics.Percent, "N6") + " %");

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

            stat.Add("Microseconds / pure round",
                     Converter.IntToText(statistics.MicrosecondsPerPureRound));

            stat.Add("Current / Max pure rounds/sec",
                     string.Format(Culture.Current,
                                   $"{(int)statistics.CurrentPureRoundsPerSecond} / {(int)statistics.MaxPureRoundsPerSecond}"));

            stat.Add("Current / Max lost rounds/sec",
                     string.Format(Culture.Current,
                                   $"{(int)statistics.CurrentLostRoundsPerSecond} / {(int)statistics.MaxLostRoundsPerSecond}"));

            stat.Add("6", null);

            stat.Add("Render time, mcsec", string.Empty);
            stat.Add("Network",
                     (Converter.IntToText(TimeSpan.FromTicks(RenderTime.Network).TotalMicroseconds())));

            stat.Add("Error matrix",
                     (Converter.IntToText(TimeSpan.FromTicks(RenderTime.ErrorMatrix).TotalMicroseconds())));

            stat.Add("Statistics",
                     (Converter.IntToText(TimeSpan.FromTicks(RenderTime.Statistics).TotalMicroseconds())));

            CtlStatisticsPresenter.Draw(stat);
            return stat;
        }

        private void MenuNew_OnClick(object sender, EventArgs e)
        {
            CreateNetworksManager();
        }

        private void MenuLoad_OnClick(object sender, EventArgs e)
        {
            LoadNetworksManager();
        }

        private void MenuDelete_OnClick(object sender, EventArgs e)
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

            NetworksManager networksManager = new(CtlTabs, null, UI_OnChanged);

            if (networksManager.Config != null)
            {
                _networksManager = networksManager;

                CtlInputDataPresenter.SetConfig(_networksManager.Config);
                CtlInputDataPresenter.LoadConfig();

                ReplaceNetworksManagerControl(_networksManager);
                if (!_networksManager.IsValid())
                {
                    MessageBox.Show("Network parameter is not valid.", "Error");
                    return;
                }

                var fileName = Config.Main.Get(Constants.Param.NetworksManagerName, "");
                SetTitle(fileName);

                ApplyChangesToStandingNetworks();
            }
        }

        private void LoadNetworksManager()
        {
            OpenFileDialog loadDialog = new()
            {
                InitialDirectory = App.WorkingDirectory + "Networks",
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
            var networksManagerName = Config.Main.Get(Constants.Param.NetworksManagerName, "");
            if (string.IsNullOrEmpty(networksManagerName))
            {
                return;
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
                CtlNetworkName.Content = Path.GetFileNameWithoutExtension(
                                                  Config.Main.Get(Constants.Param.NetworksManagerName, "")
                                                             .Replace("_", "__"));

                CtlMenuStart.IsEnabled = true;
                CtlMenuReset.IsEnabled = true;
                CtlMainMenuSaveAs.IsEnabled = true;
                CtlMenuNetwork.IsEnabled = true;
                CtlNetworkContextMenu.IsEnabled = true;

                CtlPlotPresenter.Clear();
                CtlStatisticsPresenter.Clear();
                CtlMatrixPresenter.Clear();
            }

            UI_OnChanged(Notification.ParameterChanged.Structure);
        }

        private void MenuStop_OnClick(object sender, RoutedEventArgs e)
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

        private void MenuReset_OnClick(object sender, RoutedEventArgs e)
        {
            ApplyChangesToStandingNetworks();
        }

        private void NetworkTab_OnChanged(object sender, SelectionChangedEventArgs e)
        {
            // newly selected network must not affect NetworksManager until it saved

            if (_networksManager == null)
            {
                return;
            }

            if (IsRunning)
            {
                lock (Locker.ApplyChanges)
                {
                    CtlInputDataPresenter.SetInputDataAndDraw(_networksManager.NetworkModels.First);

                    CtlNetworkPresenter.ClearCache();
                    CtlNetworkPresenter.RenderRunning(_networksManager.SelectedNetworkModel,
                                                      CtlUseWeightsColors.Value,
                                                      CtlOnlyChangedWeights.Value,
                                                      CtlHighlightChangedWeights.Value,
                                                      CtlShowOnlyUnchangedWeights.Value,
                                                      CtlShowActivationLabels.Value);

                    CtlPlotPresenter.DrawPlot(_networksManager.NetworkModels,
                                              _networksManager.SelectedNetworkModel);

                    CtlStatisticsPresenter.Draw(_networksManager.SelectedNetworkModel.LastStatistics);
                }
            }
            else
            {
                CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel);
            }
        }

        private void MainWindow_OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!StopRequest())
            {
                e.Cancel = true;
            }

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

        private void MainMenuNew_OnClick(object sender, RoutedEventArgs e)
        {
            CreateNetworksManager();
        }

        private void MainMenuLoad_OnClick(object sender, RoutedEventArgs e)
        {
            LoadNetworksManager();
        }

        private void MainMenuSaveAs_OnClick(object sender, RoutedEventArgs e)
        {
            if (SaveConfig())
            {
                NetworksManager.SaveAs();
            }
        }

        private void MainMenuAddNetwork_OnClick(object sender, RoutedEventArgs e)
        {
            _networksManager.AddNetwork();
            ApplyChangesToStandingNetworks();
        }

        private void MainMenuDeleteNetwork_OnClick(object sender, RoutedEventArgs e)
        {
            _networksManager.DeleteNetwork();
        }

        private void MainMenuAddLayer_OnClick(object sender, RoutedEventArgs e)
        {
            _networksManager.SelectedNetworkControl.AddLayer();
            UI_OnChanged(Notification.ParameterChanged.Structure);
        }

        private void MainMenuDeleteLayer_OnClick(object sender, RoutedEventArgs e)
        {
            _networksManager.SelectedNetworkControl.DeleteLayer();
            UI_OnChanged(Notification.ParameterChanged.Structure);
        }

        private void MainMenuAddNeuron_OnClick(object sender, RoutedEventArgs e)
        {
            _networksManager.SelectedNetworkControl.SelectedLayer.AddNeuron();
        }

        private void ApplySettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            //SaveSettings();
        }

        private void CancelSettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            //LoadSettings();
        }

        private void MenuRun_OnSubmenuOpened(object sender, RoutedEventArgs e)
        {
            if (_networksManager.SelectedNetworkControl == null && CtlTabs.Items.Count > 1)
            {
                CtlTabs.SelectedIndex = 1;
            }

            CtlMenuStart.IsEnabled = !IsRunning && _networksManager.SelectedNetworkControl != null;
        }

        private void MenuNetwork_OnSubmenuOpened(object sender, RoutedEventArgs e)
        {
            CtlMainMenuDeleteNetwork.IsEnabled = CtlTabs.SelectedIndex > 0;
            CtlMainMenuAddLayer.IsEnabled = CtlTabs.SelectedIndex > 0;
            CtlMainMenuDeleteLayer.IsEnabled = CtlTabs.SelectedIndex > 0 && (CtlTabs.SelectedContent as NetworkControl).IsSelectedLayerHidden;
            CtlMainMenuAddNeuron.IsEnabled = CtlTabs.SelectedIndex > 0;
        }

        private void NetworkContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            CtlMenuDeleteNetwork.IsEnabled = CtlTabs.SelectedIndex > 0;
        }

        public void TaskParameter_OnChanged()
        {
            var model = CtlInputDataPresenter.GetModel();
            _networksManager.RebuildNetworksForTask(model.TaskFunction);
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

        private void MenuVersion_OnClick(object sender, RoutedEventArgs e)
        {
            var (version, date) = VersionHelper.GetVersion();
            MessageBox.Show($"Version: {version}\n\nBuilt on: {date}\n\nAuthor: echoviser@gmail.com", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CtlNotes_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            CtlNotes.Save(FileHelper.NotesPath);
        }
    }
}

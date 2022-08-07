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
        public static Main Instance;

        private Thread _timeThread;
        private Thread _runNetworksThread;
        private CancellationToken _cancellationToken;
        private CancellationTokenSource _cancellationTokenSource;

        private NetworksManager _networksManager;
        private ActionsManager _applyChangesManager = ActionsManager.Instance;

        private Stopwatch _startTime;
        private long _rounds;

        public Main()
        {
            Instance = this;

            Thread.CurrentThread.CurrentCulture = Culture.Current;
            Logger.Log("Application started.");

            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

            InitializeComponent();

            CtlNoSleepLabel.Visibility = Visibility.Collapsed;

            this.SetConfigParams(new()
            {
                CtlSettings,

                new ConfigParamWrapper(CtlDynamicSettings)
                    .SetUIParam(Notification.ParameterChanged.DynamicSettings),

                CtlTaskSolutionsPresenter
                    .SetUIParam(Notification.ParameterChanged.DynamicSettings)
            });

            Loaded += MainWindow_OnLoaded;
        }

        private void SetTitle(string fileName)
        {
            Title = "Networks - " + Path.GetFileNameWithoutExtension(fileName) + "      " + fileName;
        }

        private void MainWindow_OnLoaded(object sender, EventArgs e)
        {
            Threads.SetThreadPriority(ThreadPriorityLevel.TimeCritical);

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

        public void ___OnNetworkStructureChanged()
        {
            TurnApplyChangesButtonOn(true);
            CtlMenuStart.IsEnabled = false;

            if (_networksManager != null)
            {
                if (_networksManager.IsValid())
                {
                    _networksManager.ResetLayersTabsNames();
                }
                else
                {
                    TurnApplyChangesButtonOn(false);
                }
            }

            var taskFunction = TaskFunction.GetInstance(CtlInputDataPresenter.CtlTaskFunction);
            if (taskFunction != null && !taskFunction.VisualControl.IsValid())
            {
                TurnApplyChangesButtonOn(false);
            }
        }

        private void LoadConfig()
        {
            LoadWindowSettings();
            LoadMainConfigParams();
            LoadNetworks();

            SetOnChangeEvent();
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
            this.GetConfigParams().ForEach(p => p.SetConfig(Config.Main));
            this.GetConfigParams().ForEach(p => p.LoadConfig());
        }

        private void LoadNetworks()
        {
            var fileName = Config.Main.Get(Constants.Param.NetworksManagerName, "");
            LoadNetworksManager(fileName);
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
                Config.Main.FlushToDrive();
                return;
            }

            if (!StopRequest())
            {
                return;
            }

            //UIManager.Clear();

            _networksManager = new(CtlTabs, fileName, UI_OnChanged);
            Config.Main.Set(Constants.Param.NetworksManagerName, fileName);
            Config.Main.FlushToDrive();

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

            OnNetworksManagerLoaded();
        }

        private void OnNetworksManagerLoaded()
        {
            CtlMenuRun.IsEnabled = _networksManager != null
                                   && _networksManager.NetworkModels != null
                                   && _networksManager.NetworkModels.Any();

            _applyChangesManager.Clear();
            //TurnApplyChangesButtonOn(false);
        }

        private void SetOnChangeEvent()
        {
            this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(UI_OnChanged));

            CtlInputDataPresenter.SetOnChangeEvent(UI_OnChanged);
        }

        private bool SaveConfigSafe()
        {
            try
            {
                return SaveConfig();
            }
            catch (Exception ex)
            {
                Logger.ShowException(ex, "Cannot save config file.");
                return false;
            }
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

            if (!this.GetConfigParams().TrueForAll(p => p.IsValid()))
            {
                return false;
            }

            this.GetConfigParams().ForEach(p => p.SaveConfig());
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

        private void UI_OnChanged(Notification.ParameterChanged param, ApplyAction action)
        {
            List<ApplyAction> additionalActions = new();

            if (param == Notification.ParameterChanged.DynamicSettings)
            {
                action = null;
            }
            else if (param == Notification.ParameterChanged.NoSleepMode)
            {
                action = null;

                var isNoSleepMode = CtlSettings.CtlIsNoSleepMode.Value;
                SystemTools.SetPreventComputerFromSleep(isNoSleepMode);
                CtlNoSleepLabel.Visibility = isNoSleepMode ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (param == Notification.ParameterChanged.Settings)
            {
                additionalActions.Add(CtlSettings.GetApplyAction());
            }
            else if (param == Notification.ParameterChanged.IsPreventRepetition)
            {
                action = null;

                var taskFunction = TaskFunction.GetInstance(CtlInputDataPresenter.CtlTaskFunction);
                taskFunction.VisualControl.SetIsPreventRepetition(CtlInputDataPresenter.CtlIsPreventRepetition.Value);
            }
            else if (param == Notification.ParameterChanged.IsNetworkEnabled)
            {
                additionalActions.Add(_networksManager.GetNetworksRefreshAction(false));
            }
            else if (param == Notification.ParameterChanged.NetworkColor)
            {
                additionalActions.Add(_networksManager.GetNetworksRefreshAction(false));
            }
            else if (param == Notification.ParameterChanged.NetworkRandomizerFunction
                     || param == Notification.ParameterChanged.NetworkRandomizerFunctionParam)
            {
                action = null;

                if (!IsRunning)
                {
                    _networksManager.RefresNetworks(IsRunning);
                    CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel);
                }
            }
            else if (param == Notification.ParameterChanged.NetworkLearningRate)
            {
                additionalActions.Add(_networksManager.GetNetworksRefreshAction(true));
            }
            else if (param == Notification.ParameterChanged.BackPropagationStrategy)
            {
                additionalActions.Add(_networksManager.GetNetworksRefreshAction(true));
            }
            else if (param == Notification.ParameterChanged.NeuronsCount)
            {
                ApplyAction newAction = new();

                if (IsRunning)
                {
                    newAction.InstantAction = () =>
                    {
                        _networksManager.ResetLayersTabsNames();
                    };

                    newAction.RunningAction = () =>
                    {
                        _networksManager.RefresNetworks(true);
                    };
                }
                else
                {
                    newAction.InstantAction = () =>
                    {
                        _networksManager.ResetLayersTabsNames();
                        _networksManager.RefresNetworks(false);
                        CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel);
                    };
                }

                newAction.CancelAction = newAction.InstantAction;
 
                _applyChangesManager.Add(newAction);



                //_applyChangesManager.Add(new()
                //{
                  //  CancelAction = _networksManager.ResetLayersTabsNames
                //});

                //OnNetworkStructureChanged();
            }
            else if (param == Notification.ParameterChanged.NeuronActivationFunction)
            {
                int a = 1;
            }
            else if (param == Notification.ParameterChanged.NeuronActivationFunctionParam)
            {
                int a = 1;
            }
            else if (param == Notification.ParameterChanged.Structure)
            {
                additionalActions.Add(new()
                {
                    RunningAction = ApplyChangesToRunningNetworks,
                    StandingAction = ApplyChangesToStandingNetworks
                });
            }
            else if (param == Notification.ParameterChanged.Invalidate)
            {

            }
            else // Default handler.
            {
                additionalActions.Add(new()
                {
                    RunningAction = ApplyChangesToRunningNetworks,
                    StandingAction = ApplyChangesToStandingNetworks
                });
            }

            if (action != null)
            {
                _applyChangesManager.Add(action);
            }

            _applyChangesManager.AddMany(additionalActions);

            if (param == Notification.ParameterChanged.Invalidate)
            {
                TurnApplyChangesButtonOn(false);

                if (_applyChangesManager.HasCancelActions())
                {
                    TurnCancelChangesButtonOn(true);
                }

                return;
            }

            lock (Locker.ApplyChanges)
            {
                _applyChangesManager.ExecuteInstant();
            }

            if (_applyChangesManager.HasApplyActions() || _applyChangesManager.HasCancelActions())
            {
                TurnApplyChangesButtonOn(true);
                CtlMenuStart.IsEnabled = false;
            }
            else
            {
                TurnCancelChangesButtonOn(false);
            }
        }

        public void TurnApplyChangesButtonOn(bool isOn)
        {
            if (isOn)
            {
                CtlApplyChanges.Background = Brushes.Yellow;
                CtlApplyChanges.IsEnabled = true;
            }
            else
            {
                CtlApplyChanges.Background = Brushes.White;
                CtlApplyChanges.IsEnabled = false;
            }

            TurnCancelChangesButtonOn(isOn);
        }

        public void TurnCancelChangesButtonOn(bool isOn)
        {
            if (isOn)
            {
                CtlCancelChanges.Background = Brushes.Yellow;
                CtlCancelChanges.IsEnabled = true;
            }
            else
            {
                CtlCancelChanges.Background = Brushes.White;
                CtlCancelChanges.IsEnabled = false;
            }
        }

        private void ApplyChanges_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBoxResult.Yes !=
                    MessageBox.Show("Confirm applying changes.", "Confirm", MessageBoxButton.YesNo))
            {
                return;
            }

            lock (Locker.ApplyChanges)
            {
                if (!SaveConfigSafe())
                {
                    return;
                }

                _applyChangesManager.Execute(IsRunning);

                TurnApplyChangesButtonOn(false);
                _applyChangesManager.Clear();

                if (!IsRunning)
                {
                    CtlMenuStart.IsEnabled = true;
                    CtlMenuRun.IsEnabled = true;
                }
            }
        }

        private void CancelChanges_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBoxResult.Yes ==
                    MessageBox.Show("Confirm cancel changes.", "Confirm", MessageBoxButton.YesNo))
            {
                lock (Locker.ApplyChanges)
                {
                    _applyChangesManager.ExecuteCancel();

                    TurnApplyChangesButtonOn(false);
                    _applyChangesManager.Clear();
                }
            }
        }

        private void ApplyChangesToRunningNetworks()
        {
            lock (Locker.ApplyChanges)
            {
                CtlSettings.ApplyChanges();

                var taskFunction = CtlInputDataPresenter.TaskFunction;
                taskFunction.VisualControl.ApplyChanges();

                CtlInputDataPresenter.RearrangeWithNewPointsCount();

                _networksManager.RebuildNetworksForTask(taskFunction);
                _networksManager.RefresNetworks(true);

                CtlNetworkPresenter.ClearCache();
                CtlNetworkPresenter.RenderRunning(_networksManager.SelectedNetworkModel,
                                                  CtlUseWeightsColors.Value,
                                                  CtlOnlyChangedWeights.Value,
                                                  CtlHighlightChangedWeights.Value,
                                                  CtlShowOnlyUnchangedWeights.Value,
                                                  CtlShowActivationLabels.Value);

                TurnApplyChangesButtonOn(false);
            }
        }

        private void ApplyChangesToStandingNetworks()
        {
            if (_networksManager is null)
            {
                return;    
            }

            lock (Locker.ApplyChanges)
            {
                CtlSettings.ApplyChanges();

                var taskFunction = CtlInputDataPresenter.TaskFunction;
                taskFunction.VisualControl.ApplyChanges();

                CtlInputDataPresenter.RearrangeWithNewPointsCount();

                _networksManager.RebuildNetworksForTask(taskFunction);
                _networksManager.RefresNetworks(false);

                CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel);

                TurnApplyChangesButtonOn(false);

                CtlMenuStart.IsEnabled = true;
                CtlMenuRun.IsEnabled = true;
            }
        }

        private bool IsRunning => CtlMenuStop.IsEnabled;

        private void MenuStart_OnClick(object sender, RoutedEventArgs e)
        {
            if (_applyChangesManager.HasActions())
            {
                MessageBox.Show("Apply or cancel changes!", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

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
            _applyChangesManager.Clear();

            _cancellationTokenSource = new();
            _cancellationToken = _cancellationTokenSource.Token;

            CtlMenuStart.IsEnabled = false;
            CtlMenuReset.IsEnabled = false;
            CtlMenuStop.IsEnabled = true;
            CtlMenuRemoveNetwork.IsEnabled = false;

            _networksManager.PrepareModelsForRun();
            _networksManager.PrepareModelsForRound();

            CtlInputDataPresenter.SetInputDataAndDraw(_networksManager.SelectedNetworkModel);
            _networksManager.FeedForward(); // initialize state

            DrawNetworkAndInputData(_networksManager.SelectedNetworkModel,
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

            Settings settings = null;
            int currentLoopLimit = 0;
            var loopLimits = new LoopsLimit[3];

            bool isErrorMatrixRendering = false;
            bool isNetworksRendering = false;
            bool isStatisticsRendering = false;

            bool isErrorMatrixRenderNeeded = false;
            bool isNetworksRenderNeeded = false;
            bool isStatisticsRenderNeeded = false;

            RendererStatistics.Instance.Reset();

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
                    if (settings != CtlSettings.Settings)
                    {
                        settings = CtlSettings.Settings;
                        loopLimits = new LoopsLimit[3]
                        {
                        new(settings.SkipRoundsToDrawErrorMatrix),
                        new(settings.SkipRoundsToDrawNetworks),
                        new(settings.SkipRoundsToDrawStatistics)
                        };

                        currentLoopLimit = LoopsLimit.Min(in loopLimits);
                    }

                    _networksManager.PrepareModelsForLoop();

                    for (int round = 0; round < currentLoopLimit; ++round)
                    {
                        _networksManager.PrepareModelsForRound();

                        network = _networksManager.NetworkModels.First;
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
                            var inputId = network.TargetOutputNeuronId;
                            var cost = network.CostFunction.Do(network);
                            var statistics = network.Statistics;

                            statistics.LastInput = inputId;
                            statistics.LastOutput = outputId;

                            if (inputId == outputId)
                            {
                                ++statistics.CorrectRoundsTotal;
                                ++statistics.CorrectRounds;

                                statistics.LastGoodInput = network.OutputClasses[inputId];
                                statistics.LastGoodOutput = network.OutputClasses[outputId];
                                statistics.LastGoodOutputActivation = output.Activation;
                                statistics.LastGoodCost = cost;
                            }
                            else
                            {
                                statistics.LastBadInput = network.OutputClasses[inputId];
                                statistics.LastBadOutput = network.OutputClasses[outputId];
                                statistics.LastBadOutputActivation = output.Activation;
                                statistics.LastBadCost = cost;
                                statistics.LastBadTick = _startTime.Elapsed.Ticks;
                            }

                            statistics.CostSum += cost;
                            network.ErrorMatrix.AddData(inputId, outputId);

                            network.BackPropagationStrategy.OnError(network, inputId != outputId);

                            if (network.BackPropagationStrategy.IsBackPropagationNeeded(network))
                            {
                                network.BackPropagation();
                            }

                            network = network.Next;
                        }
                    }

                    network = _networksManager.NetworkModels.First;
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

                    for (int i = 0; i < loopLimits.Length; ++i)
                    {
                        loopLimits[i].CurrentLimit -= currentLoopLimit;
                    }

                    if (loopLimits[LoopsLimit.STATISTICS].IsLimitReached)
                    {
                        var totalTicksElapsed = _startTime.Elapsed.Ticks;

                        network = _networksManager.NetworkModels.First;
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
                            if (statistics.Percent > statistics.MaxPercent)
                            {
                                statistics.MaxPercent = statistics.Percent;
                            }

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

                            if (!isStatisticsRendering)
                            {
                                network.PlotterStatistics.Add(statistics.Percent, statistics.CostAvg);
                            }

                            statistics.CostSum = 0;
                            statistics.CorrectRounds = 0;
                            
                            network = network.Next;
                        }
                    }
                }

                isErrorMatrixRenderNeeded = false;
                if (loopLimits[LoopsLimit.ERROR_MATRIX].IsLimitReached)
                {
                    isErrorMatrixRenderNeeded = !isErrorMatrixRendering;
                    if (!isErrorMatrixRenderNeeded)
                    {
                        ++RendererStatistics.Instance.ErrorMatrixFramesLost;
                    }
                    ++RendererStatistics.Instance.ErrorMatrixFrames;
                    loopLimits[LoopsLimit.ERROR_MATRIX].Reset();
                }

                isNetworksRenderNeeded = false;
                if (loopLimits[LoopsLimit.NETWORK].IsLimitReached)
                {
                    isNetworksRenderNeeded = !isNetworksRendering;
                    if (!isNetworksRenderNeeded)
                    {
                        ++RendererStatistics.Instance.NetworkFramesLost;
                    }
                    ++RendererStatistics.Instance.NetworkFrames;
                    loopLimits[LoopsLimit.NETWORK].Reset();
                }

                isStatisticsRenderNeeded = false;
                if (loopLimits[LoopsLimit.STATISTICS].IsLimitReached)
                {
                    isStatisticsRenderNeeded = !isStatisticsRendering;
                    if (!isStatisticsRenderNeeded)
                    {
                        ++RendererStatistics.Instance.StatisticsFramesLost;
                    }
                    ++RendererStatistics.Instance.StatisticsFrames;
                    loopLimits[LoopsLimit.STATISTICS].Reset();
                }

                currentLoopLimit = int.MaxValue;
                for (int i = 0; i < loopLimits.Length; ++i)
                {
                    var loopLimit = loopLimits[i];
                    if (loopLimit.CurrentLimit > 0 && loopLimit.CurrentLimit < currentLoopLimit)
                    {
                        currentLoopLimit = loopLimit.CurrentLimit;
                    }
                }

                NetworkDataModel firstNetworkModel = null;
                NetworkDataModel selectedNetworkModel = null;
                Statistics statisticsToRender = null;
                RendererStatistics statisticsAboutRender = RendererStatistics.Instance;

                Action doRenderErrorMatrix = null;
                Action doRenderNetwork = null;
                Action doRenderStatistics = null;

                if (isErrorMatrixRenderNeeded)
                {
                    isErrorMatrixRendering = true;

                    selectedNetworkModel = _networksManager.SelectedNetworkModel;
                    firstNetworkModel = _networksManager.NetworkModels.First;
                    statisticsToRender = selectedNetworkModel.Statistics.Copy();
                    var errorMatrixToRender = selectedNetworkModel.ErrorMatrix;
                    selectedNetworkModel.ErrorMatrix = errorMatrixToRender.Next;

                    doRenderErrorMatrix = () =>
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        swRenderTime.Restart();

                        CtlMatrixPresenter.DrawErrorMatrix(errorMatrixToRender,
                                                           statisticsToRender.LastInput,
                                                           statisticsToRender.LastOutput);
                        errorMatrixToRender.ClearData();

                        swRenderTime.Stop();
                        statisticsAboutRender.ErrorMatrixRenderTime = swRenderTime.Elapsed.Ticks;
                        if (statisticsAboutRender.ErrorMatrixRenderTime > statisticsAboutRender.ErrorMatrixRenderTimeMax)
                        {
                            statisticsAboutRender.ErrorMatrixRenderTimeMax = statisticsAboutRender.ErrorMatrixRenderTime;
                        }

                        isErrorMatrixRendering = false;
                    };
                }

                if (isNetworksRenderNeeded)
                {
                    isNetworksRendering = true;

                    selectedNetworkModel = selectedNetworkModel ?? _networksManager.SelectedNetworkModel;
                    firstNetworkModel = firstNetworkModel ?? _networksManager.NetworkModels.First;
                    var networkModelToRender = selectedNetworkModel.GetCopyToDraw();
                        
                    CtlInputDataPresenter.SetInputStat(firstNetworkModel);

                    doRenderNetwork = () =>
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        swRenderTime.Restart();

                        DrawNetworkAndInputData(networkModelToRender,
                                                CtlUseWeightsColors.Value,
                                                CtlOnlyChangedWeights.Value,
                                                CtlHighlightChangedWeights.Value,
                                                CtlShowOnlyUnchangedWeights.Value,
                                                CtlShowActivationLabels.Value);

                        swRenderTime.Stop();
                        statisticsAboutRender.NetworkRenderTime = swRenderTime.Elapsed.Ticks;
                        if (statisticsAboutRender.NetworkRenderTime > statisticsAboutRender.NetworkRenderTimeMax)
                        {
                            statisticsAboutRender.NetworkRenderTimeMax = statisticsAboutRender.NetworkRenderTime;
                        }

                        isNetworksRendering = false;
                    };
                }

                if (isStatisticsRenderNeeded)
                {
                    isStatisticsRendering = true;

                    selectedNetworkModel = selectedNetworkModel ?? _networksManager.SelectedNetworkModel;
                    firstNetworkModel = firstNetworkModel ?? _networksManager.NetworkModels.First;
                    double learningRate = 0;

                    CtlPlotPresenter.OptimizePlotPointsCount(firstNetworkModel);

                    statisticsToRender = statisticsToRender ?? selectedNetworkModel.Statistics.Copy();
                    learningRate = selectedNetworkModel.LearningRate;
                    var solutionsData = CtlInputDataPresenter.TaskFunction.GetSolutionsData();

                    doRenderStatistics = () =>
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        swRenderTime.Restart();
                        CtlPlotPresenter.DrawPlot(firstNetworkModel, selectedNetworkModel);

                        var lastStats = DrawStatistics(statisticsToRender, statisticsAboutRender.Copy(), learningRate);
                        selectedNetworkModel.LastStatistics = lastStats;

                        CtlTaskSolutionsPresenter.ShowSolutionsData(solutionsData);

                        swRenderTime.Stop();
                        statisticsAboutRender.StatisticsRenderTime = swRenderTime.Elapsed.Ticks;
                        if (statisticsAboutRender.StatisticsRenderTime > statisticsAboutRender.StatisticsRenderTimeMax)
                        {
                            statisticsAboutRender.StatisticsRenderTimeMax = statisticsAboutRender.StatisticsRenderTime;
                        }

                        isStatisticsRendering = false;
                    };
                }

                if (doRenderErrorMatrix != null || doRenderNetwork != null || doRenderStatistics != null)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, () =>
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        doRenderErrorMatrix?.Invoke();
                        doRenderNetwork?.Invoke();
                        doRenderStatistics?.Invoke();
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
                        CtlTime.Text = "Time: " + _startTime.Elapsed.ToString(Culture.TimeFormat, Culture.Current);
                    });
                }

                Thread.Sleep(200);
            }
        }

        private void DrawNetworkAndInputData(NetworkDataModel network,
                                             bool isUseWeightsColors,
                                             bool isOnlyChangedWeights,
                                             bool isHighlightChangedWeights,
                                             bool isShowOnlyUnchangedWeights,
                                             bool isShowActivationLabels)
        {
            CtlNetworkPresenter.RenderRunning(network,
                                              isUseWeightsColors,
                                              isOnlyChangedWeights,
                                              isHighlightChangedWeights,
                                              isShowOnlyUnchangedWeights,
                                              isShowActivationLabels);

            CtlInputDataPresenter.SetInputDataAndDraw(network);
        }

        private Dictionary<string, string> DrawStatistics(Statistics statistics, RendererStatistics statisticsAboutRender, double learningRate)
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

            CtlStatisticsPresenter.Draw(stat);
            return stat;
        }

        private void MenuNewManager_OnClick(object sender, EventArgs e)
        {
            CreateNetworksManager();
        }

        private void MenuLoadManager_OnClick(object sender, EventArgs e)
        {
            LoadNetworksManager();
        }

        /*
        private void MenuRemoveNetwork_OnClick(object sender, EventArgs e)
        {
            if (MessageBox.Show("Would you really like to remove the network?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                RemoveNetworksManager();
            }
        }
        */

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

        private void RemoveNetworksManager()
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
                CtlNetworkName.Text = "...";

                CtlMenuStart.IsEnabled = false;
                CtlMenuReset.IsEnabled = false;
                CtlMainMenuSaveAs.IsEnabled = false;
                CtlMenuNetwork.IsEnabled = false;
                CtlNetworkContextMenu.IsEnabled = false;
            }
            else
            {
                CtlNetworkName.Text = Path.GetFileNameWithoutExtension(Config.Main.Get(Constants.Param.NetworksManagerName, "")
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

            UI_OnChanged(Notification.ParameterChanged.Structure, null);
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
            // A newly selected network must not affect NetworksManager until it saved.
            
            if (_networksManager == null)
            {
                return;
            }

            var originalSource = e.OriginalSource as TabControl;
            if (originalSource != CtlTabs)
            {
                return;
            }

            if (IsRunning)
            {
                lock (Locker.ApplyChanges)
                {
                    var firstNetworkModel = _networksManager.NetworkModels.First;
                    var selectedNetworkModel = _networksManager.SelectedNetworkModel;

                    CtlInputDataPresenter.SetInputDataAndDraw(firstNetworkModel);

                    CtlNetworkPresenter.ClearCache();
                    CtlNetworkPresenter.RenderRunning(selectedNetworkModel,
                                                      CtlUseWeightsColors.Value,
                                                      CtlOnlyChangedWeights.Value,
                                                      CtlHighlightChangedWeights.Value,
                                                      CtlShowOnlyUnchangedWeights.Value,
                                                      CtlShowActivationLabels.Value);

                    CtlPlotPresenter.DrawPlot(firstNetworkModel,
                                              selectedNetworkModel);

                    CtlStatisticsPresenter.Draw(selectedNetworkModel.LastStatistics);
                }
            }
            else
            {
                CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel);
            }
        }

        private void MainWindow_OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_applyChangesManager.HasActions())
            {
                MessageBox.Show("Apply or cancel changes!", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                e.Cancel = true;
                return;
            }

            if (!StopRequest())
            {
                e.Cancel = true;
            }

            SaveConfigSafe();
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

        private void MainMenuCloneNetwork_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedNetwork = _networksManager.SelectedNetworkControl;
            var newNetwork = _networksManager.AddNetwork();

            ApplyChangesToStandingNetworks();

            selectedNetwork.CopyTo(newNetwork);

            ApplyChangesToStandingNetworks();
        }

        private void MainMenuRemoveNetwork_OnClick(object sender, RoutedEventArgs e)
        {
            _networksManager.RemoveNetwork();
        }

        private void MainMenuAddLayer_OnClick(object sender, RoutedEventArgs e)
        {
            _networksManager.SelectedNetworkControl.AddLayer();
            UI_OnChanged(Notification.ParameterChanged.Structure, null);
        }

        private void MainMenuRemoveLayer_OnClick(object sender, RoutedEventArgs e)
        {
            _networksManager.SelectedNetworkControl.RemoveLayer();
            UI_OnChanged(Notification.ParameterChanged.Structure, null);
        }

        private void MainMenuAddNeuron_OnClick(object sender, RoutedEventArgs e)
        {
            _networksManager.SelectedNetworkControl.SelectedLayer.AddNeuron();
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
            CtlMainMenuRemoveNetwork.IsEnabled = CtlTabs.SelectedIndex > 0;
            CtlMainMenuAddLayer.IsEnabled = CtlTabs.SelectedIndex > 0;
            CtlMainMenuRemoveLayer.IsEnabled = CtlTabs.SelectedIndex > 0 && (CtlTabs.SelectedContent as NetworkControl).IsSelectedLayerHidden;
            CtlMainMenuAddNeuron.IsEnabled = CtlTabs.SelectedIndex > 0;
        }

        private void NetworkContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            CtlMenuRemoveNetwork.IsEnabled = CtlTabs.SelectedIndex > 0;
            CtlMenuCloneNetwork.IsEnabled = CtlMenuRemoveNetwork.IsEnabled;
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
            MessageBox.Show($"Version: {version}\n\nBuilt on: {date}\n\nAuthor: echoviser@gmail.com",
                            "Info",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }

        private void Notes_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            CtlNotes.Save(FileHelper.NotesPath);
        }
    }
}

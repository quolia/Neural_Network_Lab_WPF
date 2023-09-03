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
        private const int _pauseSleepIntervalMilliseconds = 1000;

        public static Main Instance;

        private Thread _timeThread;
        private Thread _runNetworksThread;
        private CancellationToken _cancellationToken;
        private CancellationTokenSource _cancellationTokenSource;

        private NetworksManager _networksManager;

        private Stopwatch _startTime;
        private long _rounds;

        private volatile bool _isRunning;
        private volatile bool _isPaused;

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
                CtlNetworkPresenter.OnSizeChanged(_isRunning,
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

             ActionManager.Instance.Lock();

            _networksManager = new(CtlTabs, fileName, NotifyUIChanged);
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

            ActionManager.Instance.Unlock();
        }

        private void OnNetworksManagerLoaded()
        {
            CtlMenuRun.IsEnabled = _networksManager != null
                                   && _networksManager.NetworkModels != null
                                   && _networksManager.NetworkModels.Any();

            CtlTabs.SelectionChanged += NetworkTab_OnChanged;
        }

        private void SetOnChangeEvent()
        {
            this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(NotifyUIChanged));

            CtlInputDataPresenter.SetOnChangeEvent(NotifyUIChanged);
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
                    MessageBox.Show("Network parameter is invalid.", "Error");
                    return false;
                }

                _networksManager.SaveConfig();
            }

            return true;
        }

        bool _notify;
        private void NotifyUIChanged(ApplyAction action)
        {
            var param = action.Param;

            if (param == Notification.ParameterChanged.Unknown)
            {
                throw new InvalidOperationException();
            }

            if (_notify)
            {
                //throw new InvalidOperationException(); // this could be if some action causes another actions
            }

            try
            {
                _notify = true;

                if (action == null)
                {
                    throw new InvalidOperationException();
                }

                var manager = ActionManager.Instance;

                if (manager.IsLocked)
                {
                    return;
                }

                if (!manager.IsValid)
                {
                    if (action.Sender == manager.Invalidator && param != Notification.ParameterChanged.Invalidate)
                    {
                        manager.Invalidator = null;
                    }
                    else
                    {
                        if (manager.Invalidator != action.Sender)
                        {
                            if (action.Cancel != null)
                            {
                                Messages.ShowError("Cannot execute operation. Editor has invalid value.");

                                Dispatcher.BeginInvoke(() =>
                                {
                                    action.ExecuteCancel(_isRunning);
                                });
                            }

                            return;
                        }
                    }
                }

                List<ApplyAction> additionalActions = new();

                if (param == Notification.ParameterChanged.DynamicSettings)
                {
                    action = null;
                }
                else if (param == Notification.ParameterChanged.NoSleepMode)
                {
                    action = null;

                    var isNoSleepMode = CtlSettings.CtlIsNoSleepMode.Value;
                    SystemTools.SetNoSleepMode(isNoSleepMode);
                    CtlNoSleepLabel.Visibility = isNoSleepMode ? Visibility.Visible : Visibility.Collapsed;
                }
                else if (param == Notification.ParameterChanged.Settings)
                {
                    ApplyAction add = new(action.Sender)
                    {
                        Apply = (isRunning) => CtlSettings.ApplyChanges(isRunning)
                    };

                    additionalActions.Add(add);
                }
                else if (param == Notification.ParameterChanged.IsPreventRepetition)
                {
                    action = null;

                    var taskFunction = TaskFunction.GetInstance(CtlInputDataPresenter.CtlTaskFunction);
                    taskFunction.VisualControl.SetIsPreventRepetition(CtlInputDataPresenter.CtlIsPreventRepetition.Value);
                    //return;
                }
                else if (param == Notification.ParameterChanged.IsNetworkEnabled)
                {
                    _networksManager.SetNetworkEnabled(action.Sender);
                    action = null;
                }
                else if (param == Notification.ParameterChanged.NetworkColor)
                {
                    additionalActions.Add(_networksManager.GetNetworksRefreshAction(action.Sender, false));
                }
                else if (param == Notification.ParameterChanged.NetworkRandomizerFunction
                         || param == Notification.ParameterChanged.NetworkRandomizerFunctionParam)
                {
                    action = new(action.Sender)
                    {
                        ApplyInstant = (isRunning) =>
                        {
                            if (!isRunning)
                            {
                                _networksManager.RefreshNetworks(action.Sender);
                                CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel.GetCopyToDraw());
                            }
                        }
                    };
                }
                else if (param == Notification.ParameterChanged.NetworkLearningRate)
                {
                    additionalActions.Add(_networksManager.GetNetworksRefreshAction(action.Sender, true));
                }
                else if (param == Notification.ParameterChanged.BackPropagationStrategy)
                {
                    additionalActions.Add(_networksManager.GetNetworksRefreshAction(action.Sender, true));
                }
                else if (param == Notification.ParameterChanged.NeuronsAdded ||
                         param == Notification.ParameterChanged.NeuronsRemoved)
                {
                    ApplyAction newAction = new(this)
                    {
                        Apply = (isRunning) =>
                        {
                            _networksManager.RefreshNetworks(action.Sender);
                        },
                        ApplyInstant = (isRunning) =>
                        {
                            _networksManager.ResetLayersTabsNames();
                            if (!isRunning)
                            {
                                _networksManager.RefreshNetworks(action.Sender);
                                CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel.GetCopyToDraw());
                            }
                        }
                    };

                    ApplyAction cancel = new(this)
                    {
                        Cancel = newAction.ApplyInstant
                    };

                    manager.Add(cancel);

                    if (param == Notification.ParameterChanged.NeuronsAdded)
                    {
                        manager.Add(newAction);
                    }
                    else
                    {
                        additionalActions.Add(newAction);
                    }
                }
                else if (param == Notification.ParameterChanged.NeuronParam)
                {
                    additionalActions.Add(new(action.Sender)
                    {
                        Apply = (isRunning) =>
                        {
                            _networksManager.RefreshNetworks(action.Sender);
                        }
                    });
                }
                else if (param == Notification.ParameterChanged.NetworksCount)
                {
                    //
                }
                else if (param == Notification.ParameterChanged.NetworkUpdated)
                {
                    additionalActions.Add(new(this)
                    {
                        Apply = (isRunning) => _networksManager.RefreshNetworks(action.Sender)
                    });
                }
                else if (param == Notification.ParameterChanged.TaskParameter)
                {
                    additionalActions.Add(new(this)
                    {
                        Apply = (isRunning) => ApplyChangesToNetworks(isRunning)
                    });
                }
                else if (param == Notification.ParameterChanged.Invalidate)
                {
                    manager.Invalidator = action.Sender;
                }
                else if (param == Notification.ParameterChanged.TaskFunction)
                {
                    if (action.Apply != null)
                    {
                        additionalActions.Add(new(this)
                        {
                            Apply = (isRunning) => ApplyChangesToNetworks(isRunning)
                        });
                    }
                }
                else if (param == Notification.ParameterChanged.TaskDistributionFunction)
                {
                    //
                }
                else if (param == Notification.ParameterChanged.TaskDistributionFunctionParam)
                {
                    //
                }
                else // Default handler.
                {
                    throw new InvalidOperationException();
                }

                manager.Add(action);
                manager.AddMany(additionalActions);

                if (param == Notification.ParameterChanged.Invalidate)
                {
                    TurnApplyChangesButtonOn(false);
                    return;
                }

                lock (Locker.ApplyChanges)
                {
                    try
                    {
                        manager.ExecuteInstant(_isRunning);
                    }
                    catch (Exception ex)
                    {
                        Logger.ShowException(ex, "Cannot execute operation.");

                        Dispatcher.BeginInvoke(() =>
                        {
                            CancelActions();
                        });

                        return;
                    }
                }

                if (manager.HasApplyActions() || manager.HasCancelActions())
                {
                    TurnApplyChangesButtonOn(true);
                    CtlMenuStart.IsEnabled = false;
                }
                else
                {
                    TurnCancelChangesButtonOn(false);
                }
            }
            finally
            {
                _notify = false;
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

            TurnCancelChangesButtonOn(ActionManager.Instance.HasCancelActions());
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

                ActionManager.Instance.Execute(_isRunning);
                ApplyChangesToNetworks(_isRunning);
                ActionManager.Instance.Clear();

                TurnApplyChangesButtonOn(false);

                if (!_isRunning)
                {
                    CtlMenuStart.IsEnabled = true;
                    CtlMenuRun.IsEnabled = true;
                }

                SaveConfigSafe();
            }
        }

        private void ApplyChangesToNetworks(bool isRunning)
        {
            if (isRunning)
            {
                ApplyChangesToRunningNetworks();
            }
            else
            {
                ApplyChangesToStandingNetworks();
            }
        }

        private void CancelChanges_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBoxResult.Yes ==
                    MessageBox.Show("Confirm cancel changes.", "Confirm", MessageBoxButton.YesNo))
            {
                CancelActions();
            }
        }

        private void CancelActions()
        {
            lock (Locker.ApplyChanges)
            {
                ActionManager.Instance.Invalidator = null;
                ActionManager.Instance.ExecuteCancel(_isRunning);
                ActionManager.Instance.Clear();

                TurnApplyChangesButtonOn(false);
            }
        }

        private void ApplyChangesToRunningNetworks()
        {
            lock (Locker.ApplyChanges)
            {
                CtlSettings.ApplyChanges(true);

                var taskFunction = CtlInputDataPresenter.GetTaskFunction();
                taskFunction.VisualControl.ApplyChanges();

                CtlInputDataPresenter.ApplyChanges();

                _networksManager.RebuildNetworksForTask(taskFunction);
                _networksManager.RefreshNetworks(null);

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
                CtlSettings.ApplyChanges(false);

                var taskFunction = CtlInputDataPresenter.GetTaskFunction();
                taskFunction.VisualControl.ApplyChanges();

                CtlInputDataPresenter.ApplyChanges();

                _networksManager.RebuildNetworksForTask(taskFunction);
                _networksManager.RefreshNetworks(null);

                CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel.GetCopyToDraw());

                TurnApplyChangesButtonOn(ActionManager.Instance.HasApplyActions());

                CtlMenuStart.IsEnabled = !_isRunning && !_isPaused;
                CtlMenuRun.IsEnabled = true;
            }
        }

        private void MenuPause_OnClick(object sender, RoutedEventArgs e) 
        {
            CtlMenuContinue.IsEnabled = true;
            CtlMenuPause.IsEnabled = false;

            _isRunning = false;
            _isPaused = true;
            _startTime.Stop();
        }

        private void MenuContinue_OnClick(object sender, RoutedEventArgs e)
        {
            if (ActionManager.Instance.HasActions())
            {
                Messages.ShowApplyOrCancel();
                return;
            }

            CtlMenuContinue.IsEnabled = false;
            CtlMenuPause.IsEnabled = true;

            _isRunning = true;
            _isPaused = false;
            _startTime.Start();
        }

        private void MenuStart_OnClick(object sender, RoutedEventArgs e)
        {
            if (ActionManager.Instance.HasActions())
            {
                Messages.ShowApplyOrCancel();
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
            ActionManager.Instance.Execute(false);
            ActionManager.Instance.Clear();
            TurnApplyChangesButtonOn(false);

            _cancellationTokenSource = new();
            _cancellationToken = _cancellationTokenSource.Token;

            CtlMenuStart.IsEnabled = false;
            CtlMenuReset.IsEnabled = false;
            CtlMenuStop.IsEnabled = true;
            CtlMenuPause.IsEnabled = true;

            _isRunning = true;
            _isPaused = false;

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

            CtlStatisticsPresenter.Clear();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForPendingFinalizers();

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

            RendererStatistics.Reset();

            Stopwatch swCurrentMiscCodeTime = new();
            Stopwatch swCurrentPureRoundsPerSecond = new();
            Stopwatch swRenderTime = new();
            
            var currentMiscCodeTimeSpan = TimeSpan.FromTicks(0);

            NetworkDataModel network;

            while (!_cancellationToken.IsCancellationRequested)
            {
                if (_isPaused)
                {
                    Thread.Sleep(_pauseSleepIntervalMilliseconds);
                    continue;
                }

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

                            statistics.Percent = percent;
                            if (statistics.Percent > statistics.MaxPercent)
                            {
                                statistics.MaxPercent = statistics.Percent;
                            }

                            var costAvg = statistics.CostSum / settings.SkipRoundsToDrawStatistics;
                            var costAvgTotal = statistics.CostSumTotal / statistics.Rounds;
                            statistics.CostAvg = costAvg;

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
                                network.PlotterStatistics.Add(statistics.Percent, statistics.CostAvg, _startTime.Elapsed.Ticks);
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

                RendererStatistics statisticsAboutRender = RendererStatistics.Instance;

                Action doRenderErrorMatrix = null;
                Action doRenderNetwork = null;
                Action doRenderStatistics = null;

                if (isErrorMatrixRenderNeeded)
                {
                    isErrorMatrixRendering = true;

                    doRenderErrorMatrix = () =>
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        swRenderTime.Restart();

                        var network = _networksManager.SelectedNetworkModel;
                        var errorMatrixToRender = network.ErrorMatrix;
                        var statisticsToRender = network.Statistics.Copy();

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

                    doRenderNetwork = () =>
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        swRenderTime.Restart();

                        var network = _networksManager.SelectedNetworkModel;
                        var firstNetwork = _networksManager.NetworkModels.First;
                        var networkModelToRender = network.GetCopyToDraw();

                        CtlInputDataPresenter.SetInputStat(firstNetwork);

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

                    var solutionsData = CtlInputDataPresenter.TaskFunction.GetSolutionsData();

                    doRenderStatistics = () =>
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        var network = _networksManager.SelectedNetworkModel;
                        var firstNetwork = _networksManager.NetworkModels.First;
                        var statisticsToRender = network.Statistics.Copy();
                        var learningRate = network.LearningRate;

                        CtlPlotPresenter.OptimizePlotPointsCount(firstNetwork);

                        swRenderTime.Restart();
                        CtlPlotPresenter.DrawPlot(firstNetwork, network);

                        var lastStats = CtlStatisticsPresenter.DrawStatistics(statisticsToRender, statisticsAboutRender.Copy(), learningRate, _startTime.Elapsed);
                        network.LastStatistics = lastStats;

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
            _isRunning = false;
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

        private void MenuNewManager_OnClick(object sender, EventArgs e)
        {
            CreateNetworksManager();
        }

        private void MenuLoadManager_OnClick(object sender, EventArgs e)
        {
            LoadNetworksManager();
        }

        private bool StopRequest()
        {
            if (!_isRunning && !_isPaused)
            {
                return true;
            }

            if (MessageBox.Show("Would you like to stop the network?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                _runNetworksThread.Priority = ThreadPriority.Lowest;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

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

            NetworksManager networksManager = new(CtlTabs, null, NotifyUIChanged);

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

            //NotifyUIChanged(Notification.ParameterChanged.Structure, null);
        }

        private void MenuStop_OnClick(object sender, RoutedEventArgs e)
        {
            StopRunning();
        }

        private void StopRunning()
        {
            _cancellationTokenSource.Cancel();
            _isPaused = false;

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

            _isRunning = false;

            CtlMenuStart.IsEnabled = true;
            CtlMenuPause.IsEnabled = false;
            CtlMenuContinue.IsEnabled = false;
            CtlMenuStop.IsEnabled = false;
            CtlMenuReset.IsEnabled = true;
        }

        private void StopRunningFromThread()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, () =>
            {
                StopRunning();
            });
        }

        private void MenuReset_OnClick(object sender, RoutedEventArgs e)
        {
            ActionManager.Instance.Lock();
            ApplyChangesToStandingNetworks();
            ActionManager.Instance.Unlock();
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

            if (_isRunning)
            {
                lock (Locker.ApplyChanges)
                {
                    CtlNetworkPresenter.ClearCache();

                    /*
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

                    if (selectedNetworkModel != null)
                    {
                        CtlStatisticsPresenter.Draw(selectedNetworkModel.LastStatistics);
                    }
                    */
                }
            }
            else
            {
                var networkModel = _networksManager.SelectedNetworkModel;
                CtlNetworkPresenter.RenderStanding(networkModel?.GetCopyToDraw());
            }
        }

        private void MainWindow_OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ActionManager.Instance.HasActions())
            {
                Messages.ShowApplyOrCancel();
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
            if (!ActionManager.Instance.IsValid)
            {
                Messages.ShowError("Cannot add network. Editor has invalid value.");
                return;
            }

            lock (Locker.ApplyChanges)
            {
                ActionManager.Instance.Lock();

                _networksManager.AddNetwork();
                ApplyChangesToNetworks(_isRunning);
                
                ActionManager.Instance.Unlock();
            }
        }

        private void MainMenuCloneNetwork_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ActionManager.Instance.IsValid)
            {
                Messages.ShowError("Cannot clone network. Editor has invalid value.");
                return;
            }

            lock (Locker.ApplyChanges)
            {
                ActionManager.Instance.Lock();

                var selectedNetwork = _networksManager.SelectedNetworkControl;
                var newNetwork = _networksManager.AddNetwork();

                ApplyChangesToStandingNetworks();
                selectedNetwork.CopyTo(newNetwork);
                var newModel = _networksManager.CreateNetworkDataModel(newNetwork);
                _networksManager.MergeModel(newModel);
                ApplyChangesToStandingNetworks();

                ActionManager.Instance.Unlock();
            }
        }

        private void MainMenuRemoveNetwork_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ActionManager.Instance.IsValid)
            {
                Messages.ShowError("Cannot remove network. Editor has invalid value.");
                return;
            }

            _networksManager.RemoveNetwork();
        }

        private void MainMenuAddLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ActionManager.Instance.IsValid)
            {
                Messages.ShowError("Cannot add layer. Editor has invalid value.");
                return;
            }

            _networksManager.SelectedNetworkControl.AddLayer();
            //NotifyUIChanged(Notification.ParameterChanged.Structure, null);
        }

        private void MainMenuRemoveLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ActionManager.Instance.IsValid)
            {
                Messages.ShowError("Cannot remove layer. Editor has invalid value.");
                return;
            }

            _networksManager.SelectedNetworkControl.RemoveLayer();
            //NotifyUIChanged(Notification.ParameterChanged.Structure, null);
        }

        private void MainMenuAddNeuron_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ActionManager.Instance.IsValid)
            {
                Messages.ShowError("Cannot add neuron. Editor has invalid value.");
                return;
            }

            _networksManager.SelectedNetworkControl.SelectedLayer.AddNeuron();
        }

        private void MenuRun_OnSubmenuOpened(object sender, RoutedEventArgs e)
        {
            if (_networksManager.SelectedNetworkControl == null && CtlTabs.Items.Count > 1)
            {
                CtlTabs.SelectedIndex = 1;
            }

            _networksManager.RefreshSelectedNetworkTab();
            CtlMenuStart.IsEnabled = !_isRunning && !_isPaused && _networksManager.SelectedNetworkControl != null;
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
            //CtlMenuRemoveNetwork.IsEnabled = !_isRunning && CtlTabs.SelectedIndex > 0;
            //CtlMenuAddNetwork.IsEnabled = CtlMenuRemoveNetwork.IsEnabled;
            //CtlMenuCloneNetwork.IsEnabled = CtlMenuRemoveNetwork.IsEnabled;
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

using Microsoft.Win32;
using Qualia.Controls;
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
using Tools;
using static Tools.Threads;

namespace Qualia
{
    sealed public partial class Main : WindowResizeControl, INetworkTaskChanged, IDisposable
    {
        public static object ApplyChangesLocker = new();

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
            //SetProcessorAffinity(Processor.Proc1);
            //SetThreadPriority(ThreadPriorityLevel.Normal);

            Thread.CurrentThread.CurrentCulture = Culture.Current;
            Logger.Log("Application started.");

            InitializeComponent();

            WindowState = WindowState.Maximized;

            _configParams = new()
            {
                CtlUseWeightsColors,
                CtlOnlyChangedWeights,
                CtlHighlightChangedWeights,
                CtlShowOnlyUnchangedWeights
            };

            Loaded += Main_Load;
        }

        private void Main_Load(object sender, EventArgs e)
        {

            CreateDirectories();

            CtlNetworkPresenter.SizeChanged += NetworkPresenter_SizeChanged;

            LoadConfig();

            CtlMenuRun.IsEnabled = _networksManager != null && _networksManager.NetworkModels != null && _networksManager.NetworkModels.Any();
        }

        private void NetworkPresenter_SizeChanged(object sender, EventArgs e)
        {
            //CtlNetworkPresenter.Height = QMath.Max(CtlNetworkPresenter.Height, 400);

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
                    CtlNetworkPresenter.ClearCache();
                    CtlNetworkPresenter.RenderRunning(_networksManager.SelectedNetworkModel, CtlUseWeightsColors.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn, CtlShowOnlyUnchangedWeights.IsOn);
                }
                else
                {
                    CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel);
                }

            }, DispatcherPriority.Render);
        }

        private void LoadConfig()
        {
            Width = Config.Main.GetDouble(Constants.Param.ScreenWidth, SystemParameters.PrimaryScreenWidth).Value;
            Height = Config.Main.GetDouble(Constants.Param.ScreenHeight, SystemParameters.PrimaryScreenHeight).Value;
            Top = Config.Main.GetDouble(Constants.Param.ScreenTop, 0).Value;
            Left = Config.Main.GetDouble(Constants.Param.ScreenLeft, 0).Value;
            Topmost = Config.Main.GetBool(Constants.Param.OnTop, false);
            DataWidth.Width = new(Config.Main.GetDouble(Constants.Param.DataWidth, 100).Value);
            NetworkHeight.Height = new(Config.Main.GetDouble(Constants.Param.NetworkHeight, 200).Value);

            _configParams.ForEach(p => p.SetConfig(Config.Main));
            _configParams.ForEach(p => p.LoadConfig());

            var fileName = Config.Main.GetString(Constants.Param.NetworksManagerName, null);
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
                if (!StopRequest())
                {
                    return;
                }

                _networksManager = new(CtlTabs, fileName, OnNetworkUIChanged);
                Config.Main.Set(Constants.Param.NetworksManagerName, fileName);
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
                Config.Main.Set(Constants.Param.NetworksManagerName, string.Empty);
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

            var MNISTDirectory = "Networks" + Path.DirectorySeparatorChar + "MNIST";
            if (!Directory.Exists(MNISTDirectory))
            {
                Directory.CreateDirectory(MNISTDirectory);
            }
        }

        private void ToggleApplyChanges(Constants.Toggle state)
        {
            if (state == Constants.Toggle.On)
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
            ToggleApplyChanges(Constants.Toggle.On);
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
                        ToggleApplyChanges(Constants.Toggle.Off);
                    }
                }

                if (CtlInputDataPresenter.NetworkTask != null && !CtlInputDataPresenter.NetworkTask.IsValid())
                {
                    ToggleApplyChanges(Constants.Toggle.Off);
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

                CtlNetworkPresenter.ClearCache();
                CtlNetworkPresenter.RenderRunning(_networksManager.SelectedNetworkModel,
                                                  CtlUseWeightsColors.IsOn,
                                                  CtlOnlyChangedWeights.IsOn,
                                                  CtlHighlightChangedWeights.IsOn,
                                                  CtlShowOnlyUnchangedWeights.IsOn);

                ToggleApplyChanges(Constants.Toggle.Off);
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

                ToggleApplyChanges(Constants.Toggle.Off);

                CtlMenuStart.IsEnabled = true;
                CtlMenuRun.IsEnabled = true;
            }
        }

        private bool IsRunning => CtlMenuStop.IsEnabled;

        private void CtlMenuStart_Click(object sender, RoutedEventArgs e)
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
            CtlInputDataPresenter.SetInputDataAndDraw(_networksManager.SelectedNetworkModel);
            _networksManager.FeedForward(); // initialize state

            DrawNetwork(_networksManager.SelectedNetworkModel, CtlUseWeightsColors.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn, CtlShowOnlyUnchangedWeights.IsOn);

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
            _runNetworksThread.Start(new object[] { Processor.Proc2 | Processor.Proc3 } );

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
            var processors = (Processor)arr[0];

            //Thread.BeginThreadAffinity();
            //SetProcessorAffinity(processors);
            SetThreadPriority(ThreadPriorityLevel.TimeCritical);

            var loopLimits = new LoopsLimit[3]
            {
                new(Settings.SkipRoundsToDrawErrorMatrix),
                new(Settings.SkipRoundsToDrawNetworks),
                new(Settings.SkipRoundsToDrawStatistics)

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

            Stopwatch swCurrentMiscCodeTime = new();
            Stopwatch swCurrentPureRoundsPerSecond = new();
            Stopwatch swRenderTime = new();
            
            var currentMiscCodeTimeSpan = TimeSpan.FromTicks(0);

            const double K1 = 1;
            const double K2 = 0;

            while (!_cancellationToken.IsCancellationRequested)
            {
                swCurrentPureRoundsPerSecond.Restart();

                lock (ApplyChangesLocker)
                {
                    //swCurrentPureRoundsPerSecond.Restart();

                    for (int round = 0; round < currentLoopLimit; ++round)
                    {
                        _networksManager.PrepareModelsForRound();

                        var networkModel = _networksManager.NetworkModels.First;
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
                                statistics.LastBadTick = _startTime.Elapsed.Ticks;
                            }

                            statistics.CostSum += cost;
                            networkModel.ErrorMatrix.AddData(input, outputId);

                            networkModel.BackPropagation();

                            networkModel = networkModel.Next;
                        }
                    }

                    swCurrentPureRoundsPerSecond.Stop();
                    swCurrentMiscCodeTime.Restart();

                    _rounds += currentLoopLimit;

                    if (_rounds % Settings.SkipRoundsToDrawStatistics == 0)
                    {
                        var totalTicksElapsed = _startTime.Elapsed.Ticks;

                        var networkModel = _networksManager.NetworkModels.First;
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

                            var pureElapsed = swCurrentPureRoundsPerSecond.Elapsed;
                            statistics.CurrentPureRoundsPerSecond = currentLoopLimit / pureElapsed.TotalSeconds;
                            if (statistics.CurrentPureRoundsPerSecond > statistics.MaxPureRoundsPerSecond)
                            {
                                statistics.MaxPureRoundsPerSecond = statistics.CurrentPureRoundsPerSecond;
                            }
                            statistics.MicroSecondsPerPureRound = pureElapsed.TotalMicroseconds() / currentLoopLimit;

                            statistics.CurrentLostRoundsPerSecond = statistics.CurrentPureRoundsPerSecond * currentMiscCodeTimeSpan.TotalSeconds;
                            if (statistics.CurrentLostRoundsPerSecond > statistics.MaxLostRoundsPerSecond)
                            {
                                statistics.MaxLostRoundsPerSecond = statistics.CurrentLostRoundsPerSecond;
                            }

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
                            errorMatrixToRender = selectedNetworkModel.ErrorMatrix;
                            selectedNetworkModel.ErrorMatrix = errorMatrixToRender.Next;
                        }
                    }

                    if (isNetworksRenderNeeded)
                    {
                        isNetworksRendering = true;

                        //lock (ApplyChangesLocker)
                        {
                            networkModelToRender = selectedNetworkModel.GetCopyForRender();
                            CtlInputDataPresenter.SetInputStat(_networksManager.NetworkModels.First);
                        }
                    }

                    if (isStatisticsRenderNeeded)
                    {
                        isStatisticsRendering = true;

                        //lock (ApplyChangesLocker)
                        {
                            CtlPlotPresenter.Vanish(_networksManager.NetworkModels);
                            {
                                statisticsToRender = selectedNetworkModel.Statistics.Copy();
                                learningRate = selectedNetworkModel.LearningRate;
                            }
                        }
                    }

                    Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)(() =>
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        if (isErrorMatrixRendering)
                        {
                            swRenderTime.Restart();

                            CtlMatrixPresenter.Draw(errorMatrixToRender);
                            errorMatrixToRender.ClearData();

                            swRenderTime.Stop();
                            RenderTime.ErrorMatrix = swRenderTime.Elapsed.Ticks;
                        }

                        if (isNetworksRendering)
                        {
                            swRenderTime.Restart();

                            DrawNetwork(networkModelToRender, CtlUseWeightsColors.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn, CtlShowOnlyUnchangedWeights.IsOn);

                            swRenderTime.Stop();
                            RenderTime.Network = swRenderTime.Elapsed.Ticks;
                        }

                        if (isStatisticsRendering)
                        {
                            swRenderTime.Restart();
                            CtlPlotPresenter.Draw(_networksManager.NetworkModels, selectedNetworkModel);

                            var lastStats = DrawStatistics(statisticsToRender, learningRate);
                            selectedNetworkModel.LastStatistics = lastStats;

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
                    Dispatcher.BeginInvoke((Action)(() => CtlTime.Content = "Time: " + _startTime.Elapsed.ToString(Culture.TimeFormat)));
                }

                Thread.Sleep(200);
            }
        }

        private void DrawNetwork(NetworkDataModel model, bool isUseWeightsColors, bool isOnlyChangedWeights, bool isHighlightChangedWeights, bool isShowOnlyUnchangedWeights)
        {
            CtlNetworkPresenter.RenderRunning(model, isUseWeightsColors, isOnlyChangedWeights, isHighlightChangedWeights, isShowOnlyUnchangedWeights);
            CtlInputDataPresenter.SetInputDataAndDraw(model);
        }

        private Dictionary<string, string> DrawStatistics(Statistics statistics, double learningRate)
        {
            if (statistics == null)
            {
                CtlStatisticsPresenter.Draw(null);
                return null;
            }

            Dictionary<string, string> stat = new(24);

            stat.Add("Time",
                     _startTime.Elapsed.ToString(Culture.TimeFormat));

            if (statistics.Percent > 0)
            {
                var linerRemains = (long)((double)statistics.TotalTicksElapsed * 100 / statistics.Percent) - statistics.TotalTicksElapsed;
                stat.Add("Leaner time remaining",
                         TimeSpan.FromTicks(linerRemains).ToString(Culture.TimeFormat));
            }
            else
            {
                stat.Add("Time remaining", "N/A");
            }

            stat.Add("Learning rate",
                     Converter.DoubleToText(learningRate));

            stat.Add("1", null);

            if (statistics.LastGoodOutput != null)
            {
                stat.Add("Last good output",
                         $"{statistics.LastGoodInput}={statistics.LastGoodOutput} ({Converter.DoubleToText(100 * statistics.LastGoodOutputActivation, "N4")} %)");

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
                         $"{statistics.LastBadInput}={statistics.LastBadOutput} ({Converter.DoubleToText(100 * statistics.LastBadOutputActivation, "N4")} %)");

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
                     statistics.Rounds.ToString());

            stat.Add("Percent",
                     Converter.DoubleToText(statistics.Percent, "N6") + " %");

            stat.Add("5", null);

            stat.Add("Microseconds / pure round",
                     statistics.MicroSecondsPerPureRound.ToString());

            stat.Add("Current / Max pure rounds/sec",
                     string.Format($"{(int)statistics.CurrentPureRoundsPerSecond} / {(int)statistics.MaxPureRoundsPerSecond}"));

            stat.Add("Current / Max lost rounds/sec",
                     string.Format($"{(int)statistics.CurrentLostRoundsPerSecond} / {(int)statistics.MaxLostRoundsPerSecond}"));

            stat.Add("6", null);

            stat.Add("Render time, mcs", string.Empty);
            stat.Add("Network",
                     ((int)TimeSpan.FromTicks(RenderTime.Network).TotalMicroseconds()).ToString());

            stat.Add("Error matrix",
                     ((int)TimeSpan.FromTicks(RenderTime.ErrorMatrix).TotalMicroseconds()).ToString());

            stat.Add("Statistics",
                     ((int)TimeSpan.FromTicks(RenderTime.Statistics).TotalMicroseconds()).ToString());
                
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

            NetworksManager networksManager = new(CtlTabs, null, OnNetworkUIChanged);

            if (networksManager.Config != null)
            {
                _networksManager = networksManager;
                CtlInputDataPresenter.LoadConfig(_networksManager.Config, this);

                ReplaceNetworksManagerControl(_networksManager);
                if (_networksManager.IsValid())
                {
                    var fileName = Config.Main.GetString(Constants.Param.NetworksManagerName, null);
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
            OpenFileDialog loadDialog = new()
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
            var networksManagerName = Config.Main.GetString(Constants.Param.NetworksManagerName);
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
                CtlNetworkName.Content = Path.GetFileNameWithoutExtension(Config.Main.GetString(Constants.Param.NetworksManagerName).Replace("_", "__"));

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
                    CtlInputDataPresenter.SetInputDataAndDraw(_networksManager.NetworkModels.First);
                    CtlNetworkPresenter.ClearCache();
                    CtlNetworkPresenter.RenderRunning(_networksManager.SelectedNetworkModel, CtlUseWeightsColors.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn, CtlShowOnlyUnchangedWeights.IsOn);
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
                NetworksManager.SaveAs();
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

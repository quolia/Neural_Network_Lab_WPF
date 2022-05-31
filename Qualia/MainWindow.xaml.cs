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
using Tools;
using static Tools.Threads;
using System.Windows.Threading;
using Microsoft.Win32;
using Qualia.Controls;

namespace Qualia
{
    public partial class Main : WindowResizeControl, INetworkTaskChanged
    {
        public static object ApplyChangesLocker = new object();

        private Thread _timeThread;
        private Thread _runNetworkThread;
        private CancellationToken _cancellationToken;
        private CancellationTokenSource _cancellationTokenSource;

        private NetworksManager _networksManager;

        private Stopwatch _startTime;
        private long _round;

        private List<IConfigValue> _configParams;

        public Main()
        {
            SetProcessorAffinity(Processor.Proc0);
            SetThreadPriority(ThreadPriorityLevel.Highest);

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
            CreateDirectories();

            CtlNetworkPresenter.SizeChanged += NetworkPresenter_SizeChanged;

            LoadConfig();

            CtlMenuRun.IsEnabled = _networksManager != null && _networksManager.Models != null && _networksManager.Models.Any();
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

            }, DispatcherPriority.Background);
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

            var name = Config.Main.GetString(Const.Param.NetworksManagerName, null);
            LoadNetworksManager(name);
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

        private void LoadNetworksManager(string name)
        {
            if (!StopRequest())
            {
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (!File.Exists(name))
            {
                name = "\\Networks\\" + System.IO.Path.GetFileName(name);
            }

            if (File.Exists(name))
            {
                _networksManager = new NetworksManager(CtlTabs, name, OnNetworkUIChanged);
                Config.Main.Set(Const.Param.NetworksManagerName, name);
                CtlInputDataPresenter.LoadConfig(_networksManager.Config, this);

                ReplaceNetworksManagerControl(_networksManager);
                if (_networksManager.IsValid())
                {
                    ApplyChangesToStandingNetworks();
                }
                else
                {
                    MessageBox.Show("Network parameter is not valid.", "Error");
                }
            }
            else
            {
                MessageBox.Show($"Network '{name}' is not found!", "Error", MessageBoxButton.OK);
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

                if (CtlInputDataPresenter.Task != null && !CtlInputDataPresenter.Task.IsValid())
                {
                    ToggleApplyChanges(Const.Toggle.Off);
                }
            }
        }

        private void ApplyChangesToRunningNetworks()
        {
            lock (ApplyChangesLocker)
            {
                CtlInputDataPresenter.Task.ApplyChanges();
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
                CtlInputDataPresenter.Task.ApplyChanges();
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

                _round = 0;
                _startTime = Stopwatch.StartNew();

                _runNetworkThread = new Thread(new ParameterizedThreadStart(RunNetwork))
                {
                    Name = "RunNetwork",
                    Priority = ThreadPriority.Highest
                };
                _runNetworkThread.Start(new object[] { Processor.Proc1 } );

                _timeThread = new Thread(new ThreadStart(RunTimer))
                {
                    Name = "Timer",
                    Priority = ThreadPriority.AboveNormal
                };
                _timeThread.Start();
            }
        }

        unsafe private void RunNetwork(object args)
        {
            var arr = (object[])args;
            var processor = (Processor)arr[0];

            SetProcessorAffinity(processor);
            SetThreadPriority(ThreadPriorityLevel.Highest);

            var forLimit = new List<ForLimit>
            {
                new ForLimit(Settings.SkipRoundsToDrawErrorMatrix),
                new ForLimit(Settings.SkipRoundsToDrawNetworks),
                new ForLimit(Settings.SkipRoundsToDrawStatistics)
            };

            forLimit = forLimit.OrderBy(fl => fl.Current).ToList();

            bool IsErrorMatrixRendering = false;
            bool IsNetworkRendering = false;
            bool IsStatisticsRendering = false;

            var currentForLimit = forLimit[0];

            var swPureSpeed = new Stopwatch();
            var swLock = new Stopwatch();

            double k1 = 0.5;
            double k2 = 0.5;

            while (!_cancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(1);

                lock (ApplyChangesLocker)
                {
                    swPureSpeed.Start();

                    for (int i = 0; i < currentForLimit.Current; ++i)
                    {
                        _networksManager.PrepareModelsForRound();

                        var model = _networksManager.Models[0];
                        while (model != null)
                        {
                            if (!model.IsEnabled)
                            {
                                continue;
                            }

                            model.FeedForward();
                            
                            var output = model.GetMaxActivatedOutputNeuron();
                            var outputId = output.Id;
                            var input = model.TargetOutput;
                            var cost = model.CostFunction.Do(model);

                            if (input == outputId)
                            {
                                ++model.Statistics.CorrectRoundsTotal;
                                ++model.Statistics.CorrectRounds;

                                model.Statistics.LastGoodInput = model.Classes[input];
                                model.Statistics.LastGoodOutput = model.Classes[outputId];
                                model.Statistics.LastGoodOutputActivation = output.Activation;
                                model.Statistics.LastGoodCost = cost;
                            }
                            else
                            {
                                model.Statistics.LastBadInput = model.Classes[input];
                                model.Statistics.LastBadOutput = model.Classes[outputId];
                                model.Statistics.LastBadOutputActivation = output.Activation;
                                model.Statistics.LastBadCost = cost;
                                model.Statistics.LastBadTick = _startTime.Elapsed.Duration().Ticks;
                            }

                            model.Statistics.CostSum += cost;
                            model.ErrorMatrix.AddData(input, outputId);

                            model.BackPropagation();

                            model = model.Next;
                        }
                    }

                    swPureSpeed.Stop();

                    _round += currentForLimit.Current;
                    if (_round % Settings.SkipRoundsToDrawStatistics == 0)
                    {
                        var pureSpeedElapsedSeconds = swPureSpeed.Elapsed.Duration().TotalSeconds;
                        var totalTicksElapsed = _startTime.Elapsed.Duration().Ticks;

                        var m = _networksManager.Models[0];
                        while (m != null)
                        {
                            m.Statistics.Rounds = _round;
                            m.Statistics.TotalTicksElapsed = totalTicksElapsed;
                            m.Statistics.CostSumTotal += m.Statistics.CostSum;

                            m.Statistics.PureRoundsPerSecond = _round / pureSpeedElapsedSeconds;

                            var percent = 100 * (double)m.Statistics.CorrectRounds / Settings.SkipRoundsToDrawStatistics;
                            var percentTotal = 100 * (double)m.Statistics.CorrectRoundsTotal / _round;

                            k1 = 1;
                            k2 = 0;
                            m.Statistics.Percent = percent * k1 + percentTotal * k2;

                            var costAvg = m.Statistics.CostSum / Settings.SkipRoundsToDrawStatistics;
                            var costAvgTotal = m.Statistics.CostSumTotal / _round;
                            m.Statistics.CostAvg = costAvg * k1 + costAvgTotal * k2;

                            m.DynamicStatistics.Add(m.Statistics.Percent, m.Statistics.CostAvg);

                            m.Statistics.CostSum = 0;
                            m.Statistics.CorrectRounds = 0;
                            
                            m = m.Next;
                        }
                    }

                    if (forLimit.Count > 1)
                    {
                        for (int i = 1; i < forLimit.Count; ++i)
                        {
                            forLimit[i].Current -= currentForLimit.Current;
                        }
                        currentForLimit.Current = currentForLimit.Original;

                        forLimit.RemoveAll(fl => fl.Current == 0);
                        forLimit = forLimit.OrderBy(fl => fl.Current).ToList();
                        currentForLimit = forLimit[0];
                    }

                }

                var matrixNeeded = !IsErrorMatrixRendering && _round % Settings.SkipRoundsToDrawErrorMatrix == 0;
                var networkNeeded = !IsNetworkRendering && _round % Settings.SkipRoundsToDrawNetworks == 0;
                var statisticsNeeded = !IsStatisticsRendering && _round % Settings.SkipRoundsToDrawStatistics == 0;
                var anyNeeded = matrixNeeded || networkNeeded || statisticsNeeded;

                if (anyNeeded)
                {
                    if (matrixNeeded)
                    {
                        IsErrorMatrixRendering = true;
                    }

                    if (networkNeeded)
                    {
                        IsNetworkRendering = true;
                    }

                    if (statisticsNeeded)
                    {
                        IsStatisticsRendering = true;
                    }

                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (matrixNeeded)
                        {
                            ErrorMatrix errorMatrix;

                            lock (ApplyChangesLocker)
                            {
                                swLock.Restart();

                                errorMatrix = _networksManager.SelectedNetworkModel.ErrorMatrix;
                                _networksManager.SelectedNetworkModel.ErrorMatrix = errorMatrix.Next;

                                swLock.Stop();
                                RenderTime.ErrorMatrix = swLock.Elapsed.Ticks;
                            }

                            CtlMatrixPresenter.Draw(errorMatrix);
                            errorMatrix.ClearData();
                            IsErrorMatrixRendering = false;
                        }

                        if (networkNeeded)
                        {
                            NetworkDataModel modelCopy;

                            lock (ApplyChangesLocker)
                            {
                                swLock.Restart();

                                modelCopy = _networksManager.SelectedNetworkModel.GetCopyForRender();
                                CtlInputDataPresenter.SetInputStat(_networksManager.Models[0]);

                                swLock.Stop();
                                RenderTime.Network = swLock.Elapsed.Ticks;
                            }

                            DrawNetwork(modelCopy, CtlOnlyWeights.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn);

                            IsNetworkRendering = false;
                        }

                        if (statisticsNeeded)
                        {
                            NetworkDataModel selectedModel;
                            Statistics statistics;
                            double learningRate;

                            lock (ApplyChangesLocker)
                            {
                                swLock.Restart();

                                CtlPlotPresenter.Vanish(_networksManager.Models);

                                selectedModel = _networksManager.SelectedNetworkModel;
                                statistics = selectedModel?.Statistics.Copy();
                                learningRate = selectedModel == null ? 0 : selectedModel.LearningRate;

                                swLock.Stop();
                                RenderTime.Statistics = swLock.Elapsed.Ticks;
                            }

                            CtlPlotPresenter.Draw(_networksManager.Models, selectedModel);

                            var lastStats = DrawStatistics(statistics, learningRate);
                            if (selectedModel != null)
                            {
                                selectedModel.LastStatistics = lastStats;
                            }

                            IsStatisticsRendering = false;
                        }

                    }), DispatcherPriority.Render);
                }
            }

            _startTime.Stop();
        }

        private void RunTimer()
        {
            SetProcessorAffinity(Processor.Proc2);

            var prevTime = _startTime.Elapsed.Duration();

            while (!_cancellationToken.IsCancellationRequested)
            {
                var now = _startTime.Elapsed.Duration();
                if (now.Subtract(prevTime).TotalSeconds >= 1)
                {
                    prevTime = now;
                    Dispatcher.BeginInvoke((Action)(() => CtlTime.Content = "Time: " + _startTime.Elapsed.Duration().ToString(@"hh\:mm\:ss")));
                }

                Thread.Sleep(100);
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
            else
            {
                var stat = new Dictionary<string, string>(20);
                stat.Add("Time", _startTime.Elapsed.Duration().ToString(@"hh\:mm\:ss"));

                if (statistics.Percent > 0)
                {
                    var linerRemains = (long)((double)statistics.TotalTicksElapsed * 100 / statistics.Percent) - statistics.TotalTicksElapsed;
                    stat.Add("Leaner time remaining", TimeSpan.FromTicks(linerRemains).ToString(@"hh\:mm\:ss"));
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

                double totalRoundsPerSec = statistics.Rounds / _startTime.Elapsed.Duration().TotalSeconds; //DateTime.UtcNow.Subtract(StartTime).TotalSeconds;
                stat.Add("Total rounds/sec", ((int)totalRoundsPerSec).ToString());
                stat.Add("Total pure rounds/sec", ((int)statistics.PureRoundsPerSecond).ToString());

                stat.Add(string.Empty, string.Empty);
                stat.Add("Render lock time, mcs", string.Empty);
                stat.Add("Network", ((int)TimeSpan.FromTicks(RenderTime.Network).TotalMicroseconds()).ToString());
                stat.Add("Error matrix", ((int)TimeSpan.FromTicks(RenderTime.ErrorMatrix).TotalMicroseconds()).ToString());
                
                stat.Add("Statistics", ((int)TimeSpan.FromTicks(RenderTime.Statistics).TotalMicroseconds()).ToString());
                
                var lostRounds = (int)(statistics.PureRoundsPerSecond - totalRoundsPerSec);
                stat.Add("Lost rounds/sec on misc code", lostRounds.ToString());

                CtlStatisticsPresenter.Draw(stat);

                return stat;
            }
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

            _runNetworkThread.Priority = ThreadPriority.Lowest;
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

            var network = new NetworksManager(CtlTabs, null, OnNetworkUIChanged);
            if (network.Config != null)
            {
                _networksManager = network;
                CtlInputDataPresenter.LoadConfig(_networksManager.Config, this);

                ReplaceNetworksManagerControl(_networksManager);
                if (_networksManager.IsValid())
                {
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
            if (!StopRequest())
            {
                return;
            }

            var loadDialog = new OpenFileDialog
            {
                InitialDirectory = Path.GetFullPath("Networks\\"),
                DefaultExt = "txt",
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            };
            {
                if (loadDialog.ShowDialog() == true)
                {
                    LoadNetworksManager(loadDialog.FileName);
                }
            }
        }

        private void DeleteNetworksManager()
        {
            var name = Config.Main.GetString(Const.Param.NetworksManagerName);
            if (!String.IsNullOrEmpty(name))
            {
                if (!File.Exists(name))
                {
                    name = "\\Networks\\" + System.IO.Path.GetFileName(name);
                }

                if (File.Exists(name))
                {
                    File.Delete(name);
                }

                ReplaceNetworksManagerControl(null);
            }
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
                CtlNetworkName.Content = Path.GetFileNameWithoutExtension(Config.Main.GetString(Const.Param.NetworksManagerName));

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

            if (_runNetworkThread != null)
            {
                _runNetworkThread.Join();
                _runNetworkThread = null;
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

            if (_networksManager != null)
            {
                if (IsRunning)
                {
                    lock (ApplyChangesLocker)
                    {
                        CtlInputDataPresenter.SetInputDataAndDraw(_networksManager.Models[0]);
                        CtlNetworkPresenter.RenderRunning(_networksManager.SelectedNetworkModel, CtlOnlyWeights.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn);
                        CtlPlotPresenter.Draw(_networksManager.Models, _networksManager.SelectedNetworkModel);
                        CtlStatisticsPresenter.Draw(_networksManager.SelectedNetworkModel.LastStatistics);
                    }
                }
                else
                {
                    CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel);
                }
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
            _networksManager.SelectedNetwork.AddLayer();
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        private void CtlMainMenuDeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            _networksManager.SelectedNetwork.DeleteLayer();
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        private void CtlMainMenuAddNeuron_Click(object sender, RoutedEventArgs e)
        {
            _networksManager.SelectedNetwork.SelectedLayer.AddNeuron();
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
            if (_networksManager.SelectedNetwork == null && CtlTabs.Items.Count > 1)
            {
                CtlTabs.SelectedIndex = 1;
            }

            CtlMenuStart.IsEnabled = !IsRunning && _networksManager.SelectedNetwork != null;
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
            CtlInputDataPresenter.Task.SetConfig(_networksManager.Config);
            CtlInputDataPresenter.Task.LoadConfig();
            TaskParameterChanged();
        }

        public void TaskParameterChanged()
        {
            _networksManager.RebuildNetworksForTask(CtlInputDataPresenter.Task);
            _networksManager.ResetLayersTabsNames();
            CtlNetworkPresenter.RenderStanding(_networksManager.SelectedNetworkModel);
        }
    }
}

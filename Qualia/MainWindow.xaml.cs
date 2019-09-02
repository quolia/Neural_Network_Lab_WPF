using Microsoft.Win32;
using Qualia.Controls;
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

namespace Qualia
{
    public partial class Main : WindowResizeControl, INetworkTaskChanged
    {
        Thread TimeThread;
        Thread RunNetworkThread;
        CancellationToken CancellationToken;
        CancellationTokenSource CancellationTokenSource;
        public static object ApplyChangesLocker = new object();

        NetworksManager NetworksManager;

        Stopwatch StartTime;
        long Round;

        public Main()
        {
            SetProcessorAffinity(Processor.Proc7);
            SetThreadPriority(ThreadPriorityLevel.Highest);

            InitializeComponent();
            Loaded += Main_Load;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            //Config.Main.Clear();

            CreateDirectories();

            CtlNetworkPresenter.SizeChanged += NetworkPresenter_SizeChanged;

            LoadConfig();

            CtlMenuRun.IsEnabled = NetworksManager != null && NetworksManager.Models != null && NetworksManager.Models.Any();
        }

        private void NetworkPresenter_SizeChanged(object sender, EventArgs e)
        {
            CtlNetworkPresenter.Height = Math.Max(CtlNetworkPresenter.Height, 400);

            if (NetworksManager != null)
            {
                CtlNetworkPresenter.Dispatch(() =>
                {
                    if (IsRunning)
                        CtlNetworkPresenter.RenderRunning(NetworksManager.SelectedNetworkModel, CtlOnlyWeights.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn);
                    else
                        CtlNetworkPresenter.RenderStanding(NetworksManager.SelectedNetworkModel);
                });
            }
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
            CtlOnlyWeights.Load(Config.Main);
            CtlOnlyChangedWeights.Load(Config.Main);
            CtlHighlightChangedWeights.Load(Config.Main);

            var name = Config.Main.GetString(Const.Param.NetworksManagerName, null);
            LoadNetworksManager(name);
            LoadSettings();
        }

        private void LoadSettings()
        {
            CtlSettings.Load(Config.Main);
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
            CtlSettings.Save(Config.Main);
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

            if (String.IsNullOrEmpty(name))
            {
                return;
            }

            if (!File.Exists(name))
            {
                name = "\\Networks\\" + System.IO.Path.GetFileName(name);
            }

            if (File.Exists(name))
            {
                NetworksManager = new NetworksManager(CtlTabs, name, OnNetworkUIChanged);
                Config.Main.Set(Const.Param.NetworksManagerName, name);
                CtlInputDataPresenter.LoadConfig(NetworksManager.Config, this);

                ReplaceNetworksManagerControl(NetworksManager);
                if (NetworksManager.IsValid())
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
            CtlOnlyWeights.Save(Config.Main);
            CtlOnlyChangedWeights.Save(Config.Main);
            CtlHighlightChangedWeights.Save(Config.Main);

            if (!SaveSettings())
            {
                return false;
            }

            if (NetworksManager != null)
            {
                CtlInputDataPresenter.SaveConfig(NetworksManager.Config);

                if (!NetworksManager.IsValid())
                {
                    MessageBox.Show("Network parameter is invalid", "Error");
                    return false;
                }
                else
                {
                    NetworksManager.SaveConfig();
                }
            }

            Config.Main.FlushToDrive();
            if (NetworksManager != null)
            {
                NetworksManager.Config.FlushToDrive();
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
                if (NetworksManager != null)
                {
                    if (NetworksManager.IsValid())
                    {
                        NetworksManager.ResetLayersTabsNames();
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
                var newModels = NetworksManager.CreateNetworksDataModels();
                NetworksManager.MergeModels(newModels);
                CtlNetworkPresenter.RenderRunning(NetworksManager.SelectedNetworkModel, CtlOnlyWeights.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn);
                ToggleApplyChanges(Const.Toggle.Off);
            }
        }

        private void ApplyChangesToStandingNetworks()
        {
            lock (ApplyChangesLocker)
            {
                CtlInputDataPresenter.Task.ApplyChanges();
                CtlInputDataPresenter.RearrangeWithNewPointsCount();
                NetworksManager.RefreshNetworksDataModels();
                CtlNetworkPresenter.RenderStanding(NetworksManager.SelectedNetworkModel);
                ToggleApplyChanges(Const.Toggle.Off);
                CtlMenuStart.IsEnabled = true;
            }
        }

        private bool IsRunning => CtlMenuStop.IsEnabled;

        private void CtlMenuStart_Click(object sender, RoutedEventArgs e)
        {
            if (SaveConfig())
            {
                ApplyChangesToStandingNetworks();

                CancellationTokenSource = new CancellationTokenSource();
                CancellationToken = CancellationTokenSource.Token;

                CtlMenuStart.IsEnabled = false;
                CtlMenuReset.IsEnabled = false;
                CtlMenuStop.IsEnabled = true;
                CtlMenuDeleteNetwork.IsEnabled = false;

                NetworksManager.PrepareModelsForRun();

                NetworksManager.PrepareModelsForRound();
                CtlInputDataPresenter.SetInputDataAndDraw(NetworksManager.SelectedNetworkModel);
                NetworksManager.FeedForward(); // initialize state

                DrawNetwork(NetworksManager.SelectedNetworkModel, CtlOnlyWeights.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn);

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
                GC.WaitForFullGCComplete();

                Round = 0;
                StartTime = Stopwatch.StartNew();

                RunNetworkThread = new Thread(new ParameterizedThreadStart(RunNetwork))
                {
                    Name = "RunNetwork",
                    Priority = ThreadPriority.Highest
                };
                RunNetworkThread.Start(new object[] { Processor.Proc1 } );

                TimeThread = new Thread(new ThreadStart(RunTimer))
                {
                    Name = "Timer",
                    Priority = ThreadPriority.BelowNormal
                };
                TimeThread.Start();
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

            while (!CancellationToken.IsCancellationRequested)
            { 
                lock (ApplyChangesLocker)
                {
                    swPureSpeed.Start();
                    for (int i = 0; i < currentForLimit.Current; ++i)
                    {
                        NetworksManager.PrepareModelsForRound();
                        CtlInputDataPresenter.SetInputStat(NetworksManager.Models[0]);

                        var model = NetworksManager.Models[0];
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
                            }

                            model.Statistics.CostSum += cost;
                            model.ErrorMatrix.AddData(input, outputId);                          

                            model.BackPropagation();

                            model = model.Next;
                        }
                    }
                    swPureSpeed.Stop();

                    Round += currentForLimit.Current;
                                       
                    if (Round % Settings.SkipRoundsToDrawStatistics == 0)
                    {
                        var pureSpeedElapsedSeconds = swPureSpeed.Elapsed.Duration().TotalSeconds;

                        var m = NetworksManager.Models[0];
                        while (m != null)
                        {
                            m.Statistics.Rounds = Round;
                            m.Statistics.CostSumTotal += m.Statistics.CostSum;

                            m.Statistics.PureRoundsPerSecond = Round / pureSpeedElapsedSeconds;

                            var percent = 100 * (double)m.Statistics.CorrectRounds / Settings.SkipRoundsToDrawStatistics;
                            var percentTotal = 100 * (double)m.Statistics.CorrectRoundsTotal / Round;
                            m.Statistics.Percent = (percent + percentTotal) / 2;

                            var costAvg = m.Statistics.CostSum / Settings.SkipRoundsToDrawStatistics;
                            var costAvgTotal = m.Statistics.CostSumTotal / Round;
                            m.Statistics.CostAvg = (costAvg + costAvgTotal) / 2;

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

                var matrixNeeded = !IsErrorMatrixRendering && Round % Settings.SkipRoundsToDrawErrorMatrix == 0;
                var networkNeeded = !IsNetworkRendering && Round % Settings.SkipRoundsToDrawNetworks == 0;
                var statisticsNeeded = !IsStatisticsRendering && Round % Settings.SkipRoundsToDrawStatistics == 0;
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

                                errorMatrix = NetworksManager.SelectedNetworkModel.ErrorMatrix;
                                NetworksManager.SelectedNetworkModel.ErrorMatrix = errorMatrix.Next;

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

                                modelCopy = NetworksManager.SelectedNetworkModel.GetCopyForRender();

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

                                CtlPlotPresenter.Draw(NetworksManager.Models, NetworksManager.SelectedNetworkModel);

                                selectedModel = NetworksManager.SelectedNetworkModel;
                                statistics = selectedModel?.Statistics.Copy();
                                learningRate = selectedModel == null ? 0 : selectedModel.LearningRate;

                                swLock.Stop();
                                RenderTime.Statistics = swLock.Elapsed.Ticks;
                            }

                            var lastStats = DrawStatistics(statistics, learningRate);
                            if (selectedModel != null)
                            {
                                selectedModel.LastStatistics = lastStats;
                            }

                            IsStatisticsRendering = false;
                        }

                    }), System.Windows.Threading.DispatcherPriority.Send);
                }
            }

            StartTime.Stop();
        }

        private void RunTimer()
        {
            SetProcessorAffinity(Processor.Proc0);

            var prevTime = StartTime.Elapsed.Duration();

            while (!CancellationToken.IsCancellationRequested)
            {
                var now = StartTime.Elapsed.Duration();
                if (now.Subtract(prevTime).TotalSeconds >= 1)
                {
                    prevTime = now;
                    Dispatcher.BeginInvoke((Action)(() => CtlTime.Content = "Time: " + StartTime.Elapsed.Duration().ToString(@"hh\:mm\:ss")));
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
                var span = StartTime.Elapsed.Duration(); 
                stat.Add("Time", span.ToString(@"hh\:mm\:ss"));

                if (statistics.Percent > 0)
                {
                    var linerRemains = (long)((double)span.Ticks * 100 / statistics.Percent) - span.Ticks;
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

                double totalRoundsPerSec = statistics.Rounds / StartTime.Elapsed.Duration().TotalSeconds; //DateTime.UtcNow.Subtract(StartTime).TotalSeconds;
                stat.Add("Total rounds/sec", ((int)totalRoundsPerSec).ToString());
                stat.Add("Total pure rounds/sec", ((int)statistics.PureRoundsPerSecond).ToString());

                stat.Add(string.Empty, string.Empty);
                stat.Add("Render lock time, mcs", string.Empty);
                stat.Add("Network", ((int)TimeSpan.FromTicks(RenderTime.Network).TotalMicroseconds()).ToString());
                stat.Add("Error matrix", ((int)TimeSpan.FromTicks(RenderTime.ErrorMatrix).TotalMicroseconds()).ToString());
                //stat.Add("Plotter", ((int)TimeSpan.FromTicks(RenderTime.Plotter).TotalMicroseconds()).ToString());
                stat.Add("Statistics", ((int)TimeSpan.FromTicks(RenderTime.Statistics).TotalMicroseconds()).ToString());
                //stat.Add("Data", ((int)TimeSpan.FromTicks(RenderTime.Data).TotalMicroseconds()).ToString());
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

            RunNetworkThread.Priority = ThreadPriority.Lowest;
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
                NetworksManager = network;
                CtlInputDataPresenter.LoadConfig(NetworksManager.Config, this);

                ReplaceNetworksManagerControl(NetworksManager);
                if (NetworksManager.IsValid())
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
            }

            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        private void CtlMenuStop_Click(object sender, RoutedEventArgs e)
        {
            StopRunning();
        }

        private void StopRunning()
        {
            CancellationTokenSource.Cancel();

            if (TimeThread != null)
            {
                TimeThread.Join();
                TimeThread = null;
            }

            if (RunNetworkThread != null)
            {
                RunNetworkThread.Join();
                RunNetworkThread = null;
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

            if (NetworksManager != null)
            {
                if (IsRunning)
                {
                    lock (ApplyChangesLocker)
                    {
                        CtlInputDataPresenter.SetInputDataAndDraw(NetworksManager.Models[0]);
                        CtlNetworkPresenter.RenderRunning(NetworksManager.SelectedNetworkModel, CtlOnlyWeights.IsOn, CtlOnlyChangedWeights.IsOn, CtlHighlightChangedWeights.IsOn);
                        CtlPlotPresenter.Draw(NetworksManager.Models, NetworksManager.SelectedNetworkModel);
                        CtlStatisticsPresenter.Draw(NetworksManager.SelectedNetworkModel.LastStatistics);
                    }
                }
                else
                {
                    CtlNetworkPresenter.RenderStanding(NetworksManager.SelectedNetworkModel);
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
                NetworksManager.SaveAs();
            }
        }

        private void CtlMainMenuAddNetwork_Click(object sender, RoutedEventArgs e)
        {
            NetworksManager.AddNetwork();
            ApplyChangesToStandingNetworks();
        }

        private void CtlMainMenuDeleteNetwork_Click(object sender, RoutedEventArgs e)
        {
            NetworksManager.DeleteNetwork();
        }

        private void CtlMainMenuAddLayer_Click(object sender, RoutedEventArgs e)
        {
            NetworksManager.SelectedNetwork.AddLayer();
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        private void CtlMainMenuDeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            NetworksManager.SelectedNetwork.DeleteLayer();
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        private void CtlMainMenuAddNeuron_Click(object sender, RoutedEventArgs e)
        {
            NetworksManager.SelectedNetwork.SelectedLayer.AddNeuron();
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
            if (NetworksManager.SelectedNetwork == null && CtlTabs.Items.Count > 1)
            {
                CtlTabs.SelectedIndex = 1;
            }

            CtlMenuStart.IsEnabled = NetworksManager.SelectedNetwork != null;
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
            CtlInputDataPresenter.Task.Load(NetworksManager.Config);
            TaskParameterChanged();
        }

        public void TaskParameterChanged()
        {
            NetworksManager.RebuildNetworksForTask(CtlInputDataPresenter.Task);
            NetworksManager.ResetLayersTabsNames();
            CtlNetworkPresenter.RenderStanding(NetworksManager.SelectedNetworkModel);
        }
    }
}

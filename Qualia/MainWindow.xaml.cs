using Microsoft.Win32;
using Qualia.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tools;

namespace Qualia
{
    public partial class Main : WindowResizeControl, INetworkTaskChanged
    {
        Thread WorkThread;
        Thread TimeThread;
        CancellationToken CancellationToken;
        CancellationTokenSource CancellationTokenSource;
        public static SharedLock ApplyChangesLocker = new SharedLock();

        NetworksManager NetworksManager;

        DateTime StartTime;
        long Round;

        AutoResetEvent UIEvent = new AutoResetEvent(false);

        public Main()
        {
            Threads.SetProcessorAffinity(Threads.Processor.Proc0);

            InitializeComponent();
            Loaded += Main_Load;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            //Config.Main.Clear();

            CreateDirectories();

            CtlNetworkPresenter.SizeChanged += NetworkPresenter_SizeChanged;

            LoadConfig();
        }

        private void NetworkPresenter_SizeChanged(object sender, EventArgs e)
        {
            CtlNetworkPresenter.Height = Math.Max(CtlNetworkPresenter.Height, 400);

            if (NetworksManager != null)
            {
                CtlNetworkPresenter.Dispatch(() =>
                {
                    if (IsRunning)
                        CtlNetworkPresenter.RenderRunning(NetworksManager.SelectedNetworkModel);
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
            NetworksManager.Config.FlushToDrive();

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
            using (ApplyChangesLocker.GetLocker(Threads.Processor.GUI))
            {
                CtlInputDataPresenter.Task.ApplyChanges();
                CtlInputDataPresenter.RearrangeWithNewPointsCount();
                var newModels = NetworksManager.CreateNetworksDataModels();
                NetworksManager.MergeModels(newModels);
                CtlNetworkPresenter.RenderRunning(NetworksManager.SelectedNetworkModel);
                ToggleApplyChanges(Const.Toggle.Off);
            }
        }

        private void ApplyChangesToStandingNetworks()
        {
            using (ApplyChangesLocker.GetLocker(Threads.Processor.GUI))
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

                DrawNetwork(NetworksManager.SelectedNetworkModel);

                WorkThread = new Thread(new ThreadStart(RunNetwork));
                WorkThread.Priority = ThreadPriority.Highest;
                WorkThread.Start();

                TimeThread = new Thread(new ThreadStart(RunTimer));
                TimeThread.Priority = ThreadPriority.BelowNormal;
                TimeThread.Start();
            }
        }

        private void RunNetwork()
        {
            var processor = Threads.Processor.Proc1;

            Threads.SetProcessorAffinity(processor);

            Round = 0;
            StartTime = DateTime.UtcNow;
            var speedTime = DateTime.UtcNow;
            bool IsErrorMatrixRendering = false;

            while (!CancellationToken.IsCancellationRequested)
            {
                using (ApplyChangesLocker.GetLocker(processor))
                {
                    using (var locker = ApplyChangesLocker.GetLocker(processor, Threads.Processor.Proc1))
                    {
                        if (locker.IsActionAllowed)
                        {
                            NetworksManager.PrepareModelsForRound();
                        }
                    }

                    foreach (var model in NetworksManager.Models)
                    {
                        if (!model.IsEnabled)
                        {
                            continue;
                        }

                        //GPU.Instance.FeedForward(model);
                        model.FeedForward();

                        var output = model.GetMaxActivatedOutputNeuron();
                        var input = model.TargetOutput;
                        var cost = model.CostFunction.Do(model);
                        if (input == output.Id)
                        {
                            ++model.Statistics.CorrectRounds;

                            model.Statistics.LastGoodInput = model.Classes[input];
                            model.Statistics.LastGoodOutput = model.Classes[output.Id];
                            model.Statistics.LastGoodOutputActivation = output.Activation;
                            model.Statistics.LastGoodCost = cost;
                        }
                        else
                        {
                            model.Statistics.LastBadInput = model.Classes[input];
                            model.Statistics.LastBadOutput = model.Classes[output.Id];
                            model.Statistics.LastBadOutputActivation = output.Activation;
                            model.Statistics.LastBadCost = cost;
                        }

                        model.ErrorMatrix.AddData(input, output.Id);

                        ++model.Statistics.Rounds;

                        model.BackPropagation();

                        if (model.Statistics.Rounds == 1)
                        {
                            model.Statistics.AverageCost = cost;
                        }
                        else
                        {
                            model.Statistics.AverageCost = (model.Statistics.AverageCost * (model.Statistics.Rounds - 1) + cost) / model.Statistics.Rounds;
                        }
                    }

                    ++Round;
                }

                if (!IsErrorMatrixRendering && Round % Settings.SkipRoundsToDrawErrorMatrix == 0)
                {
                    IsErrorMatrixRendering = true;

                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        ErrorMatrix errorMatrix;
                        using (ApplyChangesLocker.GetLocker(Threads.Processor.GUI))
                        {
                            errorMatrix = NetworksManager.SelectedNetworkModel.ErrorMatrix;
                            NetworksManager.ResetErrorMatrix();
                        }
                        CtlMatrixPresenter.Draw(errorMatrix);
                        IsErrorMatrixRendering = false;

                    }), System.Windows.Threading.DispatcherPriority.Send);
                }

                if (Round % Settings.SkipRoundsToDrawNetworks == 0)
                {
                    UIEvent.Reset();
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        using (ApplyChangesLocker.GetLocker(Threads.Processor.GUI))
                        {
                            DrawNetwork(NetworksManager.SelectedNetworkModel);
                        }
                        UIEvent.Set();

                    }), System.Windows.Threading.DispatcherPriority.Send);
                    UIEvent.WaitOne();
                }

                if (Round % Settings.SkipRoundsToDrawStatistics == 0)
                {
                    UIEvent.Reset();
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        using (ApplyChangesLocker.GetLocker(Threads.Processor.GUI))
                        {
                            DrawPlotter(NetworksManager.Models);
                        }
                        UIEvent.Set();

                    }), System.Windows.Threading.DispatcherPriority.Send);
                    UIEvent.WaitOne();


                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        NetworkDataModel selectedModel;
                        Statistics statistics;
                        double learningRate;

                        using (ApplyChangesLocker.GetLocker(Threads.Processor.GUI))
                        {
                            selectedModel = NetworksManager.SelectedNetworkModel;
                            statistics = selectedModel == null ? null : selectedModel.Statistics.Copy();
                            learningRate = selectedModel == null ? 0 : selectedModel.LearningRate;
                        }

                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            var lastStats = DrawStatistics(statistics, learningRate, speedTime);
                            if (selectedModel != null)
                            {
                                selectedModel.LastStatistics = lastStats;
                            }

                            speedTime = DateTime.UtcNow;

                        }), System.Windows.Threading.DispatcherPriority.Send);

                    }), System.Windows.Threading.DispatcherPriority.Send);
                }
            }
        }

        private void RunTimer()
        {
            Threads.SetProcessorAffinity(Threads.Processor.Proc0);

            DateTime prevTime = DateTime.UtcNow;

            while (!CancellationToken.IsCancellationRequested)
            {
                if ((long)DateTime.UtcNow.Subtract(prevTime).TotalSeconds >= 1)
                {
                    prevTime = DateTime.UtcNow;
                    Dispatcher.BeginInvoke((Action)(() => CtlTime.Content = "Time: " + DateTime.UtcNow.Subtract(StartTime).ToString(@"hh\:mm\:ss")));
                }

                Thread.Sleep(100);
            }
        }

        private void DrawNetwork(NetworkDataModel model)
        {
            CtlNetworkPresenter.RenderRunning(model);
            CtlInputDataPresenter.SetInputDataAndDraw(model);
        }

        private void DrawPlotter(List<NetworkDataModel> models)
        {
            models.ForEach(m => m.DynamicStatistics.Add(m.Statistics.Percent, m.Statistics.AverageCost));
            CtlPlotPresenter.Draw(models, NetworksManager.SelectedNetworkModel);
        }

        private Dictionary<string, string> DrawStatistics(Statistics statistics, double learningRate, DateTime speedTime)
        {
            if (statistics == null)
            {
                CtlStatisticsPresenter.Draw(null);
                return null;
            }
            else
            {
                var sw = Stopwatch.StartNew();

                var stat = new Dictionary<string, string>();
                var span = DateTime.UtcNow.Subtract(StartTime);
                stat.Add("Time", new DateTime(span.Ticks).ToString(@"HH\:mm\:ss"));

                if (statistics.Percent > 0)
                {
                    var remains = new DateTime((long)(span.Ticks * 100 / statistics.Percent) - span.Ticks);
                    stat.Add("Time remaining", new DateTime(remains.Ticks).ToString(@"HH\:mm\:ss"));
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

                stat.Add("Average cost", Converter.DoubleToText(statistics.AverageCost, "N6"));
                stat.Add("Percent", Converter.DoubleToText(statistics.Percent, "N4") + " %");
                stat.Add("Learning rate", Converter.DoubleToText(learningRate));
                stat.Add("Rounds", Round.ToString());
                stat.Add("Rounds/sec", ((int)((double)Settings.SkipRoundsToDrawStatistics / DateTime.UtcNow.Subtract(speedTime).TotalSeconds)).ToString());

                stat.Add(string.Empty, string.Empty);
                stat.Add("Render time, mcs", string.Empty);
                stat.Add("Network", ((int)TimeSpan.FromTicks(RenderTime.Network).TotalMicroseconds()).ToString());
                stat.Add("Error matrix", ((int)TimeSpan.FromTicks(RenderTime.ErrorMatrix).TotalMicroseconds()).ToString());
                stat.Add("Plotter", ((int)TimeSpan.FromTicks(RenderTime.Plotter).TotalMicroseconds()).ToString());
                stat.Add("Statistics", ((int)TimeSpan.FromTicks(RenderTime.Statistics).TotalMicroseconds()).ToString());
                stat.Add("Data", ((int)TimeSpan.FromTicks(RenderTime.Data).TotalMicroseconds()).ToString());

                CtlStatisticsPresenter.Draw(stat);

                sw.Stop();
                RenderTime.Statistics = sw.Elapsed.Ticks;
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

            WorkThread.Priority = ThreadPriority.Lowest;
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
                InitialDirectory = System.IO.Path.GetFullPath("Networks\\"),
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
                CtlNetworkName.Content = System.IO.Path.GetFileNameWithoutExtension(Config.Main.GetString(Const.Param.NetworksManagerName));

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
            if (WorkThread != null)
            {
                WorkThread.Join();
                WorkThread = null;
            }
            if (TimeThread != null)
            {
                TimeThread.Join();
                TimeThread = null;
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
                    using (ApplyChangesLocker.GetLocker(Threads.Processor.GUI))
                    {
                        CtlInputDataPresenter.SetInputDataAndDraw(NetworksManager.Models.First());
                        CtlNetworkPresenter.RenderRunning(NetworksManager.SelectedNetworkModel);
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

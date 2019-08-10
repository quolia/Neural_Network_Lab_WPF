using Microsoft.Win32;
using Qualia.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tools;

namespace Qualia
{
    public partial class Main : Window
    {
        Thread WorkThread;
        CancellationToken CancellationToken;
        CancellationTokenSource CancellationTokenSource;
        public static object ApplyChangesLocker = new object();

        NetworksManager NetworksManager;

        DateTime StartTime;
        long Round;

        public Main()
        {
            InitializeComponent();
         
            Loaded += Main_Load;

            //SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            //SetStyle(ControlStyles.SupportsTransparentBackColor, false);
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
            CtlSettings.Changed -= OnSettingsChanged;
            CtlSettings.Changed += OnSettingsChanged;
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
                var manager = new NetworksManager(CtlInputDataPresenter, CtlTabs, name, OnNetworkUIChanged);
                Config.Main.Set(Const.Param.NetworksManagerName, name);
                ReplaceNetworksManagerControl(manager);
                if (manager.IsValid())
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
                Config.Main.Set(Const.Param.NetworksManagerName, "");
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

        private void OnNetworkUIChanged(Notification.ParameterChanged param, object newValue = null)
        {
            ToggleApplyChanges(Const.Toggle.On);
            CtlMenuStart.IsEnabled = false;

            if (param == Notification.ParameterChanged.NeuronsCount)
            {
                if (NetworksManager != null)
                {
                    NetworksManager.ResetLayersTabsNames();
                }
            }
        }

        private void ApplyChangesToRunningNetworks()
        {
            lock (ApplyChangesLocker)
            {
                CtlInputDataPresenter.RearrangeWithNewPointsCount();
                var newModels = NetworksManager.CreateNetworksDataModels();
                NetworksManager.MergeModels(newModels);
                CtlNetworkPresenter.RenderRunning(NetworksManager.SelectedNetworkModel);
                ToggleApplyChanges(Const.Toggle.Off);
            }
        }

        private void ApplyChangesToStandingNetworks()
        {
            lock (ApplyChangesLocker)
            {
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
                CtlInputDataPresenter.SetInputDataAndDraw(NetworksManager.Models.First());
                NetworksManager.FeedForward(); // initialize state

                Round = 0;
                StartTime = DateTime.Now;

                DrawModels(NetworksManager.Models);

                WorkThread = new Thread(new ThreadStart(RunNetwork));
                WorkThread.Priority = ThreadPriority.Highest;
                WorkThread.Start();
            }
        }

        private void RunNetwork()
        {
            DateTime prevTime = DateTime.Now;

            while (!CancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(0);

                lock (ApplyChangesLocker)
                {
                    NetworksManager.PrepareModelsForRound();

                    foreach (var model in NetworksManager.Models)
                    {
                        if (!model.IsEnabled)
                        {
                            continue;
                        }

                        model.FeedForward();

                        var output = model.GetMaxActivatedOutputNeuron();
                        var input = model.GetNumberOfFirstLayerActiveNeurons();
                        var cost = model.Cost(input);
                        if (input == output.Id)
                        {
                            ++model.Statistic.CorrectRounds;

                            model.Statistic.LastGoodInput = input;
                            model.Statistic.LastGoodOutput = output.Id;
                            model.Statistic.LastGoodOutputActivation = output.Activation;
                            model.Statistic.LastGoodCost = cost;
                        }
                        else
                        {
                            model.Statistic.LastBadInput = input;
                            model.Statistic.LastBadOutput = output.Id;
                            model.Statistic.LastBadOutputActivation = output.Activation;
                            model.Statistic.LastBadCost = cost;
                        }

                        model.ErrorMatrix.AddData(input, output.Id);

                        ++model.Statistic.Rounds;

                        model.BackPropagation(input);

                        if (model.Statistic.Rounds == 1)
                        {
                            model.Statistic.AverageCost = cost;
                        }
                        else
                        {
                            model.Statistic.AverageCost = (model.Statistic.AverageCost * (model.Statistic.Rounds - 1) + cost) / model.Statistic.Rounds;
                        }
                    }

                    ++Round;
                }

                if (NetworksManager.Models[0].ErrorMatrix.Count % Settings.SkipRoundsToDrawErrorMatrix == 0)
                {
                    using (var ev = new AutoResetEvent(false))
                    {
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            lock (ApplyChangesLocker)
                            {
                                CtlMatrixPresenter.Draw(NetworksManager.Models, NetworksManager.SelectedNetworkModel);
                                NetworksManager.ResetErrorMatrix();
                                ev.Set();
                            }
                        }));
                        ev.WaitOne();
                    };
                }

                if (Round % Settings.SkipRoundsToDrawNetworks == 0)// || DateTime.Now.Subtract(startTime).TotalSeconds >= 10)
                {
                    using (var ev = new AutoResetEvent(false))
                    {
                        Dispatcher.BeginInvoke((Action)(() =>
                        {      
                            lock (ApplyChangesLocker)
                            {
                                DrawModels(NetworksManager.Models);
                            }
                            ev.Set();
                        }));
                        ev.WaitOne();
                    };
                }

                if (Round % Settings.SkipRoundsToDrawStatistic == 0)
                {
                    using (var ev = new AutoResetEvent(false))
                    {
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            lock (ApplyChangesLocker)
                            {
                                DrawStatistic(NetworksManager.Models);
                            }
                            ev.Set();
                        }));
                        ev.WaitOne();
                    };
                }

                if ((long)DateTime.Now.Subtract(prevTime).TotalSeconds >= 1)
                {
                    prevTime = DateTime.Now;
                    Dispatcher.BeginInvoke((Action)(() => CtlTime.Content = "Time: " + DateTime.Now.Subtract(StartTime).ToString(@"hh\:mm\:ss")));
                }
            }
        }

        private void DrawModels(List<NetworkDataModel> models)
        {
            var renderStart = DateTime.Now;

            CtlNetworkPresenter.RenderRunning(NetworksManager.SelectedNetworkModel);
            CtlInputDataPresenter.SetInputDataAndDraw(NetworksManager.Models.First());
        }

        private void DrawStatistic(List<NetworkDataModel> models)
        {
            foreach (var model in models)
            {
                model.DynamicStatistic.Add(model.Statistic.Percent, model.Statistic.AverageCost);
            }

            CtlPlotPresenter.Draw(models, NetworksManager.SelectedNetworkModel);

            var selected = NetworksManager.SelectedNetworkModel;

            if (selected == null)
            {
                CtlStatisticsPresenter.Draw(null);
            }
            else
            {
                var stat = new Dictionary<string, string>();
                var span = DateTime.Now.Subtract(StartTime);
                stat.Add("Time", new DateTime(span.Ticks).ToString(@"HH\:mm\:ss"));

                if (selected.Statistic.Percent > 0)
                {
                    var remains = new DateTime((long)(span.Ticks * 100 / selected.Statistic.Percent) - span.Ticks);
                    stat.Add("Time remaining", new DateTime(remains.Ticks).ToString(@"HH\:mm\:ss"));
                }
                else
                {
                    stat.Add("Time remaining", "N/A");
                }

                if (selected.Statistic.LastGoodOutput > -1)
                {
                    stat.Add("Last good output", $"{selected.Statistic.LastGoodInput}={selected.Statistic.LastGoodOutput} ({Converter.DoubleToText(100 * selected.Statistic.LastGoodOutputActivation, "N6")}%)");
                    stat.Add("Last good cost", Converter.DoubleToText(selected.Statistic.LastGoodCost, "N6"));

                }
                else
                {
                    stat.Add("Last good output", "none");
                    stat.Add("Last good cost", "none");
                }

                if (selected.Statistic.LastBadOutput > -1)
                {
                    stat.Add("Last bad output", $"{selected.Statistic.LastBadInput}={selected.Statistic.LastBadOutput} ({Converter.DoubleToText(100 * selected.Statistic.LastBadOutputActivation, "N6")}%)");
                    stat.Add("Last bad cost", Converter.DoubleToText(selected.Statistic.LastBadCost, "N6"));
                }
                else
                {
                    stat.Add("Last bad output", "none");
                    stat.Add("Last bad cost", "none");
                }

                stat.Add("Average cost", Converter.DoubleToText(selected.Statistic.AverageCost, "N6"));
                stat.Add("Percent", Converter.DoubleToText(selected.Statistic.Percent, "N6") + " %");
                stat.Add("Learning rate", Converter.DoubleToText(selected.LearningRate));
                stat.Add("Rounds", Round.ToString());
                stat.Add("Rounds/sec", ((int)((double)Round / DateTime.Now.Subtract(StartTime).TotalSeconds)).ToString());

                var renderStop = DateTime.Now;

                //stat.Add("Render time, msec", ((int)(renderStop.Subtract(renderStart).TotalMilliseconds)).ToString());
                CtlStatisticsPresenter.Draw(stat);
                selected.LastStatistic = stat;
            }

            NetworksManager.ResetModelsStatistic();
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

            var network = new NetworksManager(CtlInputDataPresenter, CtlTabs, null, OnNetworkUIChanged);
            if (network.Config != null)
            {
                ReplaceNetworksManagerControl(network);
                if (network.IsValid())
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
            NetworksManager = manager;

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
                    ////////LoadConfig(); 
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
                        CtlInputDataPresenter.SetInputDataAndDraw(NetworksManager.Models.First());
                        CtlNetworkPresenter.RenderRunning(NetworksManager.SelectedNetworkModel);
                        CtlPlotPresenter.Draw(NetworksManager.Models, NetworksManager.SelectedNetworkModel);
                        CtlStatisticsPresenter.Draw(NetworksManager.SelectedNetworkModel.LastStatistic);
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
            OnNetworkUIChanged(Notification.ParameterChanged.Structure, null);
        }

        private void CtlMainMenuDeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            NetworksManager.SelectedNetwork.DeleteLayer();
            OnNetworkUIChanged(Notification.ParameterChanged.Structure, null);
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
    }
}

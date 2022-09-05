using Microsoft.Win32;
using Qualia.Model;
using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public class NetworksManager
    {
        public readonly Config Config;
        public ListX<NetworkDataModel> NetworkModels;

        private readonly TabControl _Tabs;
        private TaskFunction _taskFunction;
        private NetworkControl _selectedNetworkControl;

        public NetworksManager(TabControl tabs, string fileName, ActionManager.ApplyActionDelegate onChanged)
        {
            this.SetUIHandler(onChanged);

            _Tabs = tabs;
            _Tabs.SelectionChanged += NetworksTabs_OnSelected;

            Config = string.IsNullOrEmpty(fileName) ? CreateNewManager() : new(fileName);
            if (Config != null)
            {
                ClearNetworks();
                LoadConfig();
            }
        }

        private void NetworksTabs_OnSelected(object sender, SelectionChangedEventArgs e)
        {
            RefreshSelectedNetworkTab();
        }

        public void RefreshSelectedNetworkTab()
        {
            var selectedNetworkControl = _Tabs.SelectedContent as NetworkControl;
            if (selectedNetworkControl != null)
            {
                _selectedNetworkControl = selectedNetworkControl;
            }
            else
            {
                bool isStillExist = false;
                for (int i = 0; i < _Tabs.Items.Count; ++i)
                {
                    if ((_Tabs.Items.GetItemAt(i) as TabItem).Content == _selectedNetworkControl)
                    {
                        isStillExist = true;
                        break;
                    }
                }

                if (!isStillExist)
                {
                    _selectedNetworkControl = null;
                }
            }
        }

        public NetworkControl SelectedNetworkControl => _selectedNetworkControl;

        public NetworkDataModel SelectedNetworkModel
        {
            get
            {
                var selectedNetworkModel = SelectedNetworkControl == null
                                           ? null
                                           : NetworkModels.FirstOrDefault(model => model.VisualId == SelectedNetworkControl.VisualId);
                return selectedNetworkModel;
            }
        }

        private List<NetworkControl> NetworksControls
        {
            get
            {
                List<NetworkControl> result = new();
                for (int i = 1; i < _Tabs.Items.Count; ++i)
                {
                    result.Add(_Tabs.Tab(i).Content as NetworkControl);
                }

                return result;
            }
        }

        public static Config CreateNewManager()
        {
            var fileName = GetNewFileName();
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            Config config = new(fileName);
            Config.Main.Set(Constants.Param.NetworksManagerName, fileName);
            Config.Main.FlushToDrive();

            return config;
        }

        private static string GetNewFileName()
        {
            SaveFileDialog saveDialog = new()
            {
                InitialDirectory = App.WorkingDirectory + "Networks",
                DefaultExt = "txt",
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            };

            bool yes = false;

            try
            {
                yes = saveDialog.ShowDialog() == true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return yes ? saveDialog.FileName : null;
        }

        private void ClearNetworks()
        {
            while (_Tabs.Items.Count > 1)
            {
                _Tabs.Items.RemoveAt(1);
            }
        }

        private void LoadConfig()
        {
            var networkIds = Config.Get(Constants.Param.Networks, new long[] { Constants.UnknownId });

            Range.For(networkIds.Length, i => AddNetwork(networkIds[i]));
            _Tabs.SelectedIndex = Config.Get(Constants.Param.SelectedNetworkIndex, 0) + 1;

            RefreshNetworks(null);
        }

        public void RebuildNetworksForTask(TaskFunction task)
        {
            _taskFunction = task;
            ActionManager.Instance.Lock();
            NetworksControls.ForEach(n => n.NetworkTask_OnChanged(task));
            ActionManager.Instance.Unlock();

            this.InvokeUIHandler(new(this, Notification.ParameterChanged.NeuronsAdded));
        }

        public ApplyAction GetNetworksRefreshAction(object sender, bool isNeedConfirmation)
        {
            if (isNeedConfirmation)
            {
                return new(this)
                {
                    Apply = (isRunning) => RefreshNetworks(sender)
                };
            }
            else
            {
                return new(this)
                {
                    ApplyInstant = (isRunning) => RefreshNetworks(sender)
                };
            }
        }

        public void SetNetworkEnabled(object sender)
        {
            var network = GetParentNetworkControl(sender);
            var model = NetworkModels.Find(m => m.VisualId == network.VisualId);
            if (model == null)
            {
                return;
            }

            model.IsEnabled = network.CtlIsNetworkEnabled.Value;
        }

        public NetworkControl AddNetwork()
        {
            return AddNetwork(Constants.UnknownId);
        }

        private NetworkControl AddNetwork(long networkId)
        {
            NetworkControl network = new(networkId, Config, this.GetUIHandler());
            TabItem tabItem = new()
            {
                Header = $"Network {_Tabs.Items.Count}",
                Content = network
            };

            _Tabs.Items.Add(tabItem);
            _Tabs.SelectedItem = tabItem;

            if (networkId == Constants.UnknownId)
            {
                network.CtlIsNetworkEnabled.Value = false;
                network.NetworkTask_OnChanged(_taskFunction);

                /*
                ApplyAction action = new(this, Notification.ParameterChanged.NetworksCount)
                {
                    Apply = (isRunning) =>
                    {
                        RefreshNetworks(null);
                    }
                };

                this.InvokeUIHandler(action);
                */
            }

            return network;
        }

        public void RemoveNetwork()
        {
            if (MessageBoxResult.OK == MessageBox.Show($"Would you really like to remove Network {_Tabs.SelectedIndex}?",
                                                       "Confirm",
                                                       MessageBoxButton.OKCancel))
            {
                var selectedNetworkControl = SelectedNetworkControl;

                var selectedTab = _Tabs.SelectedTab();
                var index = _Tabs.SelectedIndex;
                _Tabs.SelectedIndex = index - 1;
                _Tabs.Items.Remove(selectedTab);

                if (_Tabs.SelectedIndex == 0 && _Tabs.Items.Count > 1)
                {
                    _Tabs.SelectedIndex = 1;
                }

                ResetNetworksTabsNames();

                ApplyAction action = new(this, Notification.ParameterChanged.NetworksCount)
                {
                    Apply = (isRunning) =>
                    {
                        selectedNetworkControl.RemoveFromConfig();
                        RefreshNetworks(null);
                    },
                    Cancel = (isRunning) =>
                    {
                        _Tabs.Items.Insert(index, selectedTab);
                        _Tabs.SelectedItem = selectedTab;
                        ResetNetworksTabsNames();
                    }
                };

                this.InvokeUIHandler(action);
            }
        }

        private void ResetNetworksTabsNames()
        {
            for (int i = 1; i < _Tabs.Items.Count; ++i)
            {
                _Tabs.Tab(i).Header = $"Network {i}";
            }
        }

        public bool IsValid()
        {
            return !NetworksControls.Any() || NetworksControls.All(n => n.IsValid());
        }

        public void SaveConfig()
        {
            Config.Set(Constants.Param.Networks, NetworksControls.Select(n => n.VisualId));
            Config.Set(Constants.Param.SelectedNetworkIndex, _Tabs.SelectedIndex - 1);

            NetworksControls.ForEach(n => n.SaveConfig());

            Config.FlushToDrive();
        }

        public void ResetLayersTabsNames()
        {
            NetworksControls.ForEach(n => n.ResetLayersTabsNames());
        }

        public static void SaveAs()
        {
            var fileName = GetNewFileName();
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            File.Copy(Config.Main.Get(Constants.Param.NetworksManagerName, ""), fileName);
        }

        public ListX<NetworkDataModel> CreateNetworksDataModels(object control)
        {
            var model = CreateNetworkDataModel(control);
            if (model == null)
            {
                ListX<NetworkDataModel> newkModels = new(NetworksControls.Count);
                NetworksControls.ForEach(network => newkModels.Add(network.CreateNetworkDataModel(_taskFunction, false)));
                MergeModels(newkModels);

                return newkModels;
            }

            MergeModel(model);

            return NetworkModels;
        }

        public NetworkDataModel CreateNetworkDataModel(object control)
        {
            var network = GetParentNetworkControl(control);
            if (network == null)
            {
                return null;
            }

            NetworkDataModel networkModel = network.CreateNetworkDataModel(_taskFunction, false);
            return networkModel;
        }

        public NetworkControl GetParentNetworkControl(object control)
        {
            if (control == null)
            {
                return null;
            }

            var fe = control as FrameworkElement;
            if (fe == null)
            {
                return null;
            }

            var network = control as NetworkControl;
            if (network == null)
            {
                network = fe.GetParentOfType<NetworkControl>();
            }

            return network;
        }

        public void RefreshNetworks(object control)
        {
            NetworkModels = CreateNetworksDataModels(control);
        }

        public void MergeModels(ListX<NetworkDataModel> networkModels)
        {
            if (NetworkModels == null)
            {
                NetworkModels = networkModels;
                return;
            }

            ListX<NetworkDataModel> newModels = new(networkModels.Count);
            var newNetworkModel = networkModels.First;

            while (newNetworkModel != null)
            {
                var networkModel = NetworkModels.Find(model => model.VisualId == newNetworkModel.VisualId);
                if (networkModel != null)
                {
                    newModels.Add(networkModel.Merge(newNetworkModel));
                }
                else
                {
                    newModels.Add(newNetworkModel);
                }

                newNetworkModel = newNetworkModel.Next;
            }

            NetworkModels = newModels;
        }

        public void MergeModel(NetworkDataModel newNetwork)
        {
            var network = NetworkModels.Find(n => n.VisualId == newNetwork.VisualId);
            if (network == null)
            {
                throw new InvalidOperationException();
            }

            var ind = NetworkModels.IndexOf(network);
            newNetwork = network.Merge(newNetwork);
            NetworkModels.Replace(ind, newNetwork);
        }

        unsafe public void PrepareModelsForRun()
        {
            NetworkModels.ForEach(PrepareModelForRun);
        }

        unsafe public void PrepareModelForRun(NetworkDataModel network)
        {
            network.ActivateFirstLayer();
            network.BackPropagationStrategy.PrepareForRun(network);
            network.PlotterStatistics = new();
            network.Statistics = new();
            network.ErrorMatrix.ClearData();
            network.ErrorMatrix.Next.ClearData();
        }

        unsafe public void PrepareModelsForRound()
        {
            var baseNetwork = NetworkModels.First;
            _taskFunction.Do(baseNetwork, _taskFunction.DistributionFunction, _taskFunction.DistributionFunctionParam);

            // copy first layer state and last layer targets to other networks

            var network = baseNetwork.Next;
            while (network != null)
            {
                if (!network.IsEnabled)
                {
                    network = network.Next;
                    continue;
                }

                network.BackPropagationStrategy.PrepareForRound(network);

                var baseNeuron = baseNetwork.Layers.First.Neurons.First;
                var neuron = network.Layers.First.Neurons.First;

                while (neuron != null)
                {
                    neuron.X = baseNeuron.X;
                    neuron.Activation = baseNeuron.Activation;

                    neuron = neuron.Next;
                    baseNeuron = baseNeuron.Next;
                }

                baseNeuron = baseNetwork.Layers.Last.Neurons.First;
                neuron = network.Layers.Last.Neurons.First;

                while (neuron != null)
                {
                    neuron.Target = baseNeuron.Target;

                    neuron = neuron.Next;
                    baseNeuron = baseNeuron.Next;
                }

                network.TargetOutputNeuronId = baseNetwork.TargetOutputNeuronId;

                network = network.Next;
            }
        }

        unsafe public void PrepareModelsForLoop()
        {
            NetworkModels.ForEach(network =>
            {
                if (network.Statistics == null)
                {
                    PrepareModelForRun(network);
                }

                network.BackPropagationStrategy.PrepareForLoop(network);
            });
        }

        public void FeedForward()
        {
            NetworkModels.ForEach(network => network.FeedForward());
        }
    }
}

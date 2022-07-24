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

        private readonly Action<Notification.ParameterChanged> _onChanged;

        private readonly TabControl _Tabs;
        private TaskFunction _taskFunction;
        private NetworkControl _selectedNetworkControl;
        private NetworkDataModel _prevSelectedNetworkModel;

        public NetworksManager(TabControl tabs, string fileName, Action<Notification.ParameterChanged> onChanged)
        {
            _onChanged = onChanged;

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
            _selectedNetworkControl = _Tabs.SelectedContent as NetworkControl;
        }

        public NetworkControl SelectedNetworkControl => _selectedNetworkControl;

        public NetworkDataModel SelectedNetworkModel
        {
            get
            {
                var selectedNetworkModel = SelectedNetworkControl == null
                                           ? _prevSelectedNetworkModel
                                           : NetworkModels.FirstOrDefault(model => model.VisualId == SelectedNetworkControl.Id);

                _prevSelectedNetworkModel = selectedNetworkModel;

                return selectedNetworkModel;
            }
        }

        private List<NetworkControl> NetworksControls
        {
            get
            {
                List<NetworkControl> networks = new();
                for (int i = 1; i < _Tabs.Items.Count; ++i)
                {
                    networks.Add(_Tabs.Tab(i).Content as NetworkControl);
                }

                return networks;
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

            RefreshNetworksDataModels();
        }

        public void RebuildNetworksForTask(TaskFunction task)
        {
            _taskFunction = task;
            NetworksControls.ForEach(n => n.NetworkTask_OnChanged(task));

            _onChanged(Notification.ParameterChanged.NeuronsCount);
        }

        public void AddNetwork()
        {
            AddNetwork(Constants.UnknownId);
        }

        private void AddNetwork(long networkId)
        {
            NetworkControl network = new(networkId, Config, _onChanged);
            TabItem tabItem = new()
            {
                Header = $"Network {_Tabs.Items.Count}",
                Content = network
            };

            _Tabs.Items.Add(tabItem);
            _Tabs.SelectedItem = tabItem;

            if (networkId == Constants.UnknownId)
            {
                network.InputLayer.NetworkTask_OnChanged(_taskFunction);
                network.ResetLayersTabsNames();
            }
        }

        public void RemoveNetwork()
        {
            if (MessageBoxResult.OK == MessageBox.Show($"Would you really like to remove Network {_Tabs.SelectedIndex}?",
                                                       "Confirm",
                                                       MessageBoxButton.OKCancel))
            {
                SelectedNetworkControl.RemoveFromConfig();

                var index = _Tabs.Items.IndexOf(_Tabs.SelectedTab());
                _Tabs.Items.Remove(_Tabs.SelectedTab());
                _Tabs.SelectedIndex = index - 1;

                ResetNetworksTabsNames();

                _onChanged(Notification.ParameterChanged.Structure);
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
            Config.Set(Constants.Param.Networks, NetworksControls.Select(n => n.Id));
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

        public ListX<NetworkDataModel> CreateNetworksDataModels()
        {
            ListX<NetworkDataModel> networkModels = new(NetworksControls.Count);
            NetworksControls.ForEach(network => networkModels.Add(network.CreateNetworkDataModel(_taskFunction, false)));

            return networkModels;
        }

        public void RefreshNetworksDataModels()
        {
            NetworkModels = CreateNetworksDataModels();
        }

        public void MergeModels(ListX<NetworkDataModel> networkModels)
        {
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

        unsafe public void PrepareModelsForRun()
        {
            NetworkModels.ForEach(network => network.ActivateFirstLayer());
            NetworkModels.ForEach(network => network.BackPropagationStrategy.PrepareForRun(network));

            ResetModelsPlotterStatistics();
            ResetModelsStatistics();
            ResetErrorMatrix();
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
            NetworkModels.ForEach(network => network.BackPropagationStrategy.PrepareForLoop(network));
        }

        public void FeedForward()
        {
            NetworkModels.ForEach(network => network.FeedForward());
        }

        public void ResetModelsStatistics()
        {
            NetworkModels.ForEach(network => network.Statistics = new());
        }

        private void ResetModelsPlotterStatistics()
        {
            NetworkModels.ForEach(network => network.PlotterStatistics = new());
        }

        public void ResetErrorMatrix()
        {
            NetworkModels.ForEach(network =>
            {
                network.ErrorMatrix.ClearData();
                network.ErrorMatrix.Next.ClearData();
            });
        }
    }
}

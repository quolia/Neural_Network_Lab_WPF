using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Tools;

namespace Qualia.Controls
{
    sealed public class NetworksManager
    {
        public readonly Config Config;
        public ListX<NetworkDataModel> NetworkModels;

        private readonly Action<Notification.ParameterChanged> OnNetworkUIChanged;

        private readonly TabControl _ctlTabs;
        private INetworkTask _networkTask;
        private NetworkControl _selectedNetworkControl;
        private NetworkDataModel _prevSelectedNetworkModel;

        public NetworksManager(TabControl tabs, string fileName, Action<Notification.ParameterChanged> onNetworkUIChanged)
        {
            OnNetworkUIChanged = onNetworkUIChanged;
            _ctlTabs = tabs;
            _ctlTabs.SelectionChanged += CtlTabs_SelectionChanged;

            Config = string.IsNullOrEmpty(fileName) ? CreateNewManager() : new Config(fileName);
            if (Config != null)
            {
                ClearNetworks();
                LoadConfig();
            }
        }

        private void CtlTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedNetworkControl = _ctlTabs.SelectedContent as NetworkControl;
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

        private List<NetworkControl> Networks
        {
            get
            {
                var ctlNetworks = new List<NetworkControl>();
                for (int i = 1; i < _ctlTabs.Items.Count; ++i)
                {
                    ctlNetworks.Add(_ctlTabs.Tab(i).Content as NetworkControl);
                }

                return ctlNetworks;
            }
        }

        public Config CreateNewManager()
        {
            var saveDialog = new SaveFileDialog
            {
                InitialDirectory = Path.GetFullPath("Networks\\"),
                DefaultExt = "txt",
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            };

            if (saveDialog.ShowDialog() == true)
            {
                var fileName = saveDialog.FileName;

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                var config = new Config(fileName);
                Config.Main.Set(Const.Param.NetworksManagerName, fileName);
                Config.Main.FlushToDrive();

                return config;
            }

            return null;
        }

        private void ClearNetworks()
        {
            while (_ctlTabs.Items.Count > 1)
            {
                _ctlTabs.Items.RemoveAt(1);
            }
        }

        private void LoadConfig()
        {
            var networkIds = Config.GetArray(Const.Param.Networks);
            if (networkIds.Length == 0)
            {
                networkIds = new long[] { Const.UnknownId };
            }

            Range.For(networkIds.Length, i => AddNetwork(networkIds[i]));
            _ctlTabs.SelectedIndex = (int)Config.GetInt(Const.Param.SelectedNetworkIndex, 0).Value + 1;

            RefreshNetworksDataModels();
        }

        public void RebuildNetworksForTask(INetworkTask task)
        {
            _networkTask = task;
            Networks.ForEach(ctlNetwork => ctlNetwork.OnTaskChanged(task));

            OnNetworkUIChanged(Notification.ParameterChanged.NeuronsCount);
        }

        public void AddNetwork()
        {
            AddNetwork(Const.UnknownId);
        }

        private void AddNetwork(long networkId)
        {
            var ctlNetwork = new NetworkControl(networkId, Config, OnNetworkUIChanged);
            var tabItem = new TabItem
            {
                Header = $"Network {_ctlTabs.Items.Count}",
                Content = ctlNetwork
            };

            _ctlTabs.Items.Add(tabItem);
            _ctlTabs.SelectedItem = tabItem;

            if (networkId == Const.UnknownId)
            {
                ctlNetwork.InputLayer.OnTaskChanged(_networkTask);
                ctlNetwork.ResetLayersTabsNames();
            }
        }

        public void DeleteNetwork()
        {
            if (MessageBox.Show($"Would you really like to delete Network {_ctlTabs.SelectedIndex}?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                SelectedNetworkControl.VanishConfig();

                var index = _ctlTabs.Items.IndexOf(_ctlTabs.SelectedTab());
                _ctlTabs.Items.Remove(_ctlTabs.SelectedTab());
                _ctlTabs.SelectedIndex = index - 1;

                ResetNetworksTabsNames();

                OnNetworkUIChanged(Notification.ParameterChanged.Structure);
            }
        }

        private void ResetNetworksTabsNames()
        {
            for (int i = 1; i < _ctlTabs.Items.Count; ++i)
            {
                _ctlTabs.Tab(i).Header = $"Network {i}";
            }
        }

        public bool IsValid()
        {
            return !Networks.Any() || Networks.All(ctlNetwork => ctlNetwork.IsValid());
        }

        public void SaveConfig()
        {
            Config.Set(Const.Param.Networks, Networks.Select(ctlNetwork => ctlNetwork.Id));
            Config.Set(Const.Param.SelectedNetworkIndex, _ctlTabs.SelectedIndex - 1);

            Networks.ForEach(ctlNetwork => ctlNetwork.SaveConfig());
        }

        public void ResetLayersTabsNames()
        {
            Networks.ForEach(ctlNetwork => ctlNetwork.ResetLayersTabsNames());
        }

        public void SaveAs()
        {
            var saveDialog = new SaveFileDialog
            {
                InitialDirectory = Path.GetFullPath("Networks\\"),
                DefaultExt = "txt",
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            };

            if (saveDialog.ShowDialog() == true)
            {
                if (File.Exists(saveDialog.FileName))
                {
                    File.Delete(saveDialog.FileName);
                }

                File.Copy(Config.Main.GetString(Const.Param.NetworksManagerName), saveDialog.FileName);
            }
        }

        public ListX<NetworkDataModel> CreateNetworksDataModels()
        {
            var networkModels = new ListX<NetworkDataModel>(Networks.Count);
            Networks.ForEach(ctlNetwork => networkModels.Add(ctlNetwork.CreateNetworkDataModel(_networkTask, false)));

            return networkModels;
        }

        public void RefreshNetworksDataModels()
        {
            NetworkModels = CreateNetworksDataModels();
        }

        public void MergeModels(ListX<NetworkDataModel> networkModels)
        {
            var newModels = new ListX<NetworkDataModel>(networkModels.Count);
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

        public void PrepareModelsForRun()
        {
            NetworkModels.ForEach(model => model.ActivateFirstLayer());
            ResetModelsDynamicStatistics();
            ResetModelsStatistics();
            ResetErrorMatrix();
        }

        public void PrepareModelsForRound()
        {
            var baseNetwork = NetworkModels.First;
            _networkTask.Do(baseNetwork);

            // copy first layer state and last layer targets to other networks

            var network = baseNetwork.Next;
            while (network != null)
            {
                if (!network.IsEnabled)
                {
                    network = network.Next;
                    continue;
                }

                var baseNeuron = baseNetwork.Layers.First.Neurons.First;
                var neuron = network.Layers.First.Neurons.First;

                while (neuron != null)
                {
                    if (!neuron.IsBias)
                    {
                        neuron.Activation = baseNeuron.Activation;
                    }

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

                network.TargetOutput = baseNetwork.TargetOutput;

                network = network.Next;
            }
        }

        public void FeedForward()
        {
            NetworkModels.ForEach(model => model.FeedForward());
        }

        public void ResetModelsStatistics()
        {
            NetworkModels.ForEach(model => model.Statistics = new Statistics());
        }

        private void ResetModelsDynamicStatistics()
        {
            NetworkModels.ForEach(model => model.DynamicStatistics = new DynamicStatistics());
        }

        public void ResetErrorMatrix()
        {
            NetworkModels.ForEach(m =>
            {
                m.ErrorMatrix.ClearData();
                m.ErrorMatrix.Next.ClearData();
            });
        }
    }
}

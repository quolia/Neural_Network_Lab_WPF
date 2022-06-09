﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Tools;

namespace Qualia.Controls
{
    public class NetworksManager
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
                for (int ind = 1; ind < _ctlTabs.Items.Count; ++ind)
                {
                    ctlNetworks.Add(_ctlTabs.Tab(ind).Content as NetworkControl);
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

            Range.For(networkIds.Length, ind => AddNetwork(networkIds[ind]));
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

                var ind = _ctlTabs.Items.IndexOf(_ctlTabs.SelectedTab());
                _ctlTabs.Items.Remove(_ctlTabs.SelectedTab());
                _ctlTabs.SelectedIndex = ind - 1;

                ResetNetworksTabsNames();

                OnNetworkUIChanged(Notification.ParameterChanged.Structure);
            }
        }

        private void ResetNetworksTabsNames()
        {
            for (int ind = 1; ind < _ctlTabs.Items.Count; ++ind)
            {
                _ctlTabs.Tab(ind).Header = $"Network {ind}";
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
            var newNetworkModel = networkModels[0];
            //foreach (var newNetworkModel in networkModels)
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
            var baseNetwork = NetworkModels[0];
            _networkTask.Do(baseNetwork);

            // copy first layer state and last layer targets to other networks

            var network = NetworkModels.Count > 1 ? NetworkModels[1] : null;          
            while (network != null)
            {
                if (!network.IsEnabled)
                {
                    network = network.Next;
                    continue;
                }

                var baseNeuron = baseNetwork.Layers[0].Neurons[0];
                var neuron = network.Layers[0].Neurons[0];

                while (neuron != null)
                {
                    if (!neuron.IsBias)
                    {
                        neuron.Activation = baseNeuron.Activation;
                    }

                    neuron = neuron.Next;
                    baseNeuron = baseNeuron.Next;
                }

                baseNeuron = baseNetwork.Layers.Last.Neurons[0];
                neuron = network.Layers.Last.Neurons[0];

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

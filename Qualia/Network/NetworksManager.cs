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

        private readonly Action<Notification.ParameterChanged> OnNetworkUIChanged;

        private readonly TabControl _ctlTabs;
        private TaskFunction _taskFunction;
        private NetworkControl _selectedNetworkControl;
        private NetworkDataModel _prevSelectedNetworkModel;

        public NetworksManager(TabControl tabs, string fileName, Action<Notification.ParameterChanged> onNetworkUIChanged)
        {
            OnNetworkUIChanged = onNetworkUIChanged;

            _ctlTabs = tabs;
            _ctlTabs.SelectionChanged += CtlTabs_SelectionChanged;

            Config = string.IsNullOrEmpty(fileName) ? CreateNewManager() : new(fileName);
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
                List<NetworkControl> ctlNetworks = new();
                for (int i = 1; i < _ctlTabs.Items.Count; ++i)
                {
                    ctlNetworks.Add(_ctlTabs.Tab(i).Content as NetworkControl);
                }

                return ctlNetworks;
            }
        }

        public static Config CreateNewManager()
        {
            SaveFileDialog saveDialog = new()
            {
                InitialDirectory = App.WorkingDirectory + "Networks",
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

                Config config = new(fileName);
                Config.Main.Set(Constants.Param.NetworksManagerName, fileName);
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
            var networkIds = Config.Get(Constants.Param.Networks, Array.Empty<long>());
            if (networkIds.Length == 0)
            {
                networkIds = new long[] { Constants.UnknownId };
            }

            Range.For(networkIds.Length, i => AddNetwork(networkIds[i]));
            _ctlTabs.SelectedIndex = (int)Config.Get(Constants.Param.SelectedNetworkIndex, 0) + 1;

            RefreshNetworksDataModels();
        }

        public void RebuildNetworksForTask(TaskFunction task)
        {
            _taskFunction = task;
            Networks.ForEach(ctlNetwork => ctlNetwork.OnTaskChanged(task));

            OnNetworkUIChanged(Notification.ParameterChanged.NeuronsCount);
        }

        public void AddNetwork()
        {
            AddNetwork(Constants.UnknownId);
        }

        private void AddNetwork(long networkId)
        {
            NetworkControl ctlNetwork = new(networkId, Config, OnNetworkUIChanged);
            TabItem tabItem = new()
            {
                Header = $"Network {_ctlTabs.Items.Count}",
                Content = ctlNetwork
            };

            _ctlTabs.Items.Add(tabItem);
            _ctlTabs.SelectedItem = tabItem;

            if (networkId == Constants.UnknownId)
            {
                ctlNetwork.InputLayer.OnTaskChanged(_taskFunction);
                ctlNetwork.ResetLayersTabsNames();
            }
        }

        public void DeleteNetwork()
        {
            if (MessageBoxResult.OK == MessageBox.Show($"Would you really like to delete Network {_ctlTabs.SelectedIndex}?",
                                                       "Confirm",
                                                       MessageBoxButton.OKCancel))
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
            Config.Set(Constants.Param.Networks, Networks.Select(ctlNetwork => ctlNetwork.Id));
            Config.Set(Constants.Param.SelectedNetworkIndex, _ctlTabs.SelectedIndex - 1);

            Networks.ForEach(ctlNetwork => ctlNetwork.SaveConfig());
        }

        public void ResetLayersTabsNames()
        {
            Networks.ForEach(ctlNetwork => ctlNetwork.ResetLayersTabsNames());
        }

        public static void SaveAs()
        {
            SaveFileDialog saveDialog = new()
            {
                InitialDirectory = App.WorkingDirectory + "Networks",
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

                File.Copy(Config.Main.Get(Constants.Param.NetworksManagerName, ""), saveDialog.FileName);
            }
        }

        public ListX<NetworkDataModel> CreateNetworksDataModels()
        {
            ListX<NetworkDataModel> networkModels = new(Networks.Count);
            Networks.ForEach(ctlNetwork => networkModels.Add(ctlNetwork.CreateNetworkDataModel(_taskFunction, false)));

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
            NetworkModels.ForEach(model => model.ActivateFirstLayer());
            NetworkModels.ForEach(model => model.BackPropagationStrategy.PrepareForRun(model));

            ResetModelsDynamicStatistics();
            ResetModelsStatistics();
            ResetErrorMatrix();
        }

        unsafe public void PrepareModelsForRound()
        {
            var baseNetwork = NetworkModels.First;
            _taskFunction.Do(baseNetwork, _taskFunction.InputDataFunction, _taskFunction.InputDataFunctionParam);

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

        unsafe public void PrepareModelsForLoop()
        {
            NetworkModels.ForEach(model => model.BackPropagationStrategy.PrepareForLoop(model));
        }
        public void FeedForward()
        {
            NetworkModels.ForEach(model => model.FeedForward());
        }

        public void ResetModelsStatistics()
        {
            NetworkModels.ForEach(model => model.Statistics = new());
        }

        private void ResetModelsDynamicStatistics()
        {
            NetworkModels.ForEach(model => model.DynamicStatistics = new());
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

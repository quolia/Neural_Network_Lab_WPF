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
    public class NetworksManager
    {
        public readonly Config Config;
        public ListX<NetworkDataModel> NetworkModels;

        private readonly Action<Notification.ParameterChanged> OnNetworkUIChanged;

        private readonly TabControl _ctlTabs;
        private INetworkTask _task;
        private NetworkDataModel _prevSelectedNetworkModel;

        public NetworksManager(TabControl tabs, string name, Action<Notification.ParameterChanged> onNetworkUIChanged)
        {
            OnNetworkUIChanged = onNetworkUIChanged;
            _ctlTabs = tabs;

            Config = string.IsNullOrEmpty(name) ? CreateNewManager() : new Config(name);
            if (Config != null)
            {
                ClearNetworks();
                LoadConfig();
            }
        }

        public NetworkControl SelectedNetwork => _ctlTabs.SelectedContent as NetworkControl;

        public NetworkDataModel SelectedNetworkModel
        {
            get
            {
                var selected = SelectedNetwork == null
                               ? _prevSelectedNetworkModel
                               : NetworkModels.FirstOrDefault(m => m.VisualId == SelectedNetwork.Id);

                _prevSelectedNetworkModel = selected;

                return selected;
            }
        }

        private List<NetworkControl> Networks
        {
            get
            {
                var result = new List<NetworkControl>();
                for (int i = 1; i < _ctlTabs.Items.Count; ++i)
                {
                    result.Add(_ctlTabs.Tab(i).Content as NetworkControl);
                }

                return result;
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
            {
                if (saveDialog.ShowDialog() == true)
                {
                    if (File.Exists(saveDialog.FileName))
                    {
                        File.Delete(saveDialog.FileName);
                    }

                    var config = new Config(saveDialog.FileName);
                    Config.Main.Set(Const.Param.NetworksManagerName, saveDialog.FileName);

                    return config;
                }
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
            var networks = Config.GetArray(Const.Param.Networks);
            if (networks.Length == 0)
            {
                networks = new long[] { Const.UnknownId };
            }

            Range.For(networks.Length, i => AddNetwork(networks[i]));
            _ctlTabs.SelectedIndex = (int)Config.GetInt(Const.Param.SelectedNetworkIndex, 0).Value + 1;

            RefreshNetworksDataModels();
        }

        public void RebuildNetworksForTask(INetworkTask task)
        {
            _task = task;
            Networks.ForEach(n => n.OnTaskChanged(task));

            OnNetworkUIChanged(Notification.ParameterChanged.NeuronsCount);
        }

        public void AddNetwork()
        {
            AddNetwork(Const.UnknownId);
        }

        private void AddNetwork(long id)
        {
            var network = new NetworkControl(id, Config, OnNetworkUIChanged);
            var tab = new TabItem
            {
                Header = $"Network {_ctlTabs.Items.Count}",
                Content = network
            };

            _ctlTabs.Items.Add(tab);
            _ctlTabs.SelectedItem = tab;

            if (id == Const.UnknownId)
            {
                network.InputLayer.OnTaskChanged(_task);
                network.ResetLayersTabsNames();
            }
        }

        public void DeleteNetwork()
        {
            if (MessageBox.Show($"Would you really like to delete Network {_ctlTabs.SelectedIndex}?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                SelectedNetwork.VanishConfig();

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
            return !Networks.Any() || Networks.All(n => n.IsValid());
        }

        public void SaveConfig()
        {
            Config.Set(Const.Param.Networks, Networks.Select(l => l.Id));
            Config.Set(Const.Param.SelectedNetworkIndex, _ctlTabs.SelectedIndex - 1);

            Networks.ForEach(n => n.SaveConfig());
        }

        public void ResetLayersTabsNames()
        {
            Networks.ForEach(n => n.ResetLayersTabsNames());
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
            {
                if (saveDialog.ShowDialog() == true)
                {
                    if (File.Exists(saveDialog.FileName))
                    {
                        File.Delete(saveDialog.FileName);
                    }

                    File.Copy(Config.Main.GetString(Const.Param.NetworksManagerName), saveDialog.FileName);
                }
            }
        }

        public ListX<NetworkDataModel> CreateNetworksDataModels()
        {
            var result = new ListX<NetworkDataModel>(Networks.Count);
            Networks.ForEach(n => result.Add(n.CreateNetworkDataModel(_task, false)));

            return result;
        }

        public void RefreshNetworksDataModels()
        {
            NetworkModels = CreateNetworksDataModels();
        }

        public void MergeModels(ListX<NetworkDataModel> models)
        {
            var newModels = new ListX<NetworkDataModel>(models.Count);
            foreach (var newModel in models)
            {
                var model = NetworkModels.Find(m => m.VisualId == newModel.VisualId);
                if (model != null)
                {
                    newModels.Add(model.Merge(newModel));
                }
                else
                {
                    newModels.Add(newModel);
                }
            }

            NetworkModels = newModels;
        }

        public void PrepareModelsForRun()
        {
            NetworkModels.ForEach(m => m.ActivateFirstLayer());
            ResetModelsDynamicStatistics();
            ResetModelsStatistics();
            ResetErrorMatrix();
        }

        public void PrepareModelsForRound()
        {
            _task.Do(NetworkModels[0]);

            // copy first layer state and last layer targets to other networks

            var networkModel = NetworkModels.Count > 1 ? NetworkModels[1] : null;          
            while (networkModel != null)
            {
                if (!networkModel.IsEnabled)
                {
                    networkModel = networkModel.Next;
                    continue;
                }

                var neuronFirstModelFirstLayer = NetworkModels[0].Layers[0].Neurons[0];
                var neuron = networkModel.Layers[0].Neurons[0];

                while (neuron != null)
                {
                    if (!neuron.IsBias)
                    {
                        neuron.Activation = neuronFirstModelFirstLayer.Activation;
                    }

                    neuron = neuron.Next;
                    neuronFirstModelFirstLayer = neuronFirstModelFirstLayer.Next;
                }

                var neuronFirstModelLastLayer = NetworkModels[0].Layers.Last().Neurons[0];
                neuron = networkModel.Layers.Last().Neurons[0];

                while (neuron != null)
                {
                    neuron.Target = neuronFirstModelLastLayer.Target;

                    neuron = neuron.Next;
                    neuronFirstModelLastLayer = neuronFirstModelLastLayer.Next;
                }

                networkModel.TargetOutput = NetworkModels[0].TargetOutput;

                networkModel = networkModel.Next;
            }
        }

        public void FeedForward()
        {
            NetworkModels.ForEach(m => m.FeedForward());
        }

        public void ResetModelsStatistics()
        {
            NetworkModels.ForEach(m => m.Statistics = new Statistics());
        }

        private void ResetModelsDynamicStatistics()
        {
            NetworkModels.ForEach(m => m.DynamicStatistics = new DynamicStatistics());
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

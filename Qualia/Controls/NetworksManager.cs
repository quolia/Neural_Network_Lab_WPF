using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public class NetworksManager
    {
        public readonly Config Config;
        public List<NetworkDataModel> Models;

        Action<Notification.ParameterChanged, object> OnNetworkUIChanged;

        readonly TabControl CtlTabs;
        readonly DataPresenter CtlInputData;

        public NetworksManager(DataPresenter inputData, TabControl tabs, string name, Action<Notification.ParameterChanged, object> onNetworkUIChanged)
        {
            OnNetworkUIChanged = onNetworkUIChanged;
            CtlInputData = inputData;
            CtlTabs = tabs;

            Config = String.IsNullOrEmpty(name) ? CreateNewManager() : new Config(name);
            if (Config != null)
            {
                ClearNetworks();
                LoadConfig();
            }
        }

        //public int InputNeuronsCount => Models.First().Layers[0].Neurons.Where(c => !c.IsBias).Count();

        NetworkDataModel _prevSelectedNetworkModel;

        public NetworkControl SelectedNetwork => (CtlTabs.SelectedContent as Panel).Children[0] as NetworkControl;
        public NetworkDataModel SelectedNetworkModel
        {
            get
            {
                var selected = SelectedNetwork == null ? _prevSelectedNetworkModel : Models.FirstOrDefault(m => m.VisualId == SelectedNetwork.Id);
                _prevSelectedNetworkModel = selected;
                return selected;
            }
        }

        List<NetworkControl> Networks
        {
            get
            {
                var result = new List<NetworkControl>();
                for (int i = 1; i < CtlTabs.TabCount; ++i)
                {
                    result.Add(CtlTabs.TabPages[i].Controls[0] as NetworkControl);
                }
                return result;
            }
        }

        public Config CreateNewManager()
        {
            using (var saveDialog = new SaveFileDialog
            {
                InitialDirectory = Path.GetFullPath("Networks\\"),
                DefaultExt = "txt",
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    if (File.Exists(saveDialog.FileName))
                    {
                        File.Delete(saveDialog.FileName);
                    }

                    var name = Path.GetFileNameWithoutExtension(saveDialog.FileName);
                    var config = new Config(saveDialog.FileName);
                    Config.Main.Set(Const.Param.NetworksManagerName, saveDialog.FileName);
                    return config;
                }
            }

            return null;
        }

        private void ClearNetworks()
        {
            while (CtlTabs.TabCount > 1)
            {
                CtlTabs.TabPages.RemoveAt(1);
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
            CtlTabs.SelectedIndex = Config.GetInt(Const.Param.SelectedNetworkIndex, 0).Value + 1;
            CtlInputData.LoadConfig(Config, OnInputDataChanged);
            OnInputDataChanged(CtlInputData.InputCount);
            RefreshNetworksDataModels();
        }

        private void OnInputDataChanged(int newCount)
        {
            foreach (var network in Networks)
            {
                network.InputLayer.OnInputDataChanged(newCount);
            }
            ResetLayersTabsNames();
            OnNetworkUIChanged(Notification.ParameterChanged.NeuronsCount, null);
        }

        public void AddNetwork()
        {
            AddNetwork(Const.UnknownId);
        }

        private void AddNetwork(long id)
        {
            var network = new NetworkControl(id, Config, OnNetworkUIChanged);
            var tab = new TabPage($"Network {CtlTabs.TabCount}");
            tab.Controls.Add(network);
            CtlTabs.TabPages.Add(tab);
            CtlTabs.SelectedIndex = CtlTabs.TabPages.IndexOf(tab);
        }

        public void DeleteNetwork()
        {
            if (MessageBox.Show($"Would you really like to delete Network {CtlTabs.SelectedIndex}?", "Confirm", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                SelectedNetwork.VanishConfig();
                var index = CtlTabs.TabPages.IndexOf(CtlTabs.SelectedTab);
                CtlTabs.TabPages.Remove(CtlTabs.SelectedTab);
                CtlTabs.SelectedIndex = index - 1;
                ResetNetworksTabsNames();
                OnNetworkUIChanged(Notification.ParameterChanged.Structure, null);
            }
        }

        private void ResetNetworksTabsNames()
        {
            for (int i = 1; i < CtlTabs.TabCount; ++i)
            {
                CtlTabs.TabPages[i].Text = $"Network {i}";
            }
        }

        public bool IsValid()
        {
            return !Networks.Any() || Networks.All(n => n.IsValid());
        }

        public void SaveConfig()
        {
            CtlInputData.SaveConfig(Config);

            Config.Set(Const.Param.Networks, Networks.Select(l => l.Id));
            Config.Set(Const.Param.SelectedNetworkIndex, CtlTabs.SelectedIndex - 1);
            Range.ForEach(Networks, n => n.SaveConfig());
        }

        public void ResetLayersTabsNames()
        {
            Range.ForEach(Networks, n => n.ResetLayersTabsNames());
        }

        public void SaveAs()
        {
            using (var saveDialog = new SaveFileDialog
            {
                InitialDirectory = Path.GetFullPath("Networks\\"),
                DefaultExt = "txt",
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    if (File.Exists(saveDialog.FileName))
                    {
                        File.Delete(saveDialog.FileName);
                    }

                    File.Copy(Config.Main.GetString(Const.Param.NetworksManagerName), saveDialog.FileName);
                }
            }
        }

        public List<NetworkDataModel> CreateNetworksDataModels()
        {
            var result = new List<NetworkDataModel>();
            Range.ForEach(Networks, n => result.Add(n.CreateNetworkDataModel()));
            return result;
        }

        public void RefreshNetworksDataModels()
        {
            Models = CreateNetworksDataModels();
            //ResetModelsStatistic();
            //ResetModelsDynamicStatistic();
        }

        public void MergeModels(List<NetworkDataModel> models)
        {
            var newModels = new List<NetworkDataModel>();

            foreach (var newModel in models)
            {
                var model = Models.Find(m => m.VisualId == newModel.VisualId);
                if (model != null)
                {
                    newModels.Add(model.Merge(newModel));
                }
                else
                {
                    newModels.Add(newModel);
                }
            }

            Models = newModels;
        }

        public void PrepareModelsForRun()
        {
            Range.ForEach(Models, m => m.InitState());
            ResetModelsDynamicStatistic();
            ResetModelsStatistic();
            ResetErrorMatrix();
        }

        public void PrepareModelsForRound()
        {
            foreach (var model in Models)
            {
                Range.ForEach(model.Layers.First().Neurons.Where(n => !n.IsBias), n => n.Activation = model.InputInitial0);
                if (model == Models.First())
                {
                    CtlInputData.Task.Do(model);
                }
            }

            // copy first layer state to other networks

            foreach (var model in Models)
            {
                if (model != Models.First())
                {
                    foreach (var neuron in model.Layers.First().Neurons.Where(n => !n.IsBias))
                    {
                        neuron.Activation = Models.First().Layers.First().Neurons[neuron.Id].Activation;
                    }
                }
            }
        }

        public void FeedForward()
        {
            Range.ForEach(Models, m => m.FeedForward());
        }

        public void ResetModelsStatistic()
        {
            Range.ForEach(Models, m => m.Statistic = new Statistic(true));
        }

        private void ResetModelsDynamicStatistic()
        {
            Range.ForEach(Models, m => m.DynamicStatistic = new DynamicStatistic());
        }

        public void ResetErrorMatrix()
        {
            Range.ForEach(Models, m => m.ErrorMatrix = new ErrorMatrix());
        }
    }
}

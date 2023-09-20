using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Qualia.Models;
using Qualia.Tools;
using Qualia.Tools.Functions;
using Qualia.Tools.Managers;

namespace Qualia.Network;

public sealed class NetworksManager
{
    public readonly Config Config;
    public ListX<NetworkDataModel> NetworkModels;

    private readonly TabControl _tabControls;
    private TaskFunction _taskFunction;
    private NetworkControl _selectedNetworkControl;

    public NetworksManager(TabControl tabControls, string fileName, ActionManager.ApplyActionDelegate onChanged)
    {
        this.SetUIHandler(onChanged);

        _tabControls = tabControls;
        _tabControls.SelectionChanged += NetworksTabControlsOnSelected;

        Config = string.IsNullOrEmpty(fileName) ? CreateNewManager() : new(fileName);
        if (Config == null)
        {
            return;
        }

        ClearNetworks();
        LoadConfig();
    }

    public void RefreshSelectedNetworkTab()
    {
        var selectedNetworkControl = _tabControls.SelectedContent as NetworkControl;
        if (selectedNetworkControl != null)
        {
            _selectedNetworkControl = selectedNetworkControl;
        }
        else
        {
            var isStillExist = false;
            for (var i = 0; i < _tabControls.Items.Count; ++i)
            {
                if ((_tabControls.Items.GetItemAt(i) as TabItem).Content != _selectedNetworkControl)
                {
                    continue;
                }

                isStillExist = true;
                break;
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
            return new ApplyAction(this)
            {
                Apply = (isRunning) => RefreshNetworks(sender)
            };
        }
        else
        {
            return new ApplyAction(this)
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

    public void RemoveNetwork()
    {
        if (MessageBoxResult.OK !=
                MessageBox.Show($"Would you really like to remove Network {_tabControls.SelectedIndex}?", "Confirm", MessageBoxButton.OKCancel))
        {
            return;
        }
        
        var selectedNetworkControl = SelectedNetworkControl;

        var selectedTab = _tabControls.SelectedTab();
        var index = _tabControls.SelectedIndex;
        _tabControls.SelectedIndex = index - 1;
        _tabControls.Items.Remove(selectedTab);

        if (_tabControls.SelectedIndex == 0 && _tabControls.Items.Count > 1)
        {
            _tabControls.SelectedIndex = 1;
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
                _tabControls.Items.Insert(index, selectedTab);
                _tabControls.SelectedItem = selectedTab;
                ResetNetworksTabsNames();
            }
        };

        this.InvokeUIHandler(action);
    }

    public bool IsValid()
    {
        return !NetworksControls.Any() || NetworksControls.All(n => n.IsValid());
    }

    public void SaveConfig()
    {
        Config.Set(Constants.Param.Networks, NetworksControls.Select(n => n.VisualId));
        Config.Set(Constants.Param.SelectedNetworkIndex, _tabControls.SelectedIndex - 1);

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

    public NetworkDataModel CreateNetworkDataModel(object control)
    {
        var network = GetParentNetworkControl(control);
        var networkModel = network?.CreateNetworkDataModel(_taskFunction, false);
        
        return networkModel;
    }

    public void RefreshNetworks(object control)
    {
        NetworkModels = _createNetworksDataModels(control);
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

    public unsafe void PrepareModelsForRun()
    {
        NetworkModels.ForEach(_prepareModelForRun);
    }

    public unsafe void ActivateFirstLayer()
    {
        NetworkModels.ForEach(ActivateFirstLayer);
    }

    public unsafe void DeactivateFirstLayer()
    {
        NetworkModels.ForEach(DeactivateFirstLayer);
    }

    public unsafe void DeactivateFirstLayer(NetworkDataModel network)
    {
        network.DeactivateFirstLayer();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void PrepareModelsForRound()
    {
        var baseNetwork = NetworkModels.First;
        baseNetwork.BackPropagationStrategy.PrepareForRound(baseNetwork);
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
                neuron.Target = baseNeuron.Target == baseNeuron.PositiveTargetValue ? neuron.PositiveTargetValue : neuron.NegativeTargetValue;

                neuron = neuron.Next;
                baseNeuron = baseNeuron.Next;
            }

            network.TargetOutputNeuronId = baseNetwork.TargetOutputNeuronId;

            network = network.Next;
        }
    }

    public unsafe void PrepareModelsForLoop()
    {
        NetworkModels.ForEach(network =>
        {
            if (network.Statistics == null)
            {
                _prepareModelForRun(network);
            }

            network.BackPropagationStrategy.PrepareForLoop(network);
        });
    }

    public void FeedForward()
    {
        NetworkModels.ForEach(network => network.FeedForward());
    }
 
    private static Config CreateNewManager()
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
    
    private static unsafe void ActivateFirstLayer(NetworkDataModel network)
    {
        network.ActivateFirstLayer();
    }
    
    private void NetworksTabControlsOnSelected(object sender, SelectionChangedEventArgs e)
    {
        RefreshSelectedNetworkTab();
    }
 
    private List<NetworkControl> NetworksControls
    {
        get
        {
            List<NetworkControl> result = new();
            for (var i = 1; i < _tabControls.Items.Count; ++i)
            {
                result.Add(_tabControls.Tab(i).Content as NetworkControl);
            }

            return result;
        }
    }
    
    private static NetworkControl GetParentNetworkControl(object control)
    {
        if (control is not FrameworkElement fe)
        {
            return null;
        }

        if (control is not NetworkControl network)
        {
            network = fe.GetParentOfType<NetworkControl>();
        }

        return network;
    }
    
    private void _mergeModels(ListX<NetworkDataModel> networkModels)
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
            newModels.Add(networkModel != null ? networkModel.Merge(newNetworkModel) : newNetworkModel);

            newNetworkModel = newNetworkModel.Next;
        }

        NetworkModels = newModels;
    }

    private unsafe void _prepareModelForRun(NetworkDataModel network)
    {
        network.ActivateFirstLayer();
        network.BackPropagationStrategy.PrepareForRun(network);
        network.PlotterStatistics = new();
        network.Statistics = new();
        network.ErrorMatrix.ClearData();
        network.ErrorMatrix.Next.ClearData();
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

        var yes = false;

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
        while (_tabControls.Items.Count > 1)
        {
            _tabControls.Items.RemoveAt(1);
        }
    }

    private void LoadConfig()
    {
        var networkIds = Config.Get(Constants.Param.Networks, new long[] { Constants.UnknownId });

        Qualia.Tools.Range.For(networkIds.Length, i => AddNetwork(networkIds[i]));
        _tabControls.SelectedIndex = Config.Get(Constants.Param.SelectedNetworkIndex, 0) + 1;

        RefreshNetworks(null);
    }
    
    private NetworkControl AddNetwork(long networkId)
    {
        NetworkControl network = new(networkId, Config, this.GetUIHandler());
        TabItem tabItem = new()
        {
            Header = $"Network {_tabControls.Items.Count}",
            Content = network
        };

        _tabControls.Items.Add(tabItem);
        _tabControls.SelectedItem = tabItem;

        if (networkId != Constants.UnknownId)
        {
            return network;
        }

        // A new network.
        
        network.CtlIsNetworkEnabled.Value = false;
        network.NetworkTask_OnChanged(_taskFunction);

        return network;
    }
    
    private void ResetNetworksTabsNames()
    {
        for (var i = 1; i < _tabControls.Items.Count; ++i)
        {
            _tabControls.Tab(i).Header = $"Network {i}";
        }
    }
    
    private ListX<NetworkDataModel> _createNetworksDataModels(object control)
    {
        var model = CreateNetworkDataModel(control);
        if (model == null)
        {
            ListX<NetworkDataModel> newkModels = new(NetworksControls.Count);
            NetworksControls.ForEach(network => newkModels.Add(network.CreateNetworkDataModel(_taskFunction, false)));
            _mergeModels(newkModels);

            return newkModels;
        }

        MergeModel(model);

        return NetworkModels;
    }
}
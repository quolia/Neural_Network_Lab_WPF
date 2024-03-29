﻿using System;
using System.Linq;
using System.Windows;
using Qualia.Network.Base;
using Qualia.Tools;
using Qualia.Tools.Managers;

namespace Qualia.Network.Hidden;

public sealed partial class HiddenLayerControl : LayerBaseControl
{
    public HiddenLayerControl()
        : base(0, null, null)
    {
        InitializeComponent();
    }

    public HiddenLayerControl(long id, Config config, ActionManager.ApplyActionDelegate onChanged)
        : base(id, config, onChanged)
    {
        InitializeComponent();

        LoadConfig();
    }

    // IConfigParam

    public override void LoadConfig()
    {
        var neuronsIds = this.GetConfig().Get(Constants.Param.Neurons, new long[] { Constants.UnknownId });
        foreach (var neuronId in neuronsIds)
        {
            AddNeuron(neuronId);
        }
    }

    public override void SetConfig(Config config)
    {
        throw new InvalidOperationException("Hidden layer has no config.");
    }

    public override bool IsValid()
    {
        return Neurons.All(neuron => neuron.IsValid());
    }

    public override void SaveConfig()
    {
        var ids = Neurons.Select(n => n.VisualId);
        this.GetConfig().Set(Constants.Param.Neurons, ids);

        Neurons.ToList().ForEach(n => n.SaveConfig());
    }

    public override void RemoveFromConfig()
    {
        this.GetConfig().Remove(Constants.Param.Neurons);
        Neurons.ToList().ForEach(n => n.RemoveFromConfig());
    }

    public override void InvalidateValue()
    {
        throw new InvalidOperationException("Hidden layer cannot be invalidated.");
    }

    //

    public override void LayerControl_OnLoaded()
    {
        RefreshContent();
    }

    private void RefreshContent()
    {
        CtlContent.Content = null;
        CtlContent.Content = CtlNeurons;
    }

    public override bool IsHidden => true;

    public override NeuronBaseControl AddNeuron(long id)
    {
        NeuronControl neuron = new(id, this.GetConfig(), this.GetUIHandler(), this);

        Neurons.Add(neuron);
        CtlNeurons.Items.Add(neuron);

        RefreshContent();

        if (id == Constants.UnknownId)
        {
            ApplyAction action = new(this, Notification.ParameterChanged.NeuronsAdded)
            {
                Cancel = (isRunning) =>
                {
                    Neurons.Remove(neuron);
                    CtlNeurons.Items.Remove(neuron);
                    RefreshNeuronsOrdinalNumbers();
                }
            };

            OnChanged(action);
        }

        RefreshNeuronsOrdinalNumbers();

        return neuron;
    }

    public override int RemoveNeuron(NeuronBaseControl neuron)
    {
        if (!CanNeuronBeRemoved(neuron))
        {
            MessageBox.Show("At least one neuron must exist.", "Warning", MessageBoxButton.OK);
            return 0;
        }

        var selectedNeurons = Neurons.Where(n => n.IsSelected).ToList();

        if (NeuronsSelector.Instance.IsSelected(neuron))
        {
            selectedNeurons.ForEach(n => n.SetRemovingState(true));
        }
        else
        {
            neuron.SetRemovingState(true);
        }

        ApplyAction action = new(this, Notification.ParameterChanged.NeuronsRemoved)
        {
            Apply = (isRunning) =>
            {
                if (NeuronsSelector.Instance.IsSelected(neuron))
                {
                    selectedNeurons.ForEach(RemoveNeuronWithoutConfirmation);
                }
                else
                {
                    RemoveNeuronWithoutConfirmation(neuron);
                }

                RefreshNeuronsOrdinalNumbers();
            },
            Cancel = (isRunning) =>
            {
                if (NeuronsSelector.Instance.IsSelected(neuron))
                {
                    selectedNeurons.ForEach(n => n.SetRemovingState(false));
                }
                else
                {
                    neuron.SetRemovingState(false);
                }
            }
        };

        OnChanged(action);

        return 0;
    }

    private void RemoveNeuronWithoutConfirmation(NeuronBaseControl neuron)
    {
        Neurons.Remove(neuron);
        CtlNeurons.Items.Remove(neuron);

        neuron.RemoveFromConfig();
        neuron.SaveConfig();
    }

    public override void SetAllNeuronsSelected(bool isSelected)
    {
        Qualia.Tools.Range.ForEach(Neurons, n => n.IsSelected = isSelected);
    }
}
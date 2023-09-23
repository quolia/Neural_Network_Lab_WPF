using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Qualia.Network.Base;
using Qualia.Tools;
using Qualia.Tools.Functions;
using Qualia.Tools.Managers;

namespace Qualia.Network.Input;

public sealed partial class InputLayerControl : LayerBaseControl
{
    public InputLayerControl(long id,
        Config config,
        ActionManager.ApplyActionDelegate onChanged)
        : base(id,
            config,
            onChanged)
    {
        InitializeComponent();

        this.SetConfigParams(new List<IConfigParam>
        {
            CtlInputInitial0
                .Initialize(defaultValue: 0),

            CtlInputInitial1
                .Initialize(defaultValue: 1),

            CtlAdjustFirstLayerWeights
                .Initialize(true),
        });

        this.GetConfigParams().ForEach(cp => cp.SetConfig(this.GetConfig()));

        LoadConfig();

        this.GetConfigParams().ForEach(cp => cp.SetOnChangeEvent(LayerParameter_OnChanged));
    }

    public override void LoadConfig()
    {
        this.GetConfigParams().ForEach(cp => cp.LoadConfig());

        var neuronIds = this.GetConfig().Get(Constants.Param.Neurons, Array.Empty<long>());
        foreach (var biasNeuronId in neuronIds)
        {
            AddBias(biasNeuronId);
        }
    }

    public override void LayerControl_OnLoaded()
    {
        RefreshContent();
    }

    private void RefreshContent()
    {
        CtlContent.Content = null;
        CtlContent.Content = CtlNeurons;
    }

    private void LayerParameter_OnChanged(ApplyAction action)
    {
        if (action.Param == Notification.ParameterChanged.Unknown)
        {
            action.Param = Notification.ParameterChanged.NetworkUpdated;
        }

        OnChanged(action);
    }

    public override bool IsInputLayerControl => true;

    public double Initial0 => CtlInputInitial0.Value;
    public double Initial1 => CtlInputInitial1.Value;

    public bool IsAdjustFirstLayerWeights => CtlAdjustFirstLayerWeights.Value;

    public void SetTaskFunction(TaskFunction taskFunction)
    {
        var newNeuronsCount = taskFunction != null ? taskFunction.VisualControl.GetInputCount() : 0;

        if (Neurons.Count != newNeuronsCount)
        {
            CtlNeurons.Items.Clear();
            Neurons.Clear();

            Qualia.Tools.Range.For(newNeuronsCount, _ => Neurons.Insert(0, AddNeuron()));
        }
    }

    public new NeuronBaseControl AddNeuron()
    {
        InputNeuronControl neuron = new(Neurons.Count, this)
        {
            ActivationFunction = ActivationFunction.Identiy.Instance,
            ActivationFunctionParam = 1
        };

        return neuron;
    }

    public override NeuronBaseControl AddNeuron(long biasNeuronid)
    {
        return AddBias(biasNeuronid);
    }

    private NeuronBaseControl AddBias(long biasNeuronId)
    {
        InputBiasControl neuron = new(biasNeuronId, this.GetConfig(), this.GetUIHandler(), this);

        Neurons.Add(neuron);
        CtlNeurons.Items.Add(neuron);

        RefreshContent();

        if (biasNeuronId == Constants.UnknownId)
        {
            OnChanged(new(this, Notification.ParameterChanged.NeuronsAdded));
        }

        RefreshNeuronsOrdinalNumbers();

        return neuron;
    }

    public override bool CanNeuronBeAdded() => false;
    public override bool CanNeuronBeRemoved(NeuronBaseControl neuron) => false;

    public override int RemoveNeuron(NeuronBaseControl neuron)
    {
        MessageBox.Show("Input neuron cannot be removed.", "Warning", MessageBoxButton.OK);
        return 0;
    }

    public override void SetAllNeuronsSelected(bool isSelected)
    {
        Qualia.Tools.Range.ForEach(Neurons, n => n.IsSelected = isSelected);
    }

    public override void CopyTo(LayerBaseControl layer)
    {
        if (layer is not InputLayerControl newLayer)
        {
            throw new InvalidOperationException("Cannot copy input layer to a layer of different type.");
        }

        newLayer.CtlInputInitial0.Value = CtlInputInitial0.Value;
        newLayer.CtlInputInitial1.Value = CtlInputInitial1.Value;

        base.CopyTo(newLayer);
    }

    // IConfigParam

    public override bool IsValid()
    {
        return this.GetConfigParams().All(cp => cp.IsValid()) && Neurons.All(n => n.IsValid());
    }

    public override void SaveConfig()
    {
        this.GetConfigParams().ForEach(cp => cp.SaveConfig());
    }

    public override void RemoveFromConfig()
    {
        this.GetConfig().Remove(Constants.Param.Neurons);
        this.GetConfigParams().ForEach(cp => cp.RemoveFromConfig());
    }

    public override void SetConfig(Config config)
    {
        throw new InvalidOperationException("Input layer has no config.");
    }

    public override void InvalidateValue()
    {
        throw new InvalidOperationException("Input layer cannot be invalidated");
    }

    //
}
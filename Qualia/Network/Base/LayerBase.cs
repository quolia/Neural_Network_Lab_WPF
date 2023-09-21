using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Qualia.Controls.Base;
using Qualia.Tools;
using Qualia.Tools.Managers;

namespace Qualia.Network.Base;

public abstract partial class LayerBaseControl : BaseUserControl
{
    public ObservableCollection<NeuronBaseControl> Neurons { get; } = new();

    public LayerBaseControl(long configId,
        Config config,
        ActionManager.ApplyActionDelegate onChanged)
        : base(UniqId.GetNextId(configId))
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        this.PutConfig(config.ExtendWithId(VisualId));

        SetOnChangeEvent(onChanged);

        Loaded += LayerBaseControl_Loaded;
    }

    public void RefreshNeuronsOrdinalNumbers()
    {
        var ordinalNumber = 0;
        Qualia.Tools.Range.ForEach(Neurons, n => n.SetOrdinalNumber(++ordinalNumber));
    }

    private void LayerBaseControl_Loaded(object sender, RoutedEventArgs e)
    {
        LayerControl_OnLoaded();
    }

    public void Scroll_OnChanged(object sender, ScrollChangedEventArgs e)
    {
        MaxWidth = (sender as ScrollViewer).ViewportWidth;
    }

    public NeuronBaseControl AddNeuron()
    {
        return AddNeuron(Constants.UnknownId);
    }

    public virtual bool CanNeuronBeAdded() => true;
    public virtual bool CanNeuronBeRemoved(NeuronBaseControl neuron)
    {
        var count = Neurons.Count;

        if (count < 2)
        {
            return false;
        }

        if (!neuron.IsSelected)
        {
            return true;
        }

        var selected = Neurons.Where(n => n.IsSelected).ToList();
        return count - selected.Count > 0;
    }

    public abstract void LayerControl_OnLoaded();
    public abstract NeuronBaseControl AddNeuron(long id);
    public abstract int RemoveNeuron(NeuronBaseControl neuron);

    // Layer type.

    public virtual bool IsInputLayerControl => false;
    public virtual bool IsHidden => false;
    public virtual bool IsOutputLayerControl => false;

    //

    public abstract void SetAllNeuronsSelected(bool isSelected);

    public virtual void CopyTo(LayerBaseControl newLayer)
    {
        if (GetType() != newLayer.GetType())
        {
            throw new InvalidOperationException();
        }

        var newNeuronsCount = newLayer.Neurons.Count;

        for (var i = 0; i < Neurons.Count; ++i)
        {
            var neuron = Neurons[i];
            var newNeuron = i < newNeuronsCount ? newLayer.Neurons[i] : newLayer.AddNeuron();
            neuron.CopyTo(newNeuron);
        }
    }
}
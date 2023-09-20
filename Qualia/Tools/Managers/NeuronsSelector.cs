using System.Collections.Generic;
using Qualia.Network.Base;

namespace Qualia.Tools.Managers;

public class NeuronsSelector
{
    public static readonly NeuronsSelector Instance = new();

    private readonly List<NeuronBaseControl> _selected = new();

    public int SelectedCount => _selected.Count;

    protected NeuronsSelector()
    {
        //
    }

    public bool IsSelected(NeuronBaseControl neuron)
    {
        return _selected.Contains(neuron);
    }

    public void SetSelected(NeuronBaseControl neuron, bool isSelected)
    {
        if (isSelected)
        {
            if (!IsSelected(neuron))
            {
                _selected.Add(neuron);
                neuron.OnIsSelectedChanged(isSelected);
            }
        }
        else
        {
            if (IsSelected(neuron))
            {
                _selected.Remove(neuron);
                neuron.OnIsSelectedChanged(isSelected);
            }
        }
    }

    public void CopyNeuronToSelectedNeurons(NeuronBaseControl neuron)
    {
        _selected.ForEach(neuron.CopyTo);
    }
}
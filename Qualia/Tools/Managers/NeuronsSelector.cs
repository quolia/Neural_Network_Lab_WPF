using Qualia.Controls;
using System.Collections.Generic;

namespace Qualia.Tools
{
    public class NeuronsSelector
    {
        public static readonly NeuronsSelector Instance = new();

        private readonly List<NeuronBaseControl> _selected = new();

        public int SelectedCount => _selected.Count;

        private NeuronsSelector()
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

        public void CopyParamsToSelectedNeurons(NeuronBaseControl fromNeuron)
        {
            foreach (var neuron in _selected)
            {
                CopyNeuron(fromNeuron, neuron);
            }
        }

        public static void CopyNeuron(NeuronBaseControl from, NeuronBaseControl to)
        {
            if (from == null || to == null || from == to)
            {
                return;
            }

            to.ActivationFunction = from.ActivationFunction;
            to.ActivationFunctionParam = from.ActivationFunctionParam;

            if (to is OutputNeuronControl && from is OutputNeuronControl)
            {
                to.PositiveTargetValue = from.PositiveTargetValue;
                to.NegativeTargetValue = from.NegativeTargetValue;
            }
        }
    }
}

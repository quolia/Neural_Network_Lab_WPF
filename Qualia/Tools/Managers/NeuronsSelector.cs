using Qualia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qualia.Tools
{
    public class NeuronsSelector
    {
        public static readonly NeuronsSelector Instance = new();

        private readonly List<NeuronBaseControl> _selected = new();

        //public IList<NeuronBaseControl> Selected { get => _selected; }

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
                _selected.Add(neuron);
            }
            else
            {
                _selected.Remove(neuron);
            }

            neuron.OnSelectionChanged(isSelected);
        }

        public void CopyParamsToSelected(NeuronBaseControl fromNeuron)
        {
            foreach (var neuron in _selected)
            {
                if (neuron == fromNeuron)
                {
                    continue;
                }

                neuron.ActivationFunction = fromNeuron.ActivationFunction;
                neuron.ActivationFunctionParam = fromNeuron.ActivationFunctionParam;

                if (neuron is OutputNeuronControl && fromNeuron is OutputNeuronControl)
                {
                    neuron.PositiveTargetValue = fromNeuron.PositiveTargetValue;
                    neuron.NegativeTargetValue = fromNeuron.NegativeTargetValue;
                }
            }
        }
    }
}

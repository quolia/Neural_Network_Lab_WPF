using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public partial class HiddenLayerControl : LayerBaseControl, IConfigParam
    {
        public HiddenLayerControl()
            : base(0, null, null)
        {
            InitializeComponent();
        }

        public HiddenLayerControl(long id, Config config, Action<Notification.ParameterChanged> networkUI_OnChanged)
            : base(id, config, networkUI_OnChanged)
        {
            InitializeComponent();

            var neuronsIds = Config.Get(Constants.Param.Neurons, new long[] { Constants.UnknownId });

            foreach (var neuronId in neuronsIds)
            {
                AddNeuron(neuronId);
            }
        }

        public override bool IsHidden => true;

        public override void AddNeuron(long id)
        {
            NeuronControl neuron = new(id, Config, NetworkUI_OnChanged, this);

            CtlContent.Content = null;
            Neurons.Add(neuron);
            CtlNeurons.Items.Add(neuron);
            CtlContent.Content = CtlNeurons;

            if (id == Constants.UnknownId)
            {
                NetworkUI_OnChanged(Notification.ParameterChanged.NeuronsCount);
            }

            RefreshOrdinalNumbers();
        }

        public override bool RemoveNeuron(NeuronBaseControl neuron)
        {
            if (!CanNeuronBeRemoved())
            {
                MessageBox.Show("At least one neuron must exist.", "Warning", MessageBoxButton.OK);
                return false;
            }

            var color = neuron.Background;
            neuron.Background = Draw.GetBrush(in ColorsX.Tomato);
            if (MessageBox.Show("Would you really like to delete the neuron?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                Neurons.Remove(neuron);
                CtlNeurons.Items.Remove(neuron);
                neuron.RemoveFromConfig();
                neuron.SaveConfig();
                RefreshOrdinalNumbers();
                NetworkUI_OnChanged(Notification.ParameterChanged.NeuronsCount);
                return true;
            }

            neuron.Background = color;
            return false;
        }

        public override bool IsValid()
        {
            return Neurons.All(neuron => neuron.IsValid());
        }

        public override void SaveConfig()
        {
            var ids = Neurons.Select(ctlNeuron => ctlNeuron.Id);
            Config.Set(Constants.Param.Neurons, ids);

            Neurons.ToList().ForEach(n => n.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            Config.Remove(Constants.Param.Neurons);
            Neurons.ToList().ForEach(n => n.RemoveFromConfig());
        }
    }
}

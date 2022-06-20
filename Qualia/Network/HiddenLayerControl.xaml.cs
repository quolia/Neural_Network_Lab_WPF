using Qualia.Tools;
using System;
using System.Linq;
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

        public override Panel NeuronsHolder => CtlNeuronsHolder;

        public override void AddNeuron(long id)
        {
            NeuronControl neuron = new(id, Config, NetworkUI_OnChanged);
            NeuronsHolder.Children.Add(neuron);

            if (id == Constants.UnknownId)
            {
                NetworkUI_OnChanged(Notification.ParameterChanged.NeuronsCount);
            }

            RefreshOrdinalNumbers();
        }

        public override bool IsValid()
        {
            return GetNeuronsControls().All(neuron => neuron.IsValid());
        }

        public override void SaveConfig()
        {
            var ctlNeurons = GetNeuronsControls().ToList();
            var ids = ctlNeurons.Select(ctlNeuron => ctlNeuron.Id);
            Config.Set(Constants.Param.Neurons, ids);

            ctlNeurons.ForEach(ctlNeuron => ctlNeuron.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            Config.Remove(Constants.Param.Neurons);
            var ctlNeurons = GetNeuronsControls().ToList();
            ctlNeurons.ForEach(ctlNeuron => ctlNeuron.RemoveFromConfig());
        }
    }
}

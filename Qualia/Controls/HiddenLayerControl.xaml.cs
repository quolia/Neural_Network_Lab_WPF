using System;
using System.Linq;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    sealed public partial class HiddenLayerControl : LayerBase
    {
        public HiddenLayerControl()
            : base(0, null, null)
        {
            InitializeComponent();
        }

        public HiddenLayerControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            var neuronsIds = Config.GetArray(Constants.Param.Neurons);
            if (neuronsIds.Length == 0)
            {
                neuronsIds = new long[] { Constants.UnknownId };
            }

            foreach (var neuronId in neuronsIds)
            {
                AddNeuron(neuronId);
            }
        }

        public override bool IsHidden => true;

        public override Panel NeuronsHolder => CtlNeuronsHolder;

        public override void AddNeuron(long id)
        {
            NeuronControl neuron = new(id, Config, OnNetworkUIChanged);
            NeuronsHolder.Children.Add(neuron);

            if (id == Constants.UnknownId)
            {
                OnNetworkUIChanged(Notification.ParameterChanged.NeuronsCount);
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

        public override void VanishConfig()
        {
            Config.Remove(Constants.Param.Neurons);
            var ctlNeurons = GetNeuronsControls().ToList();
            ctlNeurons.ForEach(ctlNeuron => ctlNeuron.VanishConfig());
        }
    }
}

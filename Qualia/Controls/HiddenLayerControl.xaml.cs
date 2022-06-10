using System;
using System.Linq;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public partial class HiddenLayerControl : LayerBase
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

            var neuronsIds = Config.GetArray(Const.Param.Neurons);
            if (neuronsIds.Length == 0)
            {
                neuronsIds = new long[] { Const.UnknownId };
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
            var neuron = new NeuronControl(id, Config, OnNetworkUIChanged);
            NeuronsHolder.Children.Add(neuron);

            if (id == Const.UnknownId)
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
            Config.Set(Const.Param.Neurons, ids);

            ctlNeurons.ForEach(ctlNeuron => ctlNeuron.SaveConfig());
        }

        public override void VanishConfig()
        {
            Config.Remove(Const.Param.Neurons);
            var ctlNeurons = GetNeuronsControls().ToList();
            ctlNeurons.ForEach(ctlNeuron => ctlNeuron.VanishConfig());
        }
    }
}

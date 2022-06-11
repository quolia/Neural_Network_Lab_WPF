using System;
using System.Linq;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    sealed public partial class OutputLayerControl : LayerBase
    {
        public OutputLayerControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            var neuronIds = Config.GetArray(Constants.Param.Neurons);
            neuronIds.ToList().ForEach(neuronId => AddNeuron(neuronId));

            if (neuronIds.Length == 0)
            {
                Range.For(Constants.DefaultOutputNeuronsCount, _ => AddNeuron(Constants.UnknownId));
            }
        }

        public override bool IsOutput => true;

        public override Panel NeuronsHolder => CtlNeuronsHolder;

        private void CtlMenuAddNeuron_Click(object sender, EventArgs e)
        {
            AddNeuron(Constants.UnknownId);
        }

        public override void AddNeuron(long neuronId)
        {
            OutputNeuronControl ctlNeuron = new(neuronId, Config, OnNetworkUIChanged);
            NeuronsHolder.Children.Add(ctlNeuron);

            if (neuronId == Constants.UnknownId)
            {
                OnNetworkUIChanged(Notification.ParameterChanged.NeuronsCount);
            }

            RefreshOrdinalNumbers();
        }

        public override bool IsValid()
        {
            var ctlNeurons = GetNeuronsControls();
            return ctlNeurons.All(ctlNeuron => ctlNeuron.IsValid());
        }

        public override void SaveConfig()
        {
            var ctlNeurons = GetNeuronsControls();
            Config.Set(Constants.Param.Neurons, ctlNeurons.Select(ctlNeuron => ctlNeuron.Id));
            ctlNeurons.ToList().ForEach(ctlNeuron => ctlNeuron.SaveConfig());
        }

        public override void VanishConfig()
        {
            Config.Remove(Constants.Param.Neurons);

            var ctlNeurons = GetNeuronsControls();
            ctlNeurons.ToList().ForEach(ctlNeuron => ctlNeuron.VanishConfig());
        }

        public void OnTaskChanged(INetworkTask networkTask)
        {
            var ctlNeurons = NeuronsHolder.Children.OfType<OutputNeuronControl>().ToList();

            for (int i = ctlNeurons.Count; i < networkTask.GetClasses().Count; ++i)
            {
                AddNeuron();
            }

            for (int i = networkTask.GetClasses().Count; i < ctlNeurons.Count; ++i)
            {
                NeuronsHolder.Children.RemoveAt(NeuronsHolder.Children.Count - 1);
            }
        }
    }
}

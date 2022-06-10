using System;
using System.Linq;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public partial class OutputLayerControl : LayerBase
    {
        public OutputLayerControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            var neuronIds = Config.GetArray(Const.Param.Neurons);
            neuronIds.ToList().ForEach(neuronId => AddNeuron(neuronId));

            if (neuronIds.Length == 0)
            {
                Range.For(Const.DefaultOutputNeuronsCount, _ => AddNeuron(Const.UnknownId));
            }
        }

        public override bool IsOutput => true;

        public override Panel NeuronsHolder => CtlNeuronsHolder;

        private void CtlMenuAddNeuron_Click(object sender, EventArgs e)
        {
            AddNeuron(Const.UnknownId);
        }

        public override void AddNeuron(long neuronId)
        {
            var ctlNeuron = new OutputNeuronControl(neuronId, Config, OnNetworkUIChanged);
            NeuronsHolder.Children.Add(ctlNeuron);

            if (neuronId == Const.UnknownId)
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
            Config.Set(Const.Param.Neurons, ctlNeurons.Select(ctlNeuron => ctlNeuron.Id));
            ctlNeurons.ToList().ForEach(ctlNeuron => ctlNeuron.SaveConfig());
        }

        public override void VanishConfig()
        {
            Config.Remove(Const.Param.Neurons);

            var ctlNeurons = GetNeuronsControls();
            ctlNeurons.ToList().ForEach(ctlNeuron => ctlNeuron.VanishConfig());
        }

        public void OnTaskChanged(INetworkTask networkTask)
        {
            var ctlNeurons = NeuronsHolder.Children.OfType<OutputNeuronControl>().ToList();

            for (int ind = ctlNeurons.Count; ind < networkTask.GetClasses().Count; ++ind)
            {
                AddNeuron();
            }

            for (int ind = networkTask.GetClasses().Count; ind < ctlNeurons.Count; ++ind)
            {
                NeuronsHolder.Children.RemoveAt(NeuronsHolder.Children.Count - 1);
            }
        }
    }
}

using Qualia.Tools;
using System;
using System.Linq;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public partial class OutputLayerControl : LayerBaseControl
    {
        public OutputLayerControl(long id, Config config, Action<Notification.ParameterChanged> networkUI_OnChanged)
            : base(id, config, networkUI_OnChanged)
        {
            InitializeComponent();

            var neuronIds = Config.Get(Constants.Param.Neurons, Array.Empty<long>());
            neuronIds.ToList().ForEach(AddNeuron);

            if (neuronIds.Length == 0)
            {
                Range.For(Constants.DefaultOutputNeuronsCount, _ => AddNeuron(Constants.UnknownId));
            }
        }

        public override bool IsOutput => true;

        public override Panel NeuronsHolder => CtlNeuronsHolder;

        private void MenuAddNeuron_OnClick(object sender, EventArgs e)
        {
            AddNeuron(Constants.UnknownId);
        }

        public override void AddNeuron(long neuronId)
        {
            OutputNeuronControl neuron = new(neuronId, Config, NetworkUI_OnChanged);
            NeuronsHolder.Children.Add(neuron);

            if (neuronId == Constants.UnknownId)
            {
                NetworkUI_OnChanged(Notification.ParameterChanged.NeuronsCount);
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

        public override void RemoveFromConfig()
        {
            Config.Remove(Constants.Param.Neurons);

            var ctlNeurons = GetNeuronsControls();
            ctlNeurons.ToList().ForEach(ctlNeuron => ctlNeuron.RemoveFromConfig());
        }

        public void NetworkTask_OnChanged(TaskFunction taskFunction)
        {
            var ctlNeurons = NeuronsHolder.Children.OfType<OutputNeuronControl>().ToList();

            for (int i = ctlNeurons.Count; i < taskFunction.ITaskControl.GetClasses().Count; ++i)
            {
                AddNeuron();
            }

            for (int i = taskFunction.ITaskControl.GetClasses().Count; i < ctlNeurons.Count; ++i)
            {
                NeuronsHolder.Children.RemoveAt(NeuronsHolder.Children.Count - 1);
            }
        }
    }
}

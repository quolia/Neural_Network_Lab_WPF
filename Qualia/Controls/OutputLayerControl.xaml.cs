using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tools;

namespace Qualia.Controls
{
    public partial class OutputLayerControl : LayerBase
    {
        public OutputLayerControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            var neurons = Config.GetArray(Const.Param.Neurons);
            neurons.ToList().ForEach(n => AddNeuron(n));

            if (neurons.Length == 0)
            {
                Range.For(Const.DefaultOutputNeuronsCount, c => AddNeuron(Const.UnknownId));
            }
        }

        public override bool IsOutput => true;
        public override Panel NeuronsHolder => CtlNeuronsHolder;

        private void CtlMenuAddNeuron_Click(object sender, EventArgs e)
        {
            AddNeuron(Const.UnknownId);
        }

        public override void AddNeuron(long id)
        {
            var neuron = new OutputNeuronControl(id, Config, OnNetworkUIChanged);
            NeuronsHolder.Children.Add(neuron);

            if (id == Const.UnknownId)
            {
                OnNetworkUIChanged(Notification.ParameterChanged.NeuronsCount);
            }

            RefreshOrdinalNumbers();
        }

        public override bool IsValid()
        {
            return GetNeuronsControls().All(n => n.IsValid());
        }

        public override void SaveConfig()
        {
            var neurons = GetNeuronsControls();
            Config.Set(Const.Param.Neurons, neurons.Select(n => n.Id));
            neurons.ForEach(n => n.SaveConfig());
        }

        public override void VanishConfig()
        {
            Config.Remove(Const.Param.Neurons);
            GetNeuronsControls().ForEach(n => n.VanishConfig());
        }

        public void OnTaskChanged(INetworkTask task)
        {
            var controls = NeuronsHolder.Children.OfType<OutputNeuronControl>().ToList();

            for (int i = controls.Count; i < task.GetClasses().Count; ++i)
            {
                AddNeuron();
            }

            for (int i = task.GetClasses().Count; i < controls.Count; ++i)
            {
                NeuronsHolder.Children.RemoveAt(NeuronsHolder.Children.Count - 1);
            }
        }
    }
}

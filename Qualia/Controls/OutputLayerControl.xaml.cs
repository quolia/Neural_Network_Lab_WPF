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
    /// <summary>
    /// Interaction logic for OutputLayerControl.xaml
    /// </summary>
    public partial class OutputLayerControl : LayerBase
    {
        /*
        public OutputLayerControl()
        {
            InitializeComponent();
        }
        */

        public OutputLayerControl(long id, Config config, Action<Notification.ParameterChanged, object> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            var neurons = Config.GetArray(Const.Param.Neurons);
            Range.ForEach(neurons, n => AddNeuron(n));

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
                OnNetworkUIChanged(Notification.ParameterChanged.NeuronsCount, null);
            }
        }

        public override bool IsValid()
        {
            bool result = true;
            var neurons = GetNeuronsControls();
            Range.ForEach(neurons, n => result &= n.IsValid());
            return result;
        }

        public override void SaveConfig()
        {
            var neurons = GetNeuronsControls();
            Config.Set(Const.Param.Neurons, neurons.Select(n => n.Id));
            Range.ForEach(neurons, n => n.SaveConfig());
        }

        public override void VanishConfig()
        {
            Config.Remove(Const.Param.Neurons);
            Range.ForEach(GetNeuronsControls(), n => n.VanishConfig());
        }
    }
}

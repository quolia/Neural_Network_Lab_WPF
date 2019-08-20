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

            var neurons = Config.GetArray(Const.Param.Neurons);
            if (neurons.Length == 0)
            {
                neurons = new long[] { Const.UnknownId };
            }

            foreach (var neuron in neurons)
            {
                AddNeuron(neuron);
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
    }
}

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
    public partial class InputLayerControl : LayerBase
    {
        public InputLayerControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            LoadConfig();

            CtlInputInitial0.SetChangeEvent(ParameterChanged);
            CtlInputInitial1.SetChangeEvent(ParameterChanged);
            CtlActivationFunc.SetChangeEvent(ParameterChanged);
            CtlActivationFuncParamA.SetChangeEvent(ParameterChanged);
        }

        private void ParameterChanged()
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        public override bool IsInput => true;
        public override Panel NeuronsHolder => CtlNeuronsHolder;
        public override int NeuronsCount => GetNeuronsControls().Count;

        public double Initial0 => CtlInputInitial0.Value;
        public double Initial1 => CtlInputInitial1.Value;
        public string ActivationFunc => CtlActivationFunc.SelectedItem.ToString();
        public double? ActivationFuncParamA => CtlActivationFuncParamA.ValueOrNull;

        public void OnTaskChanged(INetworkTask task)
        {
            var controls = NeuronsHolder.Children.OfType<InputNeuronControl>().ToList();
            controls.ForEach(c => NeuronsHolder.Children.Remove(c));
            if (task != null)
            {
                Range.For(task.GetInputCount(), n => NeuronsHolder.Children.Insert(0, AddNeuron()));
            }
        }

        private void LoadConfig()
        {
            ActivationFunction.Helper.FillComboBox(CtlActivationFunc, Config, nameof(ActivationFunction.None));
            CtlInputInitial0.Load(Config);
            CtlInputInitial1.Load(Config);
            CtlActivationFuncParamA.Load(Config);

            var neurons = Config.GetArray(Const.Param.Neurons);
            foreach (var bias in neurons)
            {
                AddBias(bias);
            }
        }

        public new InputNeuronControl AddNeuron()
        {
            var neuron = new InputNeuronControl(NeuronsHolder.Children.Count);
            neuron.ActivationFunc = CtlActivationFunc.SelectedItem.ToString();
            neuron.ActivationFuncParamA = CtlActivationFuncParamA.ValueOrNull;
            return neuron;
        }

        public override void AddNeuron(long id)
        {
            AddBias(id);
        }

        public void AddBias(long id)
        {
            var neuron = new InputBiasControl(id, Config, OnNetworkUIChanged);
            NeuronsHolder.Children.Add(neuron);

            if (id == Const.UnknownId)
            {
                OnNetworkUIChanged(Notification.ParameterChanged.NeuronsCount);
            }

            RefreshOrdinalNumbers();
        }

        public override bool IsValid()
        {
            return CtlInputInitial0.IsValid() &&
                   CtlInputInitial1.IsValid() &&
                   CtlActivationFuncParamA.IsValid() &&
                   GetNeuronsControls().All(n => n.IsValid());
        }

        public override void SaveConfig()
        {
            CtlActivationFunc.Save(Config);
            CtlInputInitial0.Save(Config);
            CtlInputInitial1.Save(Config);
            CtlActivationFuncParamA.Save(Config);

            var neurons = GetNeuronsControls().Where(n => n.IsBias);
            Config.Set(Const.Param.Neurons, neurons.Select(n => n.Id));
            foreach (var neuron in neurons)
            {
                neuron.SaveConfig();
            }
        }

        public override void VanishConfig()
        {
            Config.Remove(Const.Param.Neurons);
            CtlInputInitial0.Vanish(Config);
            CtlInputInitial1.Vanish(Config);
            CtlActivationFunc.Vanish(Config);
            CtlActivationFuncParamA.Vanish(Config);
            foreach (var neuron in GetNeuronsControls())
            {
                neuron.VanishConfig();
            }
        }

        private void CtlMenuAddBias_Click(object sender, EventArgs e)
        {
            AddBias(Const.UnknownId);
        }
    }
}

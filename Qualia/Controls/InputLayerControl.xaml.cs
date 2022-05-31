using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public partial class InputLayerControl : LayerBase
    {
        private readonly List<IConfigValue> _configParams;

        public InputLayerControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            _configParams = new List<IConfigValue>()
            {
                CtlInputInitial0,
                CtlInputInitial1,
                CtlActivationFunc,
                CtlActivationFuncParamA,
                CtlAdjustFirstLayerWeights,
                CtlWeightsInitializer,
                CtlWeightsInitializerParamA
            };

            _configParams.ForEach(p => p.SetConfig(Config));
            LoadConfig();
            _configParams.ForEach(p => p.SetChangeEvent(ParameterChanged));
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
        public string WeightsInitializer => CtlWeightsInitializer.SelectedItem.ToString();
        public double? WeightsInitializerParamA => CtlWeightsInitializerParamA.ValueOrNull;
        public bool IsAdjustFirstLayerWeights => CtlAdjustFirstLayerWeights.IsOn;

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
            InitializeMode.Helper.FillComboBox(CtlWeightsInitializer, Config, nameof(InitializeMode.None));
            _configParams.ForEach(p => p.LoadConfig());

            var neurons = Config.GetArray(Const.Param.Neurons);
            foreach (var bias in neurons)
            {
                AddBias(bias);
            }
        }

        public new InputNeuronControl AddNeuron()
        {
            var neuron = new InputNeuronControl(NeuronsHolder.Children.Count)
            {
                ActivationFunc = CtlActivationFunc.SelectedItem.ToString(),
                ActivationFuncParamA = CtlActivationFuncParamA.ValueOrNull
            };

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
            return _configParams.All(p => p.IsValid()) && GetNeuronsControls().All(n => n.IsValid());
        }

        public override void SaveConfig()
        {
            _configParams.ForEach(p => p.SaveConfig());

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
            _configParams.ForEach(p => p.VanishConfig());
            GetNeuronsControls().ForEach(n => n.VanishConfig());
        }

        private void CtlMenuAddBias_Click(object sender, EventArgs e)
        {
            AddBias(Const.UnknownId);
        }
    }
}

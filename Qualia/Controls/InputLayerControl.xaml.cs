using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public partial class InputLayerControl : LayerBase
    {
        private readonly List<IConfigParam> _configParams;

        public InputLayerControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            _configParams = new List<IConfigParam>()
            {
                CtlInputInitial0,
                CtlInputInitial1,
                CtlActivationFunc,
                CtlActivationFuncParamA,
                CtlAdjustFirstLayerWeights,
                CtlWeightsInitializer,
                CtlWeightsInitializerParamA
            };

            _configParams.ForEach(param => param.SetConfig(Config));
            LoadConfig();
            _configParams.ForEach(param => param.SetChangeEvent(ParameterChanged));
        }

        private void ParameterChanged()
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        public override bool IsInput => true;
        public override Panel NeuronsHolder => CtlNeuronsHolder;
        //public override int NeuronsCount => GetNeuronsControls().Count;

        public double Initial0 => CtlInputInitial0.Value;
        public double Initial1 => CtlInputInitial1.Value;
        public string ActivationFunc => CtlActivationFunc.SelectedItem.ToString();
        public double? ActivationFuncParamA => CtlActivationFuncParamA.ValueOrNull;
        public string WeightsInitializer => CtlWeightsInitializer.SelectedItem.ToString();
        public double? WeightsInitializerParamA => CtlWeightsInitializerParamA.ValueOrNull;
        public bool IsAdjustFirstLayerWeights => CtlAdjustFirstLayerWeights.IsOn;

        public void OnTaskChanged(INetworkTask task)
        {
            var ctlNeurons = NeuronsHolder.Children.OfType<InputNeuronControl>().ToList();
            ctlNeurons.ForEach(ctlNeuron => NeuronsHolder.Children.Remove(ctlNeuron));

            if (task != null)
            {
                Range.For(task.GetInputCount(), _ => NeuronsHolder.Children.Insert(0, AddNeuron()));
            }
        }

        private void LoadConfig()
        {
            ActivationFunction.Helper.FillComboBox(CtlActivationFunc, Config, nameof(ActivationFunction.None));
            InitializeMode.Helper.FillComboBox(CtlWeightsInitializer, Config, nameof(InitializeMode.None));

            _configParams.ForEach(param => param.LoadConfig());

            var neuronIds = Config.GetArray(Const.Param.Neurons);
            foreach (var biasNeuronId in neuronIds)
            {
                AddBias(biasNeuronId);
            }
        }

        public new InputNeuronControl AddNeuron()
        {
            var ctlNeuron = new InputNeuronControl(NeuronsHolder.Children.Count)
            {
                ActivationFunc = CtlActivationFunc.SelectedItem.ToString(),
                ActivationFuncParamA = CtlActivationFuncParamA.ValueOrNull
            };

            return ctlNeuron;
        }

        public override void AddNeuron(long biasNeuronid)
        {
            AddBias(biasNeuronid);
        }

        public void AddBias(long biasNeuronId)
        {
            var ctlNeuron = new InputBiasControl(biasNeuronId, Config, OnNetworkUIChanged);
            NeuronsHolder.Children.Add(ctlNeuron);

            if (biasNeuronId == Const.UnknownId)
            {
                OnNetworkUIChanged(Notification.ParameterChanged.NeuronsCount);
            }

            RefreshOrdinalNumbers();
        }

        public override bool IsValid()
        {
            var ctlNeurons = GetNeuronsControls();
            return _configParams.All(param => param.IsValid()) && ctlNeurons.All(ctlNeuron => ctlNeuron.IsValid());
        }

        public override void SaveConfig()
        {
            _configParams.ForEach(param => param.SaveConfig());

            var ctlNeurons = GetNeuronsControls().Where(ctlNeuron => ctlNeuron.IsBias);
            Config.Set(Const.Param.Neurons, ctlNeurons.Select(ctlNeuron => ctlNeuron.Id));
            ctlNeurons.ToList().ForEach(neuron => neuron.SaveConfig());
        }

        public override void VanishConfig()
        {
            Config.Remove(Const.Param.Neurons);
            _configParams.ForEach(param => param.VanishConfig());
            var ctlNeurons = GetNeuronsControls();
            ctlNeurons.ToList().ForEach(ctlNeuron => ctlNeuron.VanishConfig());
        }

        private void CtlMenuAddBias_Click(object sender, EventArgs e)
        {
            AddBias(Const.UnknownId);
        }
    }
}

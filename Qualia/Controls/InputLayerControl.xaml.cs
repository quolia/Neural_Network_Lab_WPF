using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public partial class InputLayerControl : LayerBase
    {
        private readonly List<IConfigParam> _configParams;

        public InputLayerControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlInputInitial0,
                CtlInputInitial1,
                CtlActivationFunc,
                CtlActivationFuncParam,
                CtlAdjustFirstLayerWeights,
                CtlWeightsInitializer,
                CtlWeightsInitializerParam
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
        public double Initial0 => CtlInputInitial0.Value;
        public double Initial1 => CtlInputInitial1.Value;
        public string ActivationFunc => CtlActivationFunc.SelectedItem.ToString();
        public double? ActivationFuncParam => CtlActivationFuncParam.ValueOrNull;
        public InitializeFunction WeightsInitializer => InitializeFunctionList.Helper.GetInstance(CtlWeightsInitializer.SelectedItem.ToString());
        public double? WeightsInitializerParam => CtlWeightsInitializerParam.ValueOrNull;
        public bool IsAdjustFirstLayerWeights => CtlAdjustFirstLayerWeights.IsOn;

        public void OnTaskChanged(INetworkTask task)
        {
            var ctlNeurons = NeuronsHolder.Children.OfType<InputNeuronControl>().ToList();
            ctlNeurons.ForEach(NeuronsHolder.Children.Remove);

            if (task != null)
            {
                Range.For(task.GetInputCount(), _ => NeuronsHolder.Children.Insert(0, AddNeuron()));
            }
        }

        private void LoadConfig()
        {
            ActivationFunctionList.Helper.FillComboBox(CtlActivationFunc, Config, nameof(ActivationFunctionList.None));
            InitializeFunctionList.Helper.FillComboBox(CtlWeightsInitializer, Config, nameof(InitializeFunctionList.None));

            _configParams.ForEach(param => param.LoadConfig());

            var neuronIds = Config.GetArray(Constants.Param.Neurons);
            foreach (var biasNeuronId in neuronIds)
            {
                AddBias(biasNeuronId);
            }
        }

        public new InputNeuronControl AddNeuron()
        {
            InputNeuronControl ctlNeuron = new(NeuronsHolder.Children.Count)
            {
                ActivationFunc = CtlActivationFunc.SelectedItem.ToString(),
                ActivationFuncParam = CtlActivationFuncParam.ValueOrNull
            };

            return ctlNeuron;
        }

        public override void AddNeuron(long biasNeuronid)
        {
            AddBias(biasNeuronid);
        }

        public void AddBias(long biasNeuronId)
        {
            InputBiasControl ctlNeuron = new(biasNeuronId, Config, OnNetworkUIChanged);
            NeuronsHolder.Children.Add(ctlNeuron);

            if (biasNeuronId == Constants.UnknownId)
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
            Config.Set(Constants.Param.Neurons, ctlNeurons.Select(ctlNeuron => ctlNeuron.Id));
            ctlNeurons.ToList().ForEach(neuron => neuron.SaveConfig());
        }

        public override void VanishConfig()
        {
            Config.Remove(Constants.Param.Neurons);
            _configParams.ForEach(param => param.VanishConfig());
            var ctlNeurons = GetNeuronsControls();
            ctlNeurons.ToList().ForEach(ctlNeuron => ctlNeuron.VanishConfig());
        }

        private void CtlMenuAddBias_Click(object sender, EventArgs e)
        {
            AddBias(Constants.UnknownId);
        }
    }
}

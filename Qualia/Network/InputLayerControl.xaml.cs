using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public partial class InputLayerControl : LayerBaseControl
    {
        private readonly List<IConfigParam> _configParams;

        public InputLayerControl(long id, Config config, Action<Notification.ParameterChanged> networkUI_OnChanged)
            : base(id, config, networkUI_OnChanged)
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlInputInitial0
                    .Initialize(defaultValue: 0),

                CtlInputInitial1
                    .Initialize(defaultValue: 1),

                CtlActivationFunction
                    .Initialize(nameof(ActivationFunction.LogisticSigmoid)),

                CtlActivationFunctionParam
                    .Initialize(defaultValue: 1),

                CtlAdjustFirstLayerWeights
                    .Initialize(true),

                CtlWeightsInitializeFunction
                    .Initialize(nameof(InitializeFunction.FlatRandom)),

                CtlWeightsInitializeFunctionParam
                    .Initialize(defaultValue: 1)
            };

            _configParams.ForEach(param => param.SetConfig(Config));

            LoadConfig();

            _configParams.ForEach(param => param.SetOnChangeEvent(Parameter_OnChanged));
        }

        private void Parameter_OnChanged(Notification.ParameterChanged _)
        {
            NetworkUI_OnChanged(Notification.ParameterChanged.Structure);
        }

        public override bool IsInput => true;
        public override Panel NeuronsHolder => CtlNeuronsHolder;
        public double Initial0 => CtlInputInitial0.Value;
        public double Initial1 => CtlInputInitial1.Value;
        public ActivationFunction ActivationFunction => ActivationFunction.GetInstance(CtlActivationFunction);
        public double ActivationFunctionParam => CtlActivationFunctionParam.Value;
        public InitializeFunction WeightsInitializeFunction => InitializeFunction.GetInstance(CtlWeightsInitializeFunction);
        public double WeightsInitializeFunctionParam => CtlWeightsInitializeFunctionParam.Value;
        public bool IsAdjustFirstLayerWeights => CtlAdjustFirstLayerWeights.Value;

        public void NetworkTask_OnChanged(TaskFunction taskFunction)
        {
            var ctlNeurons = NeuronsHolder.Children.OfType<InputNeuronControl>().ToList();
            ctlNeurons.ForEach(NeuronsHolder.Children.Remove);

            if (taskFunction != null)
            {
                Range.For(taskFunction.ITaskControl.GetInputCount(), _ => NeuronsHolder.Children.Insert(0, AddNeuron()));
            }
        }

        private void LoadConfig()
        {
            CtlActivationFunction
                .Fill<ActivationFunction>(Config);

            CtlWeightsInitializeFunction
                 .Fill<InitializeFunction>(Config);

            _configParams.ForEach(param => param.LoadConfig());

            var neuronIds = Config.Get(Constants.Param.Neurons, Array.Empty<long>());
            foreach (var biasNeuronId in neuronIds)
            {
                AddBias(biasNeuronId);
            }
        }

        public new InputNeuronControl AddNeuron()
        {
            InputNeuronControl ctlNeuron = new(NeuronsHolder.Children.Count)
            {
                ActivationFunction = ActivationFunction.GetInstance(CtlActivationFunction),
                ActivationFunctionParam = CtlActivationFunctionParam.Value
            };

            return ctlNeuron;
        }

        public override void AddNeuron(long biasNeuronid)
        {
            AddBias(biasNeuronid);
        }

        public void AddBias(long biasNeuronId)
        {
            InputBiasControl ctlNeuron = new(biasNeuronId, Config, NetworkUI_OnChanged);
            NeuronsHolder.Children.Add(ctlNeuron);

            if (biasNeuronId == Constants.UnknownId)
            {
                NetworkUI_OnChanged(Notification.ParameterChanged.NeuronsCount);
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

        public override void RemoveFromConfig()
        {
            Config.Remove(Constants.Param.Neurons);
            _configParams.ForEach(param => param.RemoveFromConfig());
            var ctlNeurons = GetNeuronsControls();
            ctlNeurons.ToList().ForEach(ctlNeuron => ctlNeuron.RemoveFromConfig());
        }

        private void MenuAddBias_OnClick(object sender, EventArgs e)
        {
            AddBias(Constants.UnknownId);
        }
    }
}

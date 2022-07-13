using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
        public double Initial0 => CtlInputInitial0.Value;
        public double Initial1 => CtlInputInitial1.Value;
        public ActivationFunction ActivationFunction => ActivationFunction.GetInstance(CtlActivationFunction);
        public double ActivationFunctionParam => CtlActivationFunctionParam.Value;
        public InitializeFunction WeightsInitializeFunction => InitializeFunction.GetInstance(CtlWeightsInitializeFunction);
        public double WeightsInitializeFunctionParam => CtlWeightsInitializeFunctionParam.Value;
        public bool IsAdjustFirstLayerWeights => CtlAdjustFirstLayerWeights.Value;

        public void NetworkTask_OnChanged(TaskFunction taskFunction)
        {
            //var ctlNeurons = Neurons;// NeuronsHolder.Items.OfType<InputNeuronControl>().ToList();
            CtlNeurons.Items.Clear();// .ForEach(Neurons.Remove);
            Neurons.Clear();

            if (taskFunction != null)
            {
                Range.For(taskFunction.ITaskControl.GetInputCount(), _ => Neurons.Insert(0, AddNeuron()));
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
            InputNeuronControl ctlNeuron = new(Neurons.Count, this)
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
            InputBiasControl neuron = new(biasNeuronId, Config, NetworkUI_OnChanged, this);

            CtlContent.Content = null;
            Neurons.Add(neuron);
            CtlNeurons.Items.Add(neuron);
            CtlContent.Content = CtlNeurons;

            if (biasNeuronId == Constants.UnknownId)
            {
                NetworkUI_OnChanged(Notification.ParameterChanged.NeuronsCount);
            }

            RefreshOrdinalNumbers();
        }

        public override bool RemoveNeuron(NeuronBaseControl neuron)
        {
            MessageBox.Show("Input neuron cannot be removed.", "Warning", MessageBoxButton.OK);
            return false;
        }

        public override bool IsValid()
        {
            return _configParams.All(p => p.IsValid()) && Neurons.All(n => n.IsValid());
        }

        public override void SaveConfig()
        {
            _configParams.ForEach(p => p.SaveConfig());

            var neurons = Neurons.Where(n => n.IsBias);
            Config.Set(Constants.Param.Neurons, neurons.Select(n => n.Id));
            neurons.ToList().ForEach(n => n.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            Config.Remove(Constants.Param.Neurons);
            _configParams.ForEach(p => p.RemoveFromConfig());
            Neurons.ToList().ForEach(n => n.RemoveFromConfig());
        }

        private void MenuAddBias_OnClick(object sender, EventArgs e)
        {
            AddBias(Constants.UnknownId);
        }
    }
}

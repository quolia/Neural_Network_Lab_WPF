using Qualia.Tools;
using System;
using System.Linq;
using System.Windows;

namespace Qualia.Controls
{
    sealed public partial class InputLayerControl : LayerBaseControl
    {
        public InputLayerControl(long id,
                                 Config config,
                                 Action<Notification.ParameterChanged> onChanged)
            : base(id,
                   config,
                   onChanged)
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

            _configParams.ForEach(cp => cp.SetConfig(_config));

            LoadConfig();

            _configParams.ForEach(cp => cp.SetOnChangeEvent(LayerParameter_OnChanged));
        }

        public override void LoadConfig()
        {
            CtlActivationFunction
                .Fill<ActivationFunction>(_config);

            CtlWeightsInitializeFunction
                 .Fill<InitializeFunction>(_config);

            _configParams.ForEach(cp => cp.LoadConfig());

            var neuronIds = _config.Get(Constants.Param.Neurons, Array.Empty<long>());
            foreach (var biasNeuronId in neuronIds)
            {
                AddBias(biasNeuronId);
            }
        }

        public override void LayerControl_OnLoaded()
        {
            RefreshContent();
        }

        public void RefreshContent()
        {
            CtlContent.Content = null;
            CtlContent.Content = CtlNeurons;
        }

        private void LayerParameter_OnChanged(Notification.ParameterChanged _)
        {
            OnChanged(Notification.ParameterChanged.Structure);
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
            CtlNeurons.Items.Clear();
            Neurons.Clear();

            if (taskFunction != null)
            {
                Range.For(taskFunction.VisualControl.GetInputCount(), _ => Neurons.Insert(0, AddNeuron()));
            }
        }

        public new InputNeuronControl AddNeuron()
        {
            InputNeuronControl neuron = new(Neurons.Count, this)
            {
                ActivationFunction = ActivationFunction.GetInstance(CtlActivationFunction),
                ActivationFunctionParam = CtlActivationFunctionParam.Value
            };

            return neuron;
        }

        public override void AddNeuron(long biasNeuronid)
        {
            AddBias(biasNeuronid);
        }

        public void AddBias(long biasNeuronId)
        {
            InputBiasControl neuron = new(biasNeuronId, _config, _onChanged, this);

            Neurons.Add(neuron);
            CtlNeurons.Items.Add(neuron);

            RefreshContent();

            if (biasNeuronId == Constants.UnknownId)
            {
                OnChanged(Notification.ParameterChanged.NeuronsCount);
            }

            RefreshNeuronsOrdinalNumbers();
        }

        public override bool CanNeuronBeAdded() => false;
        public override bool CanNeuronBeRemoved() => false;

        public override bool RemoveNeuron(NeuronBaseControl neuron)
        {
            MessageBox.Show("Input neuron cannot be removed.", "Warning", MessageBoxButton.OK);
            return false;
        }

        // IConfigParam

        public override bool IsValid()
        {
            return _configParams.All(cp => cp.IsValid()) && Neurons.All(n => n.IsValid());
        }

        public override void SaveConfig()
        {
            _configParams.ForEach(cp => cp.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            _config.Remove(Constants.Param.Neurons);
            _configParams.ForEach(cp => cp.RemoveFromConfig());

            Neurons.ToList().ForEach(n => n.RemoveFromConfig());
        }

        public override void SetConfig(Config config)
        {
            throw new InvalidOperationException();
        }

        public override void InvalidateValue()
        {
            throw new InvalidOperationException();
        }

        //

        private void MenuAddBias_OnClick(object sender, EventArgs e)
        {
            AddBias(Constants.UnknownId);
        }
    }
}

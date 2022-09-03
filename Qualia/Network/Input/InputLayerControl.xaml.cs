using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Qualia.Controls
{
    sealed public partial class InputLayerControl : LayerBaseControl
    {
        public InputLayerControl(long id,
                                 Config config,
                                 ActionManager.ApplyActionDelegate onChanged)
            : base(id,
                   config,
                   onChanged)
        {
            InitializeComponent();

            this.SetConfigParams(new()
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
            });

            this.GetConfigParams().ForEach(cp => cp.SetConfig(this.GetConfig()));

            LoadConfig();

            this.GetConfigParams().ForEach(cp => cp.SetOnChangeEvent(LayerParameter_OnChanged));
        }

        public override void LoadConfig()
        {
            CtlActivationFunction
                .Fill<ActivationFunction>(this.GetConfig());

            CtlWeightsInitializeFunction
                 .Fill<InitializeFunction>(this.GetConfig());

            this.GetConfigParams().ForEach(cp => cp.LoadConfig());

            var neuronIds = this.GetConfig().Get(Constants.Param.Neurons, Array.Empty<long>());
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

        private void LayerParameter_OnChanged(Notification.ParameterChanged param, ApplyAction action)
        {
            OnChanged(param == Notification.ParameterChanged.Unknown ? Notification.ParameterChanged.Structure : param, action);
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

        public new NeuronBaseControl AddNeuron()
        {
            InputNeuronControl neuron = new(Neurons.Count, this)
            {
                ActivationFunction = ActivationFunction.GetInstance(CtlActivationFunction),
                ActivationFunctionParam = CtlActivationFunctionParam.Value
            };

            return neuron;
        }

        public override NeuronBaseControl AddNeuron(long biasNeuronid)
        {
            return AddBias(biasNeuronid);
        }

        public NeuronBaseControl AddBias(long biasNeuronId)
        {
            InputBiasControl neuron = new(biasNeuronId, this.GetConfig(), this.GetUIHandler(), this);

            Neurons.Add(neuron);
            CtlNeurons.Items.Add(neuron);

            RefreshContent();

            if (biasNeuronId == Constants.UnknownId)
            {
                OnChanged(Notification.ParameterChanged.NeuronsAdded, null);
            }

            RefreshNeuronsOrdinalNumbers();

            return neuron;
        }

        public override bool CanNeuronBeAdded() => false;
        public override bool CanNeuronBeRemoved(NeuronBaseControl neuron) => false;

        public override int RemoveNeuron(NeuronBaseControl neuron)
        {
            MessageBox.Show("Input neuron cannot be removed.", "Warning", MessageBoxButton.OK);
            return 0;
        }

        public override void SetAllNeuronsSelected(bool isSelected)
        {
            Range.ForEach(Neurons, n => n.IsSelected = isSelected);
        }

        // IConfigParam

        public override bool IsValid()
        {
            return this.GetConfigParams().All(cp => cp.IsValid()) && Neurons.All(n => n.IsValid());
        }

        public override void SaveConfig()
        {
            this.GetConfigParams().ForEach(cp => cp.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            this.GetConfig().Remove(Constants.Param.Neurons);
            this.GetConfigParams().ForEach(cp => cp.RemoveFromConfig());

            //Neurons.ToList().ForEach(n => n.RemoveFromConfig()); Input neurons are not in config.
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
    }
}

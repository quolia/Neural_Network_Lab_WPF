﻿using Qualia.Tools;
using System;
using System.Collections.Generic;

namespace Qualia.Controls
{
    sealed public partial class OutputNeuronControl : NeuronBaseControl
    {
        public OutputNeuronControl(long id, Config config, Action<Notification.ParameterChanged> onChanged, LayerBaseControl parentLayer)
            : base(id, config, onChanged, parentLayer)
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlActivationFunction
                    .Initialize(nameof(ActivationFunction.LogisticSigmoid)),

                CtlActivationFunctionParam
                    .Initialize(defaultValue: 1),

                CtlLabel
            };

            _configParams.ForEach(param => param.SetConfig(Config));

            LoadConfig();

            _configParams.ForEach(param => param.SetOnChangeEvent(Neuron_OnChanged));
        }

        public override InitializeFunction WeightsInitializeFunction => InitializeFunction.Skip.Instance;
        public override double WeightsInitializeFunctionParam => 1;
        public override bool IsBias => false;
        public override bool IsBiasConnected => false;
        public override ActivationFunction ActivationFunction
        {
            get => ActivationFunction.GetInstance(CtlActivationFunction);
            set => throw new InvalidOperationException();
        }
        
        public override double ActivationFunctionParam
        {
            get => CtlActivationFunctionParam.Value;
            set => throw new InvalidOperationException();
        }
        
        public override string Label => CtlLabel.Text;

        public override InitializeFunction ActivationInitializeFunction
        {
            get => throw new InvalidOperationException();
        }

        public override double ActivationInitializeFunctionParam => throw new NotImplementedException();

        public override void LoadConfig()
        {
            CtlActivationFunction
                .Fill<ActivationFunction>(Config);

            _configParams.ForEach(param => param.LoadConfig());

            StateChanged();
        }

        private void Neuron_OnChanged(Notification.ParameterChanged _)
        {
            OnChanged(Notification.ParameterChanged.Structure);
        }

        public override void SetOrdinalNumber(int number)
        {
            CtlNumber.Text = Converter.IntToText(number);
        }

        public override bool IsValid()
        {
            return CtlActivationFunctionParam.IsValid();
        }

        public override void SaveConfig()
        {
            _configParams.ForEach(param => param.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            _configParams.ForEach(param => param.RemoveFromConfig());
        }
    }
}
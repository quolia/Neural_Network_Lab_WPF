using Qualia.Tools;
using System;
using System.Collections.Generic;

namespace Qualia.Controls
{
    sealed public partial class OutputNeuronControl : NeuronBaseControl
    {
        private readonly List<IConfigParam> _configParams;

        public OutputNeuronControl(long id, Config config, Action<Notification.ParameterChanged> networkUI_OnChanged)
            : base(id, config, networkUI_OnChanged)
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
        public override ActivationFunction ActivationFunction => ActivationFunction.GetInstance(CtlActivationFunction);
        public override double ActivationFunctionParam => CtlActivationFunctionParam.Value;

        public override string Label => CtlLabel.Text;

        public override void LoadConfig()
        {
            CtlActivationFunction
                .Fill<ActivationFunction>(Config);

            _configParams.ForEach(param => param.LoadConfig());

            StateChanged();
        }

        private void Neuron_OnChanged(Notification.ParameterChanged _)
        {
            NetworkUI_OnChanged(Notification.ParameterChanged.Structure);
        }

        public override void OrdinalNumber_OnChanged(int number)
        {
            CtlNumber.Content = Converter.IntToText(number);
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

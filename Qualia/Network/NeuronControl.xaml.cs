using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Qualia.Controls
{
    public partial class NeuronControl : NeuronBaseControl
    {
        private readonly List<IConfigParam> _configParams;

        public NeuronControl(long id, Config config, Action<Notification.ParameterChanged> networkUI_OnChanged)
            : base(id, config, networkUI_OnChanged)
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlActivationFunction.Initialize(nameof(ActivationFunction.LogisticSigmoid)),
                CtlActivationFunctionParam.Initialize(defaultValue: 1),

                CtlActivationInitializeFunction.Initialize(nameof(InitializeFunction.Constant)),
                CtlActivationInitializeFunctionParam.Initialize(defaultValue: 1),

                CtlWeightsInitializeFunction.Initialize(nameof(InitializeFunction.Skip)),
                CtlWeightsInitializeFunctionParam.Initialize(defaultValue: 1),

                CtlIsBias.Initialize(false),
                CtlIsBiasConnected.Initialize(false)
            };

            _configParams.ForEach(param => param.SetConfig(Config));
            LoadConfig();

            _configParams.ForEach(param => param.AddChangeEventListener(Neuron_OnChanged));
        }

        private void Neuron_OnChanged()
        {
            NetworkUI_OnChanged(Notification.ParameterChanged.Structure);
        }

        public override void OrdinalNumber_OnChanged(int number)
        {
            CtlNumber.Content = Converter.IntToText(number);
        }

        private void IsBias_OnCheckedChanged()
        {
            CtlIsBiasConnected.Visibility = CtlIsBias.Value ? Visibility.Visible : Visibility.Collapsed;
            CtlActivation.Height = CtlIsBias.Value ? new(0, GridUnitType.Auto) : new(0, GridUnitType.Pixel);

            StateChanged();
            Neuron_OnChanged();
        }

        public override InitializeFunction ActivationInitializeFunction => (CtlIsBias.IsChecked == true ? InitializeFunction.GetInstance(CtlActivationInitializeFunction) : null);
        public override double ActivationInitializeFunctionParam => (CtlIsBias.IsChecked == true ? CtlActivationInitializeFunctionParam.Value : 1);
        public override InitializeFunction WeightsInitializeFunction => InitializeFunction.GetInstance(CtlWeightsInitializeFunction);
        public override double WeightsInitializeFunctionParam => CtlWeightsInitializeFunctionParam.Value;
        public override bool IsBias => CtlIsBias.IsChecked == true;
        public override bool IsBiasConnected => CtlIsBiasConnected.IsChecked == true && IsBias;
        public override ActivationFunction ActivationFunction => ActivationFunction.GetInstance(CtlActivationFunction);
        public override double ActivationFunctionParam => CtlActivationFunctionParam.Value;

        public void LoadConfig()
        {
            CtlWeightsInitializeFunction.Fill<InitializeFunction>(Config);
            CtlActivationInitializeFunction.Fill<InitializeFunction>(Config);
            CtlActivationFunction.Fill<ActivationFunction>(Config);

            _configParams.ForEach(param => param.LoadConfig());

            CtlIsBiasConnected.Visibility = CtlIsBias.Value ? Visibility.Visible : Visibility.Collapsed;
            CtlIsBiasConnected.Value &= CtlIsBias.Value;
            CtlActivation.Height = CtlIsBias.Value ? new(0, GridUnitType.Auto) : new(0, GridUnitType.Pixel);

            StateChanged();
        }

        public override bool IsValid()
        {
            return CtlWeightsInitializeFunctionParam.IsValid() && (!IsBias || CtlActivationInitializeFunctionParam.IsValid());
        }

        public override void SaveConfig()
        {
            _configParams.ForEach(param => param.SaveConfig());

            if (!CtlIsBias.Value)
            {
                CtlActivationInitializeFunction.RemoveFromConfig();
                CtlActivationInitializeFunctionParam.RemoveFromConfig();
            }
        }

        public override void RemoveFromConfig()
        {
            _configParams.ForEach(param => param.RemoveFromConfig());
        }
    }
}

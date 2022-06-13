using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Qualia.Controls
{
    public partial class NeuronControl : NeuronBase
    {
        private readonly List<IConfigParam> _configParams;

        public NeuronControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlActivationInitializeFunctionParam,
                CtlActivationInitializeFunction,
                CtlWeightsInitializeFunctionParam,
                CtlWeightsInitializeFunction,
                CtlIsBias,
                CtlIsBiasConnected,
                CtlActivationFunction,
                CtlActivationFunctionParam
            };

            _configParams.ForEach(param => param.SetConfig(Config));
            LoadConfig();

            _configParams.ForEach(param => param.SetChangeEvent(OnChanged));
        }

        private void OnChanged()
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        public override void OrdinalNumberChanged(int number)
        {
            CtlNumber.Content = number.ToString();
        }

        private void CtlIsBias_CheckedChanged()
        {
            CtlIsBiasConnected.Visibility = CtlIsBias.IsOn ? Visibility.Visible : Visibility.Collapsed;
            CtlActivation.Height = CtlIsBias.IsOn ? new(0, GridUnitType.Auto) : new(0, GridUnitType.Pixel);

            StateChanged();
            OnChanged();
        }

        public override InitializeFunction ActivationInitializeFunction => (CtlIsBias.IsChecked == true ? InitializeFunction.GetInstance(CtlActivationInitializeFunction.SelectedItem) : null);
        public override double? ActivationInitializeFunctionParam => (CtlIsBias.IsChecked == true ? CtlActivationInitializeFunctionParam.ValueOrNull : null);
        public override InitializeFunction WeightsInitializeFunction => InitializeFunction.GetInstance(CtlWeightsInitializeFunction.SelectedItem);
        public override double? WeightsInitializeFunctionParam => CtlWeightsInitializeFunctionParam.ValueOrNull;
        public override bool IsBias => CtlIsBias.IsChecked == true;
        public override bool IsBiasConnected => CtlIsBiasConnected.IsChecked == true && IsBias;
        public override ActivationFunction ActivationFunction => ActivationFunction.GetInstance(CtlActivationFunction.SelectedItem);
        public override double? ActivationFunctionParam => CtlActivationFunctionParam.ValueOrNull;

        public void LoadConfig()
        {
            Initializer.FillComboBox<InitializeFunction>(CtlWeightsInitializeFunction, Config, nameof(InitializeFunction.None));
            Initializer.FillComboBox<InitializeFunction>(CtlActivationInitializeFunction, Config, nameof(InitializeFunction.Constant));
            Initializer.FillComboBox<ActivationFunction>(CtlActivationFunction, Config, nameof(ActivationFunction.LogisticSigmoid));

            _configParams.ForEach(param => param.LoadConfig());

            CtlIsBiasConnected.Visibility = CtlIsBias.IsOn ? Visibility.Visible : Visibility.Collapsed;
            CtlIsBiasConnected.IsOn &= CtlIsBias.IsOn;
            CtlActivation.Height = CtlIsBias.IsOn ? new(0, GridUnitType.Auto) : new(0, GridUnitType.Pixel);

            StateChanged();
        }

        public bool IsValidActivationIniterParam()
        {
            return !IsBias || Converter.TryTextToDouble(CtlActivationInitializeFunctionParam.Text, out _);
        }

        public override bool IsValid()
        {
            return CtlWeightsInitializeFunctionParam.IsValid() && (!IsBias || CtlActivationInitializeFunctionParam.IsValid());
        }

        public override void SaveConfig()
        {
            _configParams.ForEach(param => param.SaveConfig());

            if (!CtlIsBias.IsOn)
            {
                CtlActivationInitializeFunction.VanishConfig();
                CtlActivationInitializeFunctionParam.VanishConfig();
            }
        }

        public override void VanishConfig()
        {
            _configParams.ForEach(param => param.VanishConfig());
        }
    }
}

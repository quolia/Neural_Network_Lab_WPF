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

        public override InitializeFunction ActivationInitializeFunction => (CtlIsBias.IsChecked == true ? InitializeFunctionList.GetInstance(CtlActivationInitializeFunction.SelectedItem.ToString()) : null);
        public override double? ActivationInitializeFunctionParam => (CtlIsBias.IsChecked == true ? CtlActivationInitializeFunctionParam.ValueOrNull : null);
        public override InitializeFunction WeightsInitializeFunction => InitializeFunctionList.GetInstance(CtlWeightsInitializeFunction.SelectedItem.ToString());
        public override double? WeightsInitializeFunctionParam => CtlWeightsInitializeFunctionParam.ValueOrNull;
        public override bool IsBias => CtlIsBias.IsChecked == true;
        public override bool IsBiasConnected => CtlIsBiasConnected.IsChecked == true && IsBias;
        public override ActivationFunction ActivationFunction => ActivationFunctionList.GetInstance(CtlActivationFunction.SelectedItem.ToString());
        public override double? ActivationFunctionParam => CtlActivationFunctionParam.ValueOrNull;

        public void LoadConfig()
        {
            Initializer.FillComboBox(InitializeFunctionList.GetItems, CtlWeightsInitializeFunction, Config, nameof(InitializeFunctionList.None));
            Initializer.FillComboBox(InitializeFunctionList.GetItems, CtlActivationInitializeFunction, Config, nameof(InitializeFunctionList.Constant));
            Initializer.FillComboBox(ActivationFunctionList.GetItems, CtlActivationFunction, Config, nameof(ActivationFunctionList.LogisticSigmoid));

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

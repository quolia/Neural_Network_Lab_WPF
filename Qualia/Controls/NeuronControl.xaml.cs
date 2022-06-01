using System;
using System.Collections.Generic;
using System.Windows;
using Tools;

namespace Qualia.Controls
{
    public partial class NeuronControl : NeuronBase
    {
        private readonly List<IConfigValue> _configParams;

        public NeuronControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            _configParams = new List<IConfigValue>()
            {
                CtlActivationInitializerParamA,
                CtlActivationInitializer,
                CtlWeightsInitializerParamA,
                CtlWeightsInitializer,
                CtlIsBias,
                CtlIsBiasConnected,
                CtlActivationFunc,
                CtlActivationFuncParamA
            };

            _configParams.ForEach(p => p.SetConfig(Config));
            LoadConfig();
            _configParams.ForEach(p => p.SetChangeEvent(OnChanged));
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
            CtlActivation.Height = CtlIsBias.IsOn ? new GridLength(0, GridUnitType.Auto) : new GridLength(0, GridUnitType.Pixel);

            StateChanged();
            OnChanged();
        }

        public override string ActivationInitializer => (CtlIsBias.IsChecked == true ? CtlActivationInitializer.SelectedItem.ToString() : null);
        public override double? ActivationInitializerParamA => (CtlIsBias.IsChecked == true ? CtlActivationInitializerParamA.ValueOrNull : null);
        public override string WeightsInitializer => CtlWeightsInitializer.SelectedItem.ToString();
        public override double? WeightsInitializerParamA => CtlWeightsInitializerParamA.ValueOrNull;
        public override bool IsBias => CtlIsBias.IsChecked == true;
        public override bool IsBiasConnected => CtlIsBiasConnected.IsChecked == true && IsBias;
        public override string ActivationFunc => CtlActivationFunc.SelectedItem.ToString();
        public override double? ActivationFuncParamA => CtlActivationFuncParamA.ValueOrNull;

        public void LoadConfig()
        {
            InitializeMode.Helper.FillComboBox(CtlWeightsInitializer, Config, nameof(InitializeMode.None));
            InitializeMode.Helper.FillComboBox(CtlActivationInitializer, Config, nameof(InitializeMode.Constant));
            ActivationFunction.Helper.FillComboBox(CtlActivationFunc, Config, nameof(ActivationFunction.LogisticSigmoid));

            _configParams.ForEach(p => p.LoadConfig());

            CtlIsBiasConnected.Visibility = CtlIsBias.IsOn ? Visibility.Visible : Visibility.Collapsed;
            CtlIsBiasConnected.IsOn &= CtlIsBias.IsOn;
            CtlActivation.Height = CtlIsBias.IsOn ? new GridLength(0, GridUnitType.Auto) : new GridLength(0, GridUnitType.Pixel);

            StateChanged();
        }

        public bool IsValidActivationIniterParamA()
        {
            return !IsBias || Converter.TryTextToDouble(CtlActivationInitializerParamA.Text, out _);
        }

        public override bool IsValid()
        {
            return CtlWeightsInitializerParamA.IsValid() && (!IsBias || CtlActivationInitializerParamA.IsValid());
        }

        public override void SaveConfig()
        {
            _configParams.ForEach(p => p.SaveConfig());

            if (!CtlIsBias.IsOn)
            {
                CtlActivationInitializer.VanishConfig();
                CtlActivationInitializerParamA.VanishConfig();
            }
        }

        public override void VanishConfig()
        {
            _configParams.ForEach(p => p.VanishConfig());
        }
    }
}

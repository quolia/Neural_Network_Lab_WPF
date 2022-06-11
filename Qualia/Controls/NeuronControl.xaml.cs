using System;
using System.Collections.Generic;
using System.Windows;
using Tools;

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
                CtlActivationInitializerParam,
                CtlActivationInitializer,
                CtlWeightsInitializerParam,
                CtlWeightsInitializer,
                CtlIsBias,
                CtlIsBiasConnected,
                CtlActivationFunc,
                CtlActivationFuncParam
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

        public override InitializeMode ActivationInitializer => (CtlIsBias.IsChecked == true ? InitializeModeList.Helper.GetInstance(CtlActivationInitializer.SelectedItem.ToString()) : null);
        public override double? ActivationInitializerParam => (CtlIsBias.IsChecked == true ? CtlActivationInitializerParam.ValueOrNull : null);
        public override InitializeMode WeightsInitializer => InitializeModeList.Helper.GetInstance(CtlWeightsInitializer.SelectedItem.ToString());
        public override double? WeightsInitializerParam => CtlWeightsInitializerParam.ValueOrNull;
        public override bool IsBias => CtlIsBias.IsChecked == true;
        public override bool IsBiasConnected => CtlIsBiasConnected.IsChecked == true && IsBias;
        public override string ActivationFunc => CtlActivationFunc.SelectedItem.ToString();
        public override double? ActivationFuncParam => CtlActivationFuncParam.ValueOrNull;

        public void LoadConfig()
        {
            InitializeModeList.Helper.FillComboBox(CtlWeightsInitializer, Config, nameof(InitializeModeList.None));
            InitializeModeList.Helper.FillComboBox(CtlActivationInitializer, Config, nameof(InitializeModeList.Constant));
            ActivationFunctionList.Helper.FillComboBox(CtlActivationFunc, Config, nameof(ActivationFunctionList.LogisticSigmoid));

            _configParams.ForEach(param => param.LoadConfig());

            CtlIsBiasConnected.Visibility = CtlIsBias.IsOn ? Visibility.Visible : Visibility.Collapsed;
            CtlIsBiasConnected.IsOn &= CtlIsBias.IsOn;
            CtlActivation.Height = CtlIsBias.IsOn ? new(0, GridUnitType.Auto) : new(0, GridUnitType.Pixel);

            StateChanged();
        }

        public bool IsValidActivationIniterParam()
        {
            return !IsBias || Converter.TryTextToDouble(CtlActivationInitializerParam.Text, out _);
        }

        public override bool IsValid()
        {
            return CtlWeightsInitializerParam.IsValid() && (!IsBias || CtlActivationInitializerParam.IsValid());
        }

        public override void SaveConfig()
        {
            _configParams.ForEach(param => param.SaveConfig());

            if (!CtlIsBias.IsOn)
            {
                CtlActivationInitializer.VanishConfig();
                CtlActivationInitializerParam.VanishConfig();
            }
        }

        public override void VanishConfig()
        {
            _configParams.ForEach(param => param.VanishConfig());
        }
    }
}

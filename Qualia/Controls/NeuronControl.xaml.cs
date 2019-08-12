using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tools;

namespace Qualia.Controls
{
    public partial class NeuronControl : NeuronBase
    {
        public NeuronControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            LoadConfig();

            CtlActivationInitializerParamA.SetChangeEvent(OnChanged);
            CtlActivationInitializer.SetChangeEvent(OnChanged);
            CtlWeightsInitializerParamA.SetChangeEvent(OnChanged);
            CtlWeightsInitializer.SetChangeEvent(OnChanged);
            CtlIsBias.SetChangeEvent(CtlIsBias_CheckedChanged);
            CtlIsBiasConnected.SetChangeEvent(OnChanged);
            CtlActivationFunc.SetChangeEvent(OnChanged);
            CtlActivationFuncParamA.SetChangeEvent(OnChanged);
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
            CtlIsBias.Load(Config);
            CtlIsBiasConnected.Load(Config);
            CtlIsBiasConnected.Visibility = CtlIsBias.IsOn ? Visibility.Visible : Visibility.Collapsed;
            CtlIsBiasConnected.IsOn &= CtlIsBias.IsOn;
            CtlActivation.Height = CtlIsBias.IsOn ? new GridLength(0, GridUnitType.Auto) : new GridLength(0, GridUnitType.Pixel);

            InitializeMode.Helper.FillComboBox(CtlWeightsInitializer, Config, nameof(InitializeMode.None));
            CtlWeightsInitializerParamA.Load(Config);

            InitializeMode.Helper.FillComboBox(CtlActivationInitializer, Config, nameof(InitializeMode.Constant));
            CtlActivationInitializerParamA.Load(Config);

            ActivationFunction.Helper.FillComboBox(CtlActivationFunc, Config, nameof(ActivationFunction.LogisticSigmoid));
            CtlActivationFuncParamA.Load(Config);

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
            if (CtlIsBias.IsOn)
            {
                CtlActivationInitializer.Save(Config);
                CtlActivationInitializerParamA.Save(Config);
            }
            else
            {
                CtlActivationInitializer.Vanish(Config);
                CtlActivationInitializerParamA.Vanish(Config);
            }

            CtlWeightsInitializer.Save(Config);
            CtlWeightsInitializerParamA.Save(Config);

            CtlActivationFunc.Save(Config);
            CtlActivationFuncParamA.Save(Config);

            CtlIsBias.Save(Config);
            CtlIsBiasConnected.Save(Config);
        }

        public override void VanishConfig()
        {
            CtlActivationInitializer.Vanish(Config);
            CtlWeightsInitializer.Vanish(Config);
            CtlActivationFunc.Vanish(Config);
            CtlWeightsInitializerParamA.Vanish(Config);
            CtlActivationInitializerParamA.Vanish(Config);
            CtlActivationFuncParamA.Vanish(Config);
            CtlIsBias.Vanish(Config);
            CtlIsBiasConnected.Vanish(Config);
        }
    }
}

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
        public NeuronControl(long id, Config config, Action<Notification.ParameterChanged, object> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            LoadConfig();

            CtlActivationIniterParamA.Changed += OnChanged;
            CtlActivationIniter.SelectedIndexChanged += CtlActivationIniter_SelectionChanged; 
            CtlWeightsIniterParamA.Changed += OnChanged;
            CtlWeightsIniter.SelectionChanged += CtlWeightsIniter_SelectionChanged;
            CtlIsBias.Changed += CtlIsBias_CheckedChanged;
            CtlIsBiasConnected.Changed += CtlIsBiasConnected_CheckedChanged;
            CtlActivationFunc.SelectedIndexChanged += CtlActivationFunc_SelectedIndexChanged;
            CtlActivationFuncParamA.Changed += OnChanged;
        }

        private void CtlWeightsIniter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure, false);
        }

        private void CtlActivationIniter_SelectionChanged(int index)
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure, false);
        }

        private void CtlActivationFunc_SelectedIndexChanged(int index)
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure, false);
        }

        private void OnChanged()
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure, null);
        }

        public override void OrdinalNumberChanged(int number)
        {
            CtlNumber.Content = number.ToString();
        }

        private void CtlIsBiasConnected_CheckedChanged()
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure, false);
        }

        private void CtlIsBias_CheckedChanged()
        {
            CtlIsBiasConnected.Visibility = CtlIsBias.IsOn ? Visibility.Visible : Visibility.Collapsed;
            CtlActivation.Height = CtlIsBias.IsOn ? new GridLength(0, GridUnitType.Auto) : new GridLength(0, GridUnitType.Pixel);
            StateChanged();
            OnNetworkUIChanged(Notification.ParameterChanged.Structure, false);
        }

        public override string ActivationInitializer => (CtlIsBias.IsChecked == true ? CtlActivationIniter.SelectedItem.ToString() : null);
        public override double? ActivationInitializerParamA => (CtlIsBias.IsChecked == true ? CtlActivationIniterParamA.ValueOrNull : null);
        public override string WeightsInitializer => CtlWeightsIniter.SelectedItem.ToString();
        public override double? WeightsInitializerParamA => CtlWeightsIniterParamA.ValueOrNull;
        public override bool IsBias => CtlIsBias.IsChecked == true;
        public override bool IsBiasConnected => CtlIsBiasConnected.IsChecked == true && IsBias;
        public override string ActivationFunc => CtlActivationFunc.SelectedItem.ToString();
        public override double? ActivationFuncParamA => CtlActivationFuncParamA.ValueOrNull;

        public void LoadConfig()
        {
            InitializeMode.Helper.FillComboBox(CtlWeightsIniter, Config, Const.Param.WeightsInitializer, nameof(InitializeMode.None));
            CtlWeightsIniterParamA.Load(Config);

            CtlIsBias.Load(Config);
            CtlIsBiasConnected.Load(Config);
            CtlIsBiasConnected.Visibility = CtlIsBias.IsOn ? Visibility.Visible : Visibility.Collapsed;
            CtlIsBiasConnected.IsOn &= CtlIsBias.IsOn;
            CtlActivation.Height = CtlIsBias.IsOn ? new GridLength(0, GridUnitType.Auto) : new GridLength(0, GridUnitType.Pixel);

            InitializeMode.Helper.FillComboBox(CtlActivationIniter, Config, Const.Param.ActivationInitializer, nameof(InitializeMode.Constant));
            CtlActivationIniterParamA.Load(Config);

            ActivationFunction.Helper.FillComboBox(CtlActivationFunc, Config, Const.Param.ActivationFunc, nameof(ActivationFunction.LogisticSigmoid));
            CtlActivationFuncParamA.Load(Config);

            StateChanged();
        }

        public bool IsValidActivationIniterParamA()
        {
            return !IsBias || Converter.TryTextToDouble(CtlActivationIniterParamA.Text, out _);
        }

        public override bool IsValid()
        {
            return CtlWeightsIniterParamA.IsValid() && (!IsBias || CtlActivationIniterParamA.IsValid());
        }

        public override void SaveConfig()
        {
            if (CtlIsBias.IsOn)
            {
                Config.Set(Const.Param.ActivationInitializer, CtlActivationIniter.SelectedItem.ToString());
                CtlActivationIniterParamA.Save(Config);
            }
            else
            {
                Config.Remove(Const.Param.ActivationInitializer);
                CtlActivationIniterParamA.Vanish(Config);
            }

            Config.Set(Const.Param.WeightsInitializer, CtlWeightsIniter.SelectedItem.ToString());
            CtlWeightsIniterParamA.Save(Config);

            Config.Set(Const.Param.ActivationFunc, CtlActivationFunc.SelectedItem.ToString());
            CtlActivationFuncParamA.Save(Config);

            CtlIsBias.Save(Config);
            CtlIsBiasConnected.Save(Config);
        }

        public override void VanishConfig()
        {
            Config.Remove(Const.Param.ActivationInitializer);
            Config.Remove(Const.Param.WeightsInitializer);
            Config.Remove(Const.Param.ActivationFunc);

            CtlWeightsIniterParamA.Vanish(Config);
            CtlActivationIniterParamA.Vanish(Config);
            CtlActivationFuncParamA.Vanish(Config);
            CtlIsBias.Vanish(Config);
            CtlIsBiasConnected.Vanish(Config);
        }
    }
}

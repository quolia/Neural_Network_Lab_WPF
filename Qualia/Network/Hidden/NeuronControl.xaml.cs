using Qualia.Tools;
using System;
using System.Windows;

namespace Qualia.Controls
{
    public partial class NeuronControl : NeuronBaseControl
    {
        public NeuronControl(long id, Config config, Action<Notification.ParameterChanged> onChanged, LayerBaseControl parentLayer)
            : base(id, config, onChanged, parentLayer)
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlActivationFunction
                    .Initialize(nameof(ActivationFunction.LogisticSigmoid)),
                    //.SetToolTip(SelectableItemsProvider.GetSelectableItem(null)),

                CtlActivationFunctionParam
                    .Initialize(defaultValue: 1),

                CtlActivationInitializeFunction
                    .Initialize(nameof(InitializeFunction.Constant)),

                CtlActivationInitializeFunctionParam
                    .Initialize(defaultValue: 1),

                CtlWeightsInitializeFunction
                    .Initialize(nameof(InitializeFunction.Skip)),

                CtlWeightsInitializeFunctionParam
                    .Initialize(defaultValue: 1),

                CtlIsBias
                    .Initialize(false),

                CtlIsBiasConnected
                    .Initialize(false)
            };

            _configParams.ForEach(param => param.SetConfig(Config));
            LoadConfig();

            _configParams.ForEach(param => param.SetOnChangeEvent(Neuron_OnChanged));
        }

        private void ActivationFunction_OnToolTipOpening(object sender, System.Windows.Controls.ToolTipEventArgs e)
        {
            //(sender as Control).ToolTip = ToolTipsProvider.GetFunctionToolTip();
        }

        private void Neuron_OnChanged(Notification.ParameterChanged _)
        {
            NetworkUI_OnChanged(Notification.ParameterChanged.Structure);
        }

        public override void OrdinalNumber_OnChanged(int number)
        {
            CtlNumber.Text = Converter.IntToText(number);
        }

        private void IsBias_OnCheckedChanged()
        {
            CtlIsBiasConnected.Visibility = CtlIsBias.Value ? Visibility.Visible : Visibility.Collapsed;
            //CtlActivation.Height = CtlIsBias.Value ? new(0, GridUnitType.Auto) : new(0, GridUnitType.Pixel);

            StateChanged();
            Neuron_OnChanged(Notification.ParameterChanged.Unknown);
        }

        public override InitializeFunction ActivationInitializeFunction => (CtlIsBias.IsChecked == true ? InitializeFunction.GetInstance(CtlActivationInitializeFunction) : null);
        public override double ActivationInitializeFunctionParam => (CtlIsBias.IsChecked == true ? CtlActivationInitializeFunctionParam.Value : 1);
        public override InitializeFunction WeightsInitializeFunction => InitializeFunction.GetInstance(CtlWeightsInitializeFunction);
        public override double WeightsInitializeFunctionParam => CtlWeightsInitializeFunctionParam.Value;
        public override bool IsBias => CtlIsBias.IsChecked == true;
        public override bool IsBiasConnected => CtlIsBiasConnected.IsChecked == true && IsBias;
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

        public override string Label => null;

        public override void LoadConfig()
        {
            CtlWeightsInitializeFunction
                .Fill<InitializeFunction>(Config);

            CtlActivationInitializeFunction
                .Fill<InitializeFunction>(Config);

            CtlActivationFunction
                .Fill<ActivationFunction>(Config);

            _configParams.ForEach(param => param.LoadConfig());

            CtlIsBiasConnected.Visibility = CtlIsBias.Value ? Visibility.Visible : Visibility.Collapsed;
            CtlIsBiasConnected.Value &= CtlIsBias.Value;
            //CtlActivation.Height = CtlIsBias.Value ? new(0, GridUnitType.Auto) : new(0, GridUnitType.Pixel);

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

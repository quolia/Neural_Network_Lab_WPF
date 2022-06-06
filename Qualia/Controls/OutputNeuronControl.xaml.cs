using System;
using System.Collections.Generic;
using Tools;

namespace Qualia.Controls
{
    public partial class OutputNeuronControl : NeuronBase
    {
        private readonly List<IConfigParam> _configParams;

        public OutputNeuronControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            _configParams = new List<IConfigParam>()
            {
                CtlActivationFunc,
                CtlActivationFuncParamA
            };

            _configParams.ForEach(param => param.SetConfig(Config));
            LoadConfig();

            _configParams.ForEach(param => param.SetChangeEvent(OnChanged));
        }

        public override string WeightsInitializer => nameof(InitializeMode.None);
        public override double? WeightsInitializerParamA => null;
        public override bool IsBias => false;
        public override bool IsBiasConnected => false;
        public override string ActivationFunc => CtlActivationFunc.SelectedItem.ToString();
        public override double? ActivationFuncParamA => CtlActivationFuncParamA.ValueOrNull;

        public void LoadConfig()
        {
            ActivationFunction.Helper.FillComboBox(CtlActivationFunc, Config, nameof(ActivationFunction.LogisticSigmoid));
            _configParams.ForEach(param => param.LoadConfig());

            StateChanged();
        }

        private void OnChanged()
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        public override void OrdinalNumberChanged(int number)
        {
            CtlNumber.Content = number.ToString();
        }

        public override bool IsValid()
        {
            return CtlActivationFuncParamA.IsValid();
        }

        public override void SaveConfig()
        {
            _configParams.ForEach(param => param.SaveConfig());
        }

        public override void VanishConfig()
        {
            _configParams.ForEach(param => param.VanishConfig());
        }
    }
}

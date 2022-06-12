using Qualia.Tools;
using System;
using System.Collections.Generic;

namespace Qualia.Controls
{
    sealed public partial class OutputNeuronControl : NeuronBase
    {
        private readonly List<IConfigParam> _configParams;

        public OutputNeuronControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlActivationFunc,
                CtlActivationFuncParam
            };

            _configParams.ForEach(param => param.SetConfig(Config));
            LoadConfig();

            _configParams.ForEach(param => param.SetChangeEvent(OnChanged));
        }

        public override InitializeFunction WeightsInitializer => InitializeFunctionList.None.Instance;
        public override double? WeightsInitializerParam => null;
        public override bool IsBias => false;
        public override bool IsBiasConnected => false;
        public override string ActivationFunc => CtlActivationFunc.SelectedItem.ToString();
        public override double? ActivationFuncParam => CtlActivationFuncParam.ValueOrNull;

        public void LoadConfig()
        {
            ActivationFunctionList.Helper.FillComboBox(CtlActivationFunc, Config, nameof(ActivationFunctionList.LogisticSigmoid));
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
            return CtlActivationFuncParam.IsValid();
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

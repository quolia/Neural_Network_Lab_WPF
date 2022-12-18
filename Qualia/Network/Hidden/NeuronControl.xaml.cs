using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Qualia.Controls
{
    public partial class NeuronControl : NeuronBaseControl
    {
        public NeuronControl(long id,
                             Config config,
                             ActionManager.ApplyActionDelegate onChanged,
                             LayerBaseControl parentLayer)
            : base(id,
                   config,
                   onChanged,
                   parentLayer)
        {
            InitializeComponent();

            this.SetConfigParams(new() 
            {
                CtlActivationFunction
                    .Initialize(nameof(ActivationFunction.LogisticSigmoid)),

                CtlActivationFunctionParam
                    .Initialize(defaultValue: 1),

                CtlWeightsInitializeFunction
                    .Initialize(nameof(InitializeFunction.Skip)),

                CtlWeightsInitializeFunctionParam
                    .Initialize(defaultValue: 1)
            });

            this.GetConfigParams().ForEach(param => param.SetConfig(Config));
            LoadConfig();

            this.GetConfigParams().ForEach(param => param.SetOnChangeEvent(Neuron_OnChanged));
        }

        private void ActivationFunction_OnToolTipOpening(object sender, System.Windows.Controls.ToolTipEventArgs e)
        {
            //(sender as Control).ToolTip = ToolTipsProvider.GetFunctionToolTip();
        }

        private void Neuron_OnChanged(ApplyAction action)
        {
            action.Param = Notification.ParameterChanged.NeuronParam;
            OnChanged(action);
        }

        public override void SetOrdinalNumber(int number)
        {
            CtlNumber.Text = Converter.IntToText(number);
        }

        public override ActivationFunction ActivationFunction
        {
            get => ActivationFunction.GetInstance(CtlActivationFunction);
            set
            {
                CtlActivationFunction.SelectByText(ActivationFunction.GetNameByInstance(value));
            }
        }
        public override double ActivationFunctionParam
        {
            get => CtlActivationFunctionParam.Value;
            set => CtlActivationFunctionParam.Value = value;
        }
        public override InitializeFunction WeightsInitializeFunction
        {
            get => InitializeFunction.GetInstance(CtlWeightsInitializeFunction);
            set
            {
                CtlWeightsInitializeFunction.SelectByText(WeightsInitializeFunction.GetNameByInstance(value));
            }
        }
        public override double WeightsInitializeFunctionParam
        {
            get => CtlWeightsInitializeFunctionParam.Value;
            set => CtlWeightsInitializeFunctionParam.Value = value;
        }

        public override string Label => null;

        public override double PositiveTargetValue { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }
        public override double NegativeTargetValue { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }

        // IConfigParam

        public override void LoadConfig()
        {
            CtlWeightsInitializeFunction
                .Fill<InitializeFunction>(Config);

            CtlActivationFunction
                .Fill<ActivationFunction>(Config);

            this.GetConfigParams().ForEach(param => param.LoadConfig());

            //CtlActivation.Height = CtlIsBias.Value ? new(0, GridUnitType.Auto) : new(0, GridUnitType.Pixel);
        }

        public override bool IsValid()
        {
            return CtlWeightsInitializeFunctionParam.IsValid();
        }

        public override void SaveConfig()
        {
            this.GetConfigParams().ForEach(param => param.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            this.GetConfigParams().ForEach(param => param.RemoveFromConfig());
        }

        //
    }
}

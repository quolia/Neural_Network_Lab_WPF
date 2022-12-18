using Qualia.Tools;
using System;
using System.Windows;

namespace Qualia.Controls
{
    sealed public partial class InputNeuronControl : NeuronBaseControl
    {
        public InputNeuronControl(long id, LayerBaseControl parentLayer)
            : base(id, null, null, parentLayer)
        {
            Visibility = Visibility.Collapsed; // Do not show input neuron.
        }

        public override InitializeFunction WeightsInitializeFunction
        {
            get => InitializeFunction.Skip.Instance;
            set { }
        }
        public override double WeightsInitializeFunctionParam
        {
            get => 1;
            set {}
        }
        public override ActivationFunction ActivationFunction { get; set; }
        public override double ActivationFunctionParam { get; set; }

        public override string Label => null;

        public override double PositiveTargetValue { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }
        public override double NegativeTargetValue { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }

        public override void SetOrdinalNumber(int number)
        {
            throw new InvalidOperationException();
        }

        // IConfigParam

        public override bool IsValid() => true;

        public override void SaveConfig()
        {
            throw new InvalidOperationException();
        }

        public override void RemoveFromConfig()
        {
            throw new InvalidOperationException();
        }

        //
    }
}

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

        public override InitializeFunction WeightsInitializeFunction => InitializeFunction.Skip.Instance;
        public override double WeightsInitializeFunctionParam => 1;
        public override bool IsBias => false;
        public override bool IsBiasConnected => false;
        public override ActivationFunction ActivationFunction { get; set; }
        public override double ActivationFunctionParam { get; set; }

        public override InitializeFunction ActivationInitializeFunction => throw new InvalidOperationException();

        public override double ActivationInitializeFunctionParam => throw new InvalidOperationException();

        public override string Label => null;

        public override void OrdinalNumber_OnChanged(int number)
        {
            throw new InvalidOperationException();
        }

        public override bool IsValid() => true;

        public override void SaveConfig()
        {
            throw new InvalidOperationException();
        }

        public override void RemoveFromConfig()
        {
            throw new InvalidOperationException();
        }
    }
}

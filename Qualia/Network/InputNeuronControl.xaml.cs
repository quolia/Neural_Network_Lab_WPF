using Qualia.Tools;
using System.Windows;

namespace Qualia.Controls
{
    sealed public partial class InputNeuronControl : NeuronBaseControl
    {
        public InputNeuronControl(long id)
            : base(id, null, null)
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed; // do not show it
        }

        public override InitializeFunction WeightsInitializeFunction => InitializeFunction.Skip.Instance;
        public override double WeightsInitializeFunctionParam => 1;
        public override bool IsBias => false;
        public override bool IsBiasConnected => false;
        public override ActivationFunction ActivationFunction { get; set; }
        public override double ActivationFunctionParam { get; set; }

        public override void OrdinalNumber_OnChanged(int number)
        {
            //
        }

        public override bool IsValid() => true;

        public override void SaveConfig()
        {
            //
        }

        public override void RemoveFromConfig()
        {
            //
        }
    }
}

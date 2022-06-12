using Qualia.Tools;
using System.Windows;

namespace Qualia.Controls
{
    sealed public partial class InputNeuronControl : NeuronBase
    {
        public InputNeuronControl(long id)
            : base(id, null, null)
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed; // do not show it
        }

        public override InitializeMode WeightsInitializer => InitializeModeList.None.Instance;
        public override double? WeightsInitializerParam => null;
        public override bool IsBias => false;
        public override bool IsBiasConnected => false;
        public override string ActivationFunc { get; set; }
        public override double? ActivationFuncParam { get; set; }

        public override void OrdinalNumberChanged(int number)
        {
            //
        }

        public override bool IsValid() => true;

        public override void SaveConfig()
        {
            //
        }

        public override void VanishConfig()
        {
            //
        }
    }
}

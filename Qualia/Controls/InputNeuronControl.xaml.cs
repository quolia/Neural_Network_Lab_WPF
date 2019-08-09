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
    public partial class InputNeuronControl : NeuronBase
    {
        public InputNeuronControl(long id)
            : base(id, null, null)
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed; // do not show it
        }

        public override string WeightsInitializer => nameof(InitializeMode.None);
        public override double? WeightsInitializerParamA => null;
        public override bool IsBias => false;
        public override bool IsBiasConnected => false;
        public override string ActivationFunc { get; set; }
        public override double? ActivationFuncParamA { get; set; }

        public override void OrdinalNumberChanged(int number)
        {
            //
        }

        public override bool IsValid()
        {
            return true;
        }

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

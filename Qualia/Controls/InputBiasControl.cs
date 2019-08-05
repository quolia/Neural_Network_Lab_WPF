using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace Qualia.Controls
{
    public class InputBiasControl : NeuronControl
    {
        public InputBiasControl()
            : base(0, null, null)
        {
            InitializeComponent();
        }

        public InputBiasControl(long id, Config config, Action<Notification.ParameterChanged, object> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            CtlIsBias.IsOn = true;
            CtlIsBiasConnected.IsOn = false;
            CtlIsBias.IsEnabled = false;
            CtlIsBiasConnected.Visible(CtlIsBias.IsOn);
            CtlIsBiasConnected.IsEnabled = false;
        }
    }
}

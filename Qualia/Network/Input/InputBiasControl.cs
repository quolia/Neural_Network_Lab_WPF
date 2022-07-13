using Qualia.Tools;
using System;

namespace Qualia.Controls
{
    sealed public class InputBiasControl : NeuronControl
    {
        public InputBiasControl(LayerBaseControl parentLayer)
            : base(0, null, null, parentLayer)
        {
            InitializeComponent();
        }

        public InputBiasControl(long id, Config config, Action<Notification.ParameterChanged> networkUI_OnChanged, LayerBaseControl parentLayer)
            : base(id, config, networkUI_OnChanged, parentLayer)
        {
            InitializeComponent();

            CtlIsBias.Value = true;
            CtlIsBiasConnected.Value = false;
            CtlIsBias.IsEnabled = false;
            CtlIsBiasConnected.SetVisible(CtlIsBias.Value);
            CtlIsBiasConnected.IsEnabled = false;
        }
    }
}

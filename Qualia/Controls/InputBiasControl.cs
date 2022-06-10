﻿using System;
using Tools;

namespace Qualia.Controls
{
    sealed public class InputBiasControl : NeuronControl
    {
        public InputBiasControl()
            : base(0, null, null)
        {
            InitializeComponent();
        }

        public InputBiasControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
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

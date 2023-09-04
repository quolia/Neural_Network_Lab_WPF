using Qualia.Tools;

namespace Qualia.Controls
{
    sealed public class InputBiasControl : NeuronControl
    {
        public InputBiasControl()
            : base(0, null, null, null)
        {
            InitializeComponent();
        }

        public InputBiasControl(long id,
                                Config config,
                                ActionManager.ApplyActionDelegate onChanged,
                                LayerBaseControl parentLayer)
            : base(id,
                   config,
                   onChanged,
                   parentLayer)
        {
            InitializeComponent();
        }
    }
}

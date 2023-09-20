using Qualia.Network.Base;
using Qualia.Network.Hidden;
using Qualia.Tools;
using Qualia.Tools.Managers;

namespace Qualia.Network.Input;

public sealed class InputBiasControl : NeuronControl
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
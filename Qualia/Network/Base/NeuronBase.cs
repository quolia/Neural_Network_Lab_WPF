using Qualia.Tools;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Qualia.Controls
{
    abstract public partial class NeuronBaseControl : BaseUserControl
    {
        public readonly long Id;
        public readonly Config Config;

        private readonly MenuItem _menuAdd;
        private readonly MenuItem _menuRemove;

        private readonly LayerBaseControl _parentLayer;

        public NeuronBaseControl(long id,
                                 Config config,
                                 Action<Notification.ParameterChanged> onChanged,
                                 LayerBaseControl parentLayer)
        {
            _parentLayer = parentLayer;

            ContextMenu = new();
            ContextMenu.Opened += ContextMenu_OnOpened;

            _menuAdd = new() { Header = "Add" };
            ContextMenu.Items.Add(_menuAdd);
            _menuAdd.Click += AddNeuron_OnClick;

            _menuRemove = new() { Header = "Remove..." };
            ContextMenu.Items.Add(_menuRemove);
            _menuRemove.Click += RemoveNeuron_OnClick;

            Id = UniqId.GetNextId(id);
            if (config != null)
            {
                Config = config.ExtendWithId(Id);
            }

            SetOnChangeEvent(onChanged);
        }

        private void ContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            _menuRemove.IsEnabled = _parentLayer.CanNeuronBeRemoved();
        }

        private void AddNeuron_OnClick(object sender, RoutedEventArgs e)
        {
            _parentLayer.AddNeuron();
        }

        private void RemoveNeuron_OnClick(object sender, RoutedEventArgs e)
        {
            RemoveNeuron();
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            var sv = this.GetParentOfType<ScrollViewer>();
            if (sv != null)
            {
                sv.ScrollToBottom();
            }
        }

        public void StateChanged()
        {
            Background = IsBias 
                         ? Draw.GetBrush(Draw.GetRandomColor(20, Draw.GetColor(240, 250, 240)))
                         : Draw.GetBrush(in ColorsX.Lavender);
        }

        abstract public InitializeFunction ActivationInitializeFunction { get; }
        abstract public double ActivationInitializeFunctionParam { get; }
        abstract public InitializeFunction WeightsInitializeFunction { get; }
        abstract public double WeightsInitializeFunctionParam { get; }

        abstract public ActivationFunction ActivationFunction { get; set; }
        abstract public double ActivationFunctionParam { get; set; }
        abstract public double PositiveTargetValue { get; set; }
        abstract public double NegativeTargetValue { get; set; }
        abstract public string Label { get; }

        abstract public bool IsBias { get; }
        abstract public bool IsBiasConnected { get; }
        abstract public void SetOrdinalNumber(int number);

        private void RemoveNeuron()
        {
            var removed = _parentLayer.RemoveNeuron(this);
        }
    }
}

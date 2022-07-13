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

        public readonly Action<Notification.ParameterChanged> NetworkUI_OnChanged;

        private readonly MenuItem _menuAdd;
        private readonly MenuItem _menuDelete;

        private readonly LayerBaseControl _parentLayer;

        public NeuronBaseControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged, LayerBaseControl parentLayer)
        {
            _parentLayer = parentLayer;

            ContextMenu = new();
            ContextMenu.Opened += OnContextMenuOpened;

            _menuAdd = new() { Header = "Add" };
            ContextMenu.Items.Add(_menuAdd);
            _menuAdd.Click += OnAddNeuronClick;

            _menuDelete = new() { Header = "Delete..." };
            ContextMenu.Items.Add(_menuDelete);
            _menuDelete.Click += OnDeleteNeuronClick;

            NetworkUI_OnChanged = onNetworkUIChanged;

            Id = UniqId.GetNextId(id);
            if (config != null)
            {
                Config = config.ExtendWithId(Id);
            }            
        }

        private void OnContextMenuOpened(object sender, RoutedEventArgs e)
        {
            _menuDelete.IsEnabled = _parentLayer.CanNeuronBeRemoved();
        }

        private void OnAddNeuronClick(object sender, RoutedEventArgs e)
        {
            _parentLayer.AddNeuron();
        }

        private void OnDeleteNeuronClick(object sender, RoutedEventArgs e)
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
        abstract public string Label { get; }

        abstract public bool IsBias { get; }
        abstract public bool IsBiasConnected { get; }
        //abstract public bool IsValid();
        //abstract public void SaveConfig();
        //abstract public void RemoveFromConfig();
        abstract public void OrdinalNumber_OnChanged(int number);

        private void RemoveNeuron()
        {
            var removed = _parentLayer.RemoveNeuron(this);
        }
    }
}

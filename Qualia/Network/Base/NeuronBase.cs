using Qualia.Tools;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Qualia.Controls
{
    public partial class NeuronBaseControl : BaseUserControl
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

        public virtual InitializeFunction ActivationInitializeFunction => throw new InvalidOperationException();
        public virtual double ActivationInitializeFunctionParam => throw new InvalidOperationException();
        public virtual InitializeFunction WeightsInitializeFunction => throw new InvalidOperationException();
        public virtual double WeightsInitializeFunctionParam => throw new InvalidOperationException();

        public virtual ActivationFunction ActivationFunction
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        public virtual double ActivationFunctionParam
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }
        public virtual string Label
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        public virtual bool IsBias => throw new InvalidOperationException();
        public virtual bool IsBiasConnected => throw new InvalidOperationException();
        public virtual bool IsValid() => throw new InvalidOperationException();
        public virtual void SaveConfig() => throw new InvalidOperationException();
        public virtual void RemoveFromConfig() => throw new InvalidOperationException();
        public virtual void OrdinalNumber_OnChanged(int number) => throw new InvalidOperationException();

        private void RemoveNeuron()
        {
            var removed = _parentLayer.RemoveNeuron(this);
        }
    }
}

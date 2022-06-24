using Qualia.Tools;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    public partial class NeuronBase : UserControl
    {
        public readonly long Id;
        public Config Config;

        public Action<Notification.ParameterChanged> OnNetworkUIChanged;

        private MenuItem _menuAdd;
        private MenuItem _menuDelete;

        public NeuronBase(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
        {
            ContextMenu = new();
            ContextMenu.Opened += ContextMenu_Opened;

            _menuAdd = new() { Header = "Add" };
            ContextMenu.Items.Add(_menuAdd);
            _menuAdd.Click += AddNeuron_Click;

            _menuDelete = new() { Header = "Delete..." };
            ContextMenu.Items.Add(_menuDelete);
            _menuDelete.Click += DeleteNeuron_Click;

            OnNetworkUIChanged = onNetworkUIChanged;

            Id = UniqId.GetNextId(id);
            if (config != null)
            {
                Config = config.Extend(Id);
            }            
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            _menuDelete.IsEnabled = this.GetParentOfType<LayerBase>().NeuronsCount > 1;
        }

        private void AddNeuron_Click(object sender, RoutedEventArgs e)
        {
            this.GetParentOfType<LayerBase>().AddNeuron();
        }

        private void DeleteNeuron_Click(object sender, RoutedEventArgs e)
        {
            DeleteNeuron();
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
                         : Draw.GetBrush(Draw.GetRandomColor(20, in QColors.Lavender));
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

        public virtual bool IsBias => throw new InvalidOperationException();
        public virtual bool IsBiasConnected => throw new InvalidOperationException();
        public virtual bool IsValid() => throw new InvalidOperationException();
        public virtual void SaveConfig() => throw new InvalidOperationException();
        public virtual void VanishConfig() => throw new InvalidOperationException();
        public virtual void OrdinalNumberChanged(int number) => throw new InvalidOperationException();

        private void DeleteNeuron()
        {
            var layerBase = this.GetParentOfType<LayerBase>();
            if (layerBase.NeuronsCount < 2)
            {
                MessageBox.Show("At least one neuron must exist.", "Warning", MessageBoxButton.OK);
                return;
            }

            var color = Background;
            Background = Brushes.Tomato;
            if (MessageBox.Show("Would you really like to delete the neuron?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                (Parent as Panel).Children.Remove(this);
                VanishConfig();
                layerBase.RefreshOrdinalNumbers();
                OnNetworkUIChanged(Notification.ParameterChanged.NeuronsCount);
            }
            else
            {
                Background = color;
            }
        }
    }
}

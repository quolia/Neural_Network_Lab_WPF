using Qualia.Tools;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Qualia.Controls
{
    abstract public partial class NeuronBaseControl : BaseUserControl
    {
        public readonly long Id;
        public readonly Config Config;

        private readonly MenuItem _menuAdd;
        private readonly MenuItem _menuRemove;
        private readonly MenuItem _menuCopyParamsToSelectedNeurons;

        private readonly LayerBaseControl _parentLayer;

        public bool IsSelected
        {
            get => NeuronsSelector.Instance.IsSelected(this);
            set => NeuronsSelector.Instance.SetSelected(this, value);
        }

        public NeuronBaseControl(long id,
                                 Config config,
                                 Action<Notification.ParameterChanged> onChanged,
                                 LayerBaseControl parentLayer)
        {
            _parentLayer = parentLayer;

            ContextMenu = new();
            ContextMenu.Opened += ContextMenu_OnOpened;

            _menuAdd = new() { Header = "Add Neuron" };
            ContextMenu.Items.Add(_menuAdd);
            _menuAdd.Click += AddNeuron_OnClick;

            _menuRemove = new() { Header = "Remove Neuron..." };
            ContextMenu.Items.Add(_menuRemove);
            _menuRemove.Click += RemoveNeuron_OnClick;

            _menuCopyParamsToSelectedNeurons = new() { Header = "Copy parameters to selected neurons" };
            ContextMenu.Items.Add(_menuCopyParamsToSelectedNeurons);
            _menuCopyParamsToSelectedNeurons.Click += CopyParamsToSelected_OnClick;

            Id = UniqId.GetNextId(id);
            if (config != null)
            {
                Config = config.ExtendWithId(Id);
            }

            SetOnChangeEvent(onChanged);

            Background = Draw.GetBrush(in ColorsX.Lavender);
        }

        private void ContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            _menuAdd.IsEnabled = _parentLayer.CanNeuronBeAdded();
            _menuRemove.IsEnabled = _parentLayer.CanNeuronBeRemoved();
            _menuCopyParamsToSelectedNeurons.IsEnabled = NeuronsSelector.Instance.SelectedCount > 0;
        }

        private void AddNeuron_OnClick(object sender, RoutedEventArgs e)
        {
            _parentLayer.AddNeuron();
        }

        private void RemoveNeuron_OnClick(object sender, RoutedEventArgs e)
        {
            RemoveNeuron();
        }

        private void CopyParamsToSelected_OnClick(object sender, RoutedEventArgs e)
        {
            NeuronsSelector.Instance.CopyParamsToSelected(this);
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            var sv = this.GetParentOfType<ScrollViewer>();
            if (sv != null)
            {
                sv.ScrollToBottom();
            }
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

        abstract public void SetOrdinalNumber(int number);

        private void RemoveNeuron()
        {
            var removed = _parentLayer.RemoveNeuron(this);
        }

        //

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            IsSelected = !IsSelected;
            base.OnMouseLeftButtonDown(e);
        }

        public void OnSelectionChanged(bool isSelected)
        {
            Background = isSelected ? Draw.GetBrush(ColorsX.Wheat) : Draw.GetBrush(ColorsX.Lavender);
        }
    }
}

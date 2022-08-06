﻿using Qualia.Tools;
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
        private readonly MenuItem _menuCopyPaste;
        private readonly MenuItem _menuRemove;
        private readonly MenuItem _menuSelectAll;
        private readonly MenuItem _menuDeselectAll;
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

            _menuCopyPaste = new() { Header = "Copy/Paste Neuron" };
            ContextMenu.Items.Add(_menuCopyPaste);
            _menuCopyPaste.Click += CopyPasteNeuron_OnClick;

            _menuRemove = new() { Header = "Remove Neuron..." };
            ContextMenu.Items.Add(_menuRemove);
            _menuRemove.Click += RemoveNeuron_OnClick;

            _menuCopyParamsToSelectedNeurons = new() { Header = "Copy parameters to selected neurons" };
            ContextMenu.Items.Add(_menuCopyParamsToSelectedNeurons);
            _menuCopyParamsToSelectedNeurons.Click += CopyParamsToSelected_OnClick;

            _menuSelectAll = new() { Header = "Select all" };
            ContextMenu.Items.Add(_menuSelectAll);
            _menuSelectAll.Click += SelectAll_OnClick;

            _menuDeselectAll = new() { Header = "Deselect all" };
            ContextMenu.Items.Add(_menuDeselectAll);
            _menuDeselectAll.Click += DeselectAll_OnClick;

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
            _menuCopyPaste.IsEnabled = _menuAdd.IsEnabled;
            _menuRemove.IsEnabled = _parentLayer.CanNeuronBeRemoved();
            _menuCopyParamsToSelectedNeurons.IsEnabled = NeuronsSelector.Instance.SelectedCount > 0;
        }

        private void AddNeuron_OnClick(object sender, RoutedEventArgs e)
        {
            _parentLayer.AddNeuron();
        }

        private void CopyPasteNeuron_OnClick(object sender, RoutedEventArgs e)
        {
            var newNeuron = _parentLayer.AddNeuron();
            CopyTo(newNeuron);
        }

        private void RemoveNeuron_OnClick(object sender, RoutedEventArgs e)
        {
            RemoveNeuron();
        }

        private void SelectAll_OnClick(object sender, RoutedEventArgs e)
        {
            _parentLayer.SetAllNeuronsSelected(true);
        }

        private void DeselectAll_OnClick(object sender, RoutedEventArgs e)
        {
            _parentLayer.SetAllNeuronsSelected(false);
        }

        private void CopyParamsToSelected_OnClick(object sender, RoutedEventArgs e)
        {
            NeuronsSelector.Instance.CopyNeuronToSelectedNeurons(this);
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
            var removedCount = _parentLayer.RemoveNeuron(this);
        }

        //

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            IsSelected = !IsSelected;
            base.OnMouseLeftButtonDown(e);
        }

        public void OnIsSelectedChanged(bool isSelected)
        {
            Background = isSelected ? Draw.GetBrush(ColorsX.Wheat) : Draw.GetBrush(ColorsX.Lavender);
        }

        public void SetRemovingState(bool isRemoving)
        {
            if (isRemoving)
            {
                Background = Draw.GetBrush(ColorsX.Tomato);
            }
            else
            {
                OnIsSelectedChanged(NeuronsSelector.Instance.IsSelected(this));
            }
        }

        public void CopyTo(NeuronBaseControl neuron)
        {
            if (neuron == null || this == neuron)
            {
                return;
            }

            neuron.ActivationFunction = ActivationFunction;
            neuron.ActivationFunctionParam = ActivationFunctionParam;

            if (neuron is OutputNeuronControl && this is OutputNeuronControl)
            {
                neuron.PositiveTargetValue = PositiveTargetValue;
                neuron.NegativeTargetValue = NegativeTargetValue;
            }
        }
    }
}

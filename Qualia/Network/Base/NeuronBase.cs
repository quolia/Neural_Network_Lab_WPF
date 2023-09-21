using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Qualia.Controls.Base;
using Qualia.Network.Output;
using Qualia.Tools;
using Qualia.Tools.Functions;
using Qualia.Tools.Managers;

namespace Qualia.Network.Base;

public abstract partial class NeuronBaseControl : BaseUserControl
{
    public readonly Config Config;

    private readonly MenuItem _menuAdd;
    private readonly MenuItem _menuClone;
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
        ActionManager.ApplyActionDelegate onChanged,
        LayerBaseControl parentLayer)
        : base(UniqId.GetNextId(id))
    {
        _parentLayer = parentLayer;

        ContextMenu = new ContextMenu();
        ContextMenu.Opened += ContextMenu_OnOpened;

        _menuAdd = new MenuItem { Header = "Add Neuron" };
        ContextMenu.Items.Add(_menuAdd);
        _menuAdd.Click += AddNeuron_OnClick;

        _menuClone = new MenuItem { Header = "Clone Neuron" };
        ContextMenu.Items.Add(_menuClone);
        _menuClone.Click += CloneNeuron_OnClick;

        _menuRemove = new MenuItem { Header = "Remove Neuron..." };
        ContextMenu.Items.Add(_menuRemove);
        _menuRemove.Click += RemoveNeuron_OnClick;

        _menuCopyParamsToSelectedNeurons = new MenuItem { Header = "Copy parameters to selected neurons" };
        ContextMenu.Items.Add(_menuCopyParamsToSelectedNeurons);
        _menuCopyParamsToSelectedNeurons.Click += CopyParamsToSelected_OnClick;

        _menuSelectAll = new MenuItem { Header = "Select all" };
        ContextMenu.Items.Add(_menuSelectAll);
        _menuSelectAll.Click += SelectAll_OnClick;

        _menuDeselectAll = new MenuItem { Header = "Deselect all" };
        ContextMenu.Items.Add(_menuDeselectAll);
        _menuDeselectAll.Click += DeselectAll_OnClick;

        if (config != null)
        {
            Config = config.ExtendWithId(VisualId);
        }

        SetOnChangeEvent(onChanged);

        Background = Draw.GetBrush(in ColorsX.Lavender);
    }

    private void ContextMenu_OnOpened(object sender, RoutedEventArgs e)
    {
        _menuAdd.IsEnabled = _parentLayer.CanNeuronBeAdded();
        _menuClone.IsEnabled = _menuAdd.IsEnabled;
        _menuRemove.IsEnabled = _parentLayer.CanNeuronBeRemoved(this);
        _menuCopyParamsToSelectedNeurons.IsEnabled = NeuronsSelector.Instance.SelectedCount > 0;
    }

    private void AddNeuron_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ActionManager.Instance.IsValid)
        {
            Messages.ShowError("Cannot add neuron. Editor has invalid value.");
            return;
        }

        _parentLayer.AddNeuron();
    }

    private void CloneNeuron_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ActionManager.Instance.IsValid)
        {
            Messages.ShowError("Cannot clone neuron. Editor has invalid value.");
            return;
        }

        var newNeuron = _parentLayer.AddNeuron();
        CopyTo(newNeuron);
    }

    private void RemoveNeuron_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ActionManager.Instance.IsValid)
        {
            Messages.ShowError("Cannot remove neuron. Editor has invalid value.");
            return;
        }

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
        OnChanged(new(this, Notification.ParameterChanged.NetworkUpdated));
    }

    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
        var sv = this.GetParentOfType<ScrollViewer>();
        if (sv != null)
        {
            sv.ScrollToBottom();
        }
    }

    public abstract ActivationFunction ActivationFunction { get; set; }
    public abstract double ActivationFunctionParam { get; set; }
    public abstract double PositiveTargetValue { get; set; }
    public abstract double NegativeTargetValue { get; set; }
    public abstract string Label { get; }

    public abstract void SetOrdinalNumber(int number);

    private void RemoveNeuron()
    {
        var _ = _parentLayer.RemoveNeuron(this);
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
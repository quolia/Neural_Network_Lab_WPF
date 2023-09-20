using System;
using System.Windows;
using System.Windows.Controls;
using Qualia.Tools;
using Qualia.Tools.Managers;

namespace Qualia.Controls.Base.Values;

public sealed class SelectorControl : ComboBox, IConfigParam
{
    public string DefaultValue { get; private set; }

    public SelectorControl Initialize(string defaultValue)
    {
        if (!string.IsNullOrEmpty(defaultValue))
        {
            DefaultValue = defaultValue;
        }

        return this;
    }

    //public SelectValueControl SetToolTip(SelectableItem toolTip)
    //{
    //ToolTip = toolTip;
    //    return this;
    //}

    public SelectorControl()
    {
        //ItemTemplate = Main.Instance.Resources["SelectableItemTemplate"] as DataTemplate;
        //Style = Main.Instance.Resources["SelectValueStyle"] as Style;
                 
        Padding = new Thickness(1);
        Margin = new Thickness(3);
        MinWidth = 60;

        //Background = Draw.GetBrush(ColorsX.Lime);

        //Background = Draw.GetBrush(in ColorsX.Yellow);
        //Foreground = Draw.GetBrush(in ColorsX.Green);
        //Resources.Add(SystemColors.WindowBrushKey, Draw.GetBrush(ColorsX.Yellow));
        //Resources.Add(SystemColors.HighlightBrushKey, Draw.GetBrush(ColorsX.Red));

        SelectionChanged += Value_OnChanged;
    }

    private void Value_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        var oldValue = e.RemovedItems.Count > 0 ? e.RemovedItems[0] : null;
        var newValue = e.AddedItems.Count > 0 ? e.AddedItems[0] : null;

        if (oldValue == newValue)
        {
            return;
        }

        if (oldValue == null || newValue == null)
        {
            return;
        }

        ApplyAction action = new(this)
        {
            Cancel = (isRunning) =>
            {
                SelectionChanged -= Value_OnChanged;
                Value = oldValue as ISelectableItem;
                SelectionChanged += Value_OnChanged;

                this.InvokeUIHandler(new(this));
            }
        };

        this.InvokeUIHandler(action);
    }

    public new ISelectableItem SelectedItem
    {
        get => base.SelectedItem as ISelectableItem;
        set => base.SelectedItem = value;
    }

    public bool IsValid() => !IsNull();

    private bool IsNull() => SelectedItem == null;

    public ISelectableItem Value
    {
        get => IsValid() ? SelectedItem : throw new InvalidValueException(Name, Text);

        set
        {
            SelectedItem = value;
        }
    }
    public void SelectByText(string text)
    {
        foreach (var item in Items)
        {
            if ((item as ISelectableItem)?.Text != text)
            {
                continue;
            }

            SelectedItem = item as ISelectableItem;
            return;
        }

        throw new InvalidValueException(Name, text);
    }

    // IConfigParam

    public void SetConfig(Config config)
    {
        this.PutConfig(config);
    }

    public void LoadConfig()
    {
        Value = SelectedItem;
    }

    public void SaveConfig()
    {
        this.GetConfig().Set(Name, Value.Text);
    }

    public void RemoveFromConfig()
    {
        this.GetConfig().Remove(Name);
    }

    public void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChanged)
    {
        this.SetUIHandler(onChanged);
    }

    public void InvalidateValue() => throw new InvalidOperationException();

    //

    public string ToXml()
    {
        var name = Config.PrepareParamName(Name);
        return $"<{name} Value=\"{Value}\" /> \n";
    }
}

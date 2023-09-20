using System;
using System.Windows;
using System.Windows.Controls;
using Qualia.Tools;
using Qualia.Tools.Managers;

namespace Qualia.Controls.Base.Values;

public sealed class BoolValueControl : CheckBox, IConfigParam
{
    public bool DefaultValue { get; set; }

    public BoolValueControl Initialize(bool defaultValue)
    {
        DefaultValue = defaultValue;
        return this;
    }

    public bool Value
    {
        get => IsChecked == true;
        set => IsChecked = value;
    }

    public BoolValueControl()
    {
        Padding = new Thickness(0);
        Margin = new Thickness(3);
    }

    private void EnableListeners(bool isEnable)
    {
        Checked -= Value_OnChanged;
        Unchecked -= Value_OnChanged;

        if (isEnable)
        {
            Checked += Value_OnChanged;
            Unchecked += Value_OnChanged;
        }
    }

    private void Value_OnChanged(object sender, RoutedEventArgs e)
    {
        ApplyAction action = new(this)
        {
            Cancel = (isRunning) =>
            {
                EnableListeners(false);
                Value = !Value;
                EnableListeners(true);

                this.InvokeUIHandler(new ApplyAction(this));
            }
        };

        this.InvokeUIHandler(action);
    }

    // IConfigParam

    public void SetConfig(Config config)
    {
        this.PutConfig(config);
    }

    public void LoadConfig()
    {
        Value = this.GetConfig().Get(Name, DefaultValue);
    }

    public void SaveConfig()
    {
        this.GetConfig().Set(Name, Value);
    }

    public void RemoveFromConfig()
    {
        this.GetConfig().Remove(Name);
    }

    public bool IsValid() => true;

    public void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChanged)
    {
        EnableListeners(true);
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
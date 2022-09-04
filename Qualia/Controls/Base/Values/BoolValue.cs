using Qualia.Tools;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public class BoolValueControl : CheckBox, IConfigParam
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
            Padding = new(0);
            Margin = new(3);
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

                    this.InvokeUIHandler(new(this));
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
            string name = Config.PrepareParamName(Name);
            return $"<{name} Value=\"{Value}\" /> \n";
        }
    }
}

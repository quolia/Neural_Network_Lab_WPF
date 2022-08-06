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

            Checked += Value_OnChanged;
            Unchecked += Value_OnChanged;
        }

        private void Value_OnChanged(object sender, RoutedEventArgs e)
        {
            this.GetUIHandler()(this.GetUIParam());
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

        public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
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

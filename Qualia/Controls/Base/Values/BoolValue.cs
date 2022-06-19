using Qualia.Tools;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public class BoolValueControl : CheckBox, IConfigParam
    {
        private Config _config;
        private event Action _onValueChanged = delegate { };

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
            Checked += Value_OnChanged;
            Unchecked += Value_OnChanged;
        }

        private void Value_OnChanged(object sender, RoutedEventArgs e)
        {
            _onValueChanged();
        }

        public void SetConfig(Config config)
        {
            _config = config.Extend(this);
        }

        public void LoadConfig()
        {
            Value = _config.Get(this, DefaultValue);
        }

        public void SaveConfig()
        {
            _config.Set(this, Value);
        }

        public void RemoveFromConfig()
        {
            _config.Remove(this);
        }

        public bool IsValid() => true;

        public void SetChangeEvent(Action onValueChanged)
        {
            _onValueChanged = onValueChanged;
        }

        public void InvalidateValue() => throw new InvalidOperationException();

        public string ToXml()
        {
            string name = Config.PrepareParamName(Name);
            return $"<{name} Value=\"{Value}\" /> \n";
        }
    }
}

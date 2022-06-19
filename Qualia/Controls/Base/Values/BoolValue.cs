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

        public bool IsOn
        {
            get => IsChecked == true;
            set => IsChecked = value;
        }

        public BoolValueControl()
        {
            Checked += OnValueChanged;
            Unchecked += OnValueChanged;
        }

        private void OnValueChanged(object sender, RoutedEventArgs e)
        {
            _onValueChanged();
        }

        public void SetConfig(Config config)
        {
            _config = config.Extend(this);
        }

        public void LoadConfig()
        {
            IsOn = _config.Get(this, DefaultValue);
        }

        public void SaveConfig()
        {
            _config.Set(this, IsOn);
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
            throw new NotImplementedException();
        }
    }
}

using Qualia.Tools;
using System;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public class BoolValueControl : CheckBox, IConfigParam
    {
        private Config _config;

        private event Action Changed = delegate { };

        public bool DefaultValue { get; set; }

        public BoolValueControl SetDefaultValue(bool value)
        {
            DefaultValue = value;
            return this;
        }

        public bool IsOn
        {
            get => IsChecked == true;
            set => IsChecked = value;
        }

        public BoolValueControl()
        {
            Checked += OnOffBox_Changed;
            Unchecked += OnOffBox_Changed;
        }

        private void OnOffBox_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            Changed();
        }

        public void SetConfig(Config config)
        {
            _config = config;
        }

        public void LoadConfig()
        {
            IsOn = _config.Get(this, DefaultValue);
        }

        public void SaveConfig()
        {
            _config.Set(Name, IsOn);
        }

        public void VanishConfig()
        {
            _config.Remove(Name);
        }

        public bool IsValid() => true;

        public void SetChangeEvent(Action action)
        {
            Changed = action;
        }

        public void InvalidateValue() => throw new InvalidOperationException();
    }
}

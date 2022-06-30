using Qualia.Tools;
using System;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public class SelectValueControl : ComboBox, IConfigParam
    {
        private Config _config;
        private event Action _onChanged = delegate { };

        public string DefaultValue { get; set; }

        public SelectValueControl Initialize(string defaultFunctionName)
        {
            if (!string.IsNullOrEmpty(defaultFunctionName))
            {
                DefaultValue = defaultFunctionName;
            }

            return this;
        }

        public SelectValueControl()
        {
            //ItemTemplate = Main.Instance.Resources["QComboBoxTemplate"] as DataTemplate;

            Padding = new(2);
            Margin = new(1);
            MinWidth = 60;

            SelectionChanged += Value_OnChanged;
        }

        private void Value_OnChanged(object sender, SelectionChangedEventArgs e)
        {
            Text = SelectedItem?.ToString();
            _onChanged();
        }
        public bool IsValid() => !IsNull();

        public bool IsNull() => string.IsNullOrEmpty(Text) && string.IsNullOrEmpty(DefaultValue);

        public string Value
        {
            get => IsValid() ? Text : throw new InvalidValueException(Name, Text);

            set
            {
                Text = value;
                Value_OnChanged(null, null);
            }
        }

        public void SetConfig(Config config)
        {
            _config = config;//.Extend(this);
        }

        public void LoadConfig()
        {
            Value = SelectedItem.ToString();
        }

        public void SaveConfig()
        {
            _config.Set(Name, SelectedItem.ToString());
        }

        public void RemoveFromConfig()
        {
            _config.Remove(Name);
        }

        public void SetChangeEvent(Action onChanged)
        {
            _onChanged -= onChanged;
            _onChanged += onChanged;
        }

        public void InvalidateValue() => throw new InvalidOperationException();

        public string ToXml()
        {
            string name = Config.PrepareParamName(Name);
            return $"<{name} Value=\"{Value}\" /> \n";
        }
    }
}

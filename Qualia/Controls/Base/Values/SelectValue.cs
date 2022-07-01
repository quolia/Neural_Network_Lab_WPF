using Qualia.Tools;
using System;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public class SelectValueControl : ComboBox, IConfigParam
    {
        private Config _config;
        private event Action<Notification.ParameterChanged> _onChanged = delegate {};

        public string DefaultValue { get; set; }

        public SelectValueControl Initialize(string defaultValue)
        {
            if (!string.IsNullOrEmpty(defaultValue))
            {
                DefaultValue = defaultValue;
            }

            return this;
        }

        public Notification.ParameterChanged UIParam { get; private set; }

        public SelectValueControl SetUIParam(Notification.ParameterChanged param)
        {
            UIParam = param;
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
             _onChanged(UIParam);
        }

        public bool IsValid() => !IsNull();

        private bool IsNull() => SelectedItem == null;

        public string Value
        {
            get => IsValid() ? SelectedItem.ToString() : throw new InvalidValueException(Name, Text);

            set
            {
                SelectedItem = value;
            }
        }

        public void SetConfig(Config config)
        {
            _config = config;
        }

        public void LoadConfig()
        {
            Value = SelectedItem.ToString();
        }

        public void SaveConfig()
        {
            _config.Set(Name, Value);
        }

        public void RemoveFromConfig()
        {
            _config.Remove(Name);
        }

        public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
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

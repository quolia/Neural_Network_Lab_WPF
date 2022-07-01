using Qualia.Tools;
using System;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public class SelectValueControl : ComboBox, IConfigParam
    {
        private Config _config;
        private event Action _onChanged = delegate {};

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

            SelectionChanged += Selection_OnChanged;
        }

        private void Selection_OnChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Text != SelectedItem?.ToString())
            {
                Text = SelectedItem?.ToString();
                _onChanged();
            }
        }

        public bool IsValid() => !IsNull();

        private bool IsNull() => SelectedItem == null;

        public string Value
        {
            get => IsValid() ? SelectedItem.ToString() : throw new InvalidValueException(Name, Text);

            set
            {
                if (SelectedItem?.ToString() != value)
                {
                    SelectedItem = value;
                    //Selection_OnChanged(null, null);
                }
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

        public void AddChangeEventListener(Action onChanged)
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

        public void RemoveChangeEventListener(Action action)
        {
            throw new NotImplementedException();
        }
    }
}

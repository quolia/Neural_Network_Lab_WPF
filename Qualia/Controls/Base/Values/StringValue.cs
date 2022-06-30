using Qualia.Tools;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public class StringValueControl : TextBox, IConfigParam
    {
        private Config _config;

        private event Action _onChanged = delegate { };

        public string DefaultValue { get; set; }

        public StringValueControl Initialize(string defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }

        public StringValueControl()
        {
            InvalidateValue();
            TextChanged += OnValueChanged;
        }

        private void OnValueChanged(object sender, EventArgs e)
        {
            if (IsValid())
            {
                Background = Brushes.White;
                _onChanged();
            }
            else
            {
                Background = Brushes.Tomato;
            }
        }

        public bool IsValid() => !string.IsNullOrEmpty(Text);

        public bool IsNull() => string.IsNullOrEmpty(Text);

        public string Value
        {
            get => IsValid() ? Text : throw new InvalidValueException(Name, Text);

            set
            {
                Text = value;
                OnValueChanged(null, null);
            }
        }

        public void SetConfig(Config config)
        {
            _config = config;//.Extend(this);
        }

        public void LoadConfig()
        {
            Value = _config.Get(Name, DefaultValue);
        }

        public void SaveConfig()
        {
            _config.Set(Name, Value);
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

        public void InvalidateValue()
        {
            Background = IsValid() ? Brushes.White : Brushes.Tomato;
        }

        public string ToXml()
        {
            string name = Config.PrepareParamName(Name);
            return $"<{name} Value=\"{Value}\" /> \n";
        }
    }
}

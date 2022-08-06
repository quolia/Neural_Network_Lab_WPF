using Qualia.Tools;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public class StringValueControl : TextBox, IConfigParam
    {
        private Config _config;

        private event Action<Notification.ParameterChanged> _onChanged = delegate { };

        public string DefaultValue { get; set; } = string.Empty;

        public bool IsEmptyAllowed { get; set; } = false;

        public StringValueControl Initialize(string defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }

        public StringValueControl()
        {
            Padding = new(0);
            Margin = new(3);

            TextChanged += Value_OnChanged;
        }

        private void Value_OnChanged(object sender, EventArgs e)
        {
            if (IsValid())
            {
                Background = Brushes.White;
                _onChanged(this.GetUIParam());
            }
            else
            {
                Background = Brushes.Tomato;
            }
        }

        public bool IsValid() => !IsNull();

        public bool IsNull() => string.IsNullOrEmpty(Text) && !IsEmptyAllowed;

        public string Value
        {
            get => IsValid() ? Text : throw new InvalidValueException(Name, Text);

            set
            {
                Text = value;
            }
        }

        // IConfigParam

        public void SetConfig(Config config)
        {
            _config = config;
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

        public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            _onChanged = onChanged;
        }

        public void InvalidateValue()
        {
            Background = IsValid() ? Brushes.White : Brushes.Tomato;
        }

        //

        public string ToXml()
        {
            string name = Config.PrepareParamName(Name);
            return $"<{name} Value=\"{Value}\" /> \n";
        }
    }
}

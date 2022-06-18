using Qualia.Tools;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public class IntValueControl : TextBox, IConfigParam
    {
        private Config _config;

        private event Action OnChanged = delegate { };

        public long DefaultValue { get; set; }

        public IntValueControl SetDefaultValue(long value)
        {
            DefaultValue = value;
            return this;
        }

        public long MinimumValue { get; set; } = long.MinValue;

        public long MaximumValue { get; set; } = long.MaxValue;

        public IntValueControl()
        {
            Padding = new(0);
            Margin = new(3);
            MinWidth = 60;

            TextChanged += IntBox_TextChanged;
        }

        private void IntBox_TextChanged(object sender, EventArgs e)
        {
            if (IsValid())
            {
                Background = Brushes.White;
                OnChanged();
            }
            else
            {
                Background = Brushes.Tomato;
            }
        }

        public bool IsValid()
        {
            if (IsNull())
            {
                return false;
            }

            var ok = Converter.TryTextToInt(Text, out long value, DefaultValue);
            return ok && (value >= MinimumValue && value <= MaximumValue);
        }

        public bool IsNull() => string.IsNullOrEmpty(Text);

        public long Value
        {
            get
            {
                return IsValid()
                       ? (IsNull() ? throw new InvalidValueException(Name, "null") : Converter.TextToInt(Text).Value)
                       : throw new InvalidValueException(Name, Text);
            }

            set
            {
                Text = Converter.IntToText(value);
                IntBox_TextChanged(null, null);
            }
        }

        public void SetConfig(Config config)
        {
            _config = config;
        }

        public void LoadConfig()
        {
            var value = _config.Get(this, DefaultValue);

            if (value < MinimumValue)
            {
                value = MinimumValue;
            }

            if (value > MaximumValue)
            {
                value = MaximumValue;
            }

            Value = value;
        }

        public void SaveConfig()
        {
            _config.Set(Name, Value);
        }

        public void VanishConfig()
        {
            _config.Remove(Name);
        }

        public void SetChangeEvent(Action onChanged)
        {
            OnChanged -= onChanged;
            OnChanged += onChanged;
        }

        public void InvalidateValue()
        {
            Background = Brushes.Tomato;
        }
    }
}

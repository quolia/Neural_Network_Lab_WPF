using Qualia.Tools;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public class DoubleValueControl : TextBox, IConfigParam
    {
        private Config _config;

        private event Action OnChanged = delegate { };

        public double DefaultValue{ get; set; } 

        public DoubleValueControl SetDefaultValue(double value)
        {
            DefaultValue = value;
            return this;
        }

        public double MinimumValue { get; set; } = double.MinValue;

        public double MaximumValue { get; set; } = double.MaxValue;

        public DoubleValueControl()
        {
            Padding = new(0);
            Margin = new(3);
            MinWidth = 60;

            TextChanged += DoubleBox_TextChanged;
        }

        private void DoubleBox_TextChanged(object sender, EventArgs e)
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

            var ok = Converter.TryTextToDouble(Text, out double value, DefaultValue);
            return ok && (value >= MinimumValue && value <= MaximumValue);
        }

        public bool IsNull() => string.IsNullOrEmpty(Text);

        public double Value
        {
            get => IsValid() 
                   ? (IsNull() ? throw new InvalidValueException(Name, "null") : Converter.TextToDouble(Text).Value)
                   : throw new InvalidValueException(Name, Text);

            set
            {
                Text = Converter.DoubleToText(value);
                DoubleBox_TextChanged(null, null);
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


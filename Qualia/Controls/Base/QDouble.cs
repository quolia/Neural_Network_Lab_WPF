using Qualia.Tools;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public class QDouble : TextBox, IConfigParam
    {
        private Config _config;

        private event Action OnChanged = delegate { };

        public double? DefaultValue { get; set; }

        public double MinimumValue { get; set; }

        public double MaximumValue { get; set; }

        public bool IsNullAllowed { get; set; }

        public bool IsUnranged => MinimumValue == 0 && MaximumValue == 0;

        public QDouble()
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
            if (IsNull() && IsNullAllowed)
            {
                return true;
            }

            var ok = Converter.TryTextToDouble(Text, out double? value);
            ok &= value != null || IsNullAllowed;

            return ok && (IsUnranged || (value >= MinimumValue && value <= MaximumValue));
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

        public double? ValueOrNull
        {
            get => IsNull() && IsNullAllowed
                   ? null
                   : IsValid() ? Converter.TextToDouble(Text) : throw new InvalidValueException(Name, Text);

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
            var value = _config.GetDouble(Name, DefaultValue);
            if (!IsUnranged && value.HasValue)
            {
                if (value.Value < MinimumValue)
                {
                    value = MinimumValue;
                }

                if (value.Value > MaximumValue)
                {
                    value = MaximumValue;
                }
            }

            if (IsNullAllowed)
            {
                ValueOrNull = value;
            }
            else
            {
                Value = value.Value;
            }
        }

        public void SaveConfig()
        {
            _config.Set(Name, IsNullAllowed ? ValueOrNull : Value);
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


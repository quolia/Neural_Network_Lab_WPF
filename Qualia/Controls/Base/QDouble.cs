using Tools;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    public class QDouble : TextBox, IConfigValue
    {
        Config Config;

        event Action OnChanged = delegate { };

        public double? DefaultValue
        {
            get;
            set;
        }

        public double MinimumValue
        {
            get;
            set;
        }

        public double MaximumValue
        {
            get;
            set;
        }

        public bool IsNullAllowed
        {
            get;
            set;
        }

        public bool IsUnranged => MinimumValue == 0 && MaximumValue == 0;

        public QDouble()
        {
            //Width = 60;
            //Height = 18;
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

        public bool IsNull()
        {
            return String.IsNullOrEmpty(Text);
        }

        public double Value
        {
            get => IsValid() ? (IsNull() ? throw new InvalidValueException(Name, "null") : Converter.TextToDouble(Text).Value) : throw new InvalidValueException(Name, Text);

            set
            {
                Text = Converter.DoubleToText(value);
                DoubleBox_TextChanged(null, null);
            }
        }

        public double? ValueOrNull
        {
            get => IsNull() && IsNullAllowed ? null : IsValid() ? Converter.TextToDouble(Text) : throw new InvalidValueException(Name, Text);

            set
            {
                Text = Converter.DoubleToText(value);
                DoubleBox_TextChanged(null, null);
            }
        }

        public void SetConfig(Config config)
        {
            Config = config;
        }

        public void LoadConfig()
        {
            var value = Config.GetDouble(Name, DefaultValue);
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
                ValueOrNull = Config.GetDouble(Name, DefaultValue);
            }
            else
            {
                Value = Config.GetDouble(Name, DefaultValue).Value;
            }
        }

        public void SaveConfig()
        {
            Config.Set(Name, IsNullAllowed ? ValueOrNull : Value);
        }

        public void VanishConfig()
        {
            Config.Remove(Name);
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


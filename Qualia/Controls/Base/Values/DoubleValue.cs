using Qualia.Tools;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public class DoubleValueControl : TextBox, IConfigParam
    {
        private Config _config;
        private event Action _onChanged = delegate { };

        public double DefaultValue { get; set; } = double.NaN;

        public DoubleValueControl Initialize(double? defaultValue = null, double? minValue = null, double? maxValue = null)
        {
            if (defaultValue.HasValue)
            {
                DefaultValue = defaultValue.Value;
            }

            if (minValue.HasValue)
            {
                MinimumValue = minValue.Value;
            }

            if (maxValue.HasValue)
            {
                MaximumValue = maxValue.Value;
            }

            return this;
        }

        public double MinimumValue { get; set; } = double.MinValue;

        public double MaximumValue { get; set; } = double.MaxValue;

        public DoubleValueControl()
        {
            Padding = new(0);
            Margin = new(3);
            MinWidth = 60;

            TextChanged += Value_OnChanged;
        }

        private void Value_OnChanged(object sender, EventArgs e)
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

        public bool IsValid()
        {
            if (IsNull())
            {
                return false;
            }

            try
            {
                var value = Converter.TextToDouble(Text, DefaultValue);
                return value >= MinimumValue && value <= MaximumValue;
            }
            catch
            {
                return false;
            }
        }

        public bool IsNull() => string.IsNullOrEmpty(Text) && double.IsNaN(DefaultValue);

        public double Value
        {
            get
            {
                if (string.IsNullOrEmpty(Text) && !double.IsNaN(DefaultValue))
                {
                    Text = Converter.DoubleToText(DefaultValue);
                }

                return IsValid()
                       ? Converter.TextToDouble(Text, DefaultValue)
                       : throw new InvalidValueException(Name, Text);
            }

            set
            {
                Text = Converter.DoubleToText(value);
                Value_OnChanged(null, null);
            }
        }

        public void SetConfig(Config config)
        {
            _config = config.Extend(this);
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
            _config.Set(this, Value);
        }

        public void RemoveFromConfig()
        {
            _config.Remove(this);
        }

        public void SetChangeEvent(Action onChanged)
        {
            _onChanged -= onChanged;
            _onChanged += onChanged;
        }

        public void InvalidateValue()
        {
            Background = Brushes.Tomato;
        }

        public string ToXml()
        {
            string name = Config.PrepareParamName(Name);
            return $"<{name} Value=\"{Value}\" /> \n";
        }
    }
}


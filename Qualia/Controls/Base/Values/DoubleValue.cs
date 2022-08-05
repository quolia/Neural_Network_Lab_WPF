using Qualia.Tools;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public class DoubleValueControl : TextBox, IConfigParam
    {
        private Config _config;
        private event Action<Notification.ParameterChanged> _onChanged = delegate {};

        public double DefaultValue { get; set; } = Constants.InvalidDouble;

        public DoubleValueControl Initialize(double? defaultValue = null, double? minValue = null, double? maxValue = null)
        {
            if (defaultValue.HasValue)
            {
                DefaultValue = defaultValue.Value;
            }

            if (minValue.HasValue)
            {
                MinValue = minValue.Value;
            }

            if (maxValue.HasValue)
            {
                MaxValue = maxValue.Value;
            }

            return this;
        }

        public double MinValue { get; set; } = double.MinValue;

        public double MaxValue { get; set; } = double.MaxValue;

        public Notification.ParameterChanged UIParam { get; private set; }

        public DoubleValueControl SetUIParam(Notification.ParameterChanged param)
        {
            UIParam = param;
            return this;
        }

        public DoubleValueControl()
        {
            Padding = new(0);
            Margin = new(3);
            //MinWidth = 30;

            TextChanged += Value_OnChanged;
        }

        private void Value_OnChanged(object sender, EventArgs e)
        {
            if (IsValidInput(Constants.InvalidDouble))
            {
                _onChanged(UIParam);
            }
        }

        private bool IsValidInput(double defaultValue)
        {
            try
            {
                if (!IsNull(defaultValue))
                {
                    var value = Converter.TextToDouble(Text, defaultValue);
                    if (value >= MinValue && value <= MaxValue)
                    {
                        Background = Brushes.White;
                        return true;
                    }
                }
            }
            catch
            {
                //
            }

            InvalidateValue();
            return false;
        }

        public bool IsValid()
        {
            return IsValidInput(DefaultValue);
        }

        private bool IsNull(double defaultValue) => string.IsNullOrEmpty(Text) && Constants.IsInvalid(defaultValue);

        public double Value
        {
            get
            {
                if (string.IsNullOrEmpty(Text) && !Constants.IsInvalid(DefaultValue))
                {
                    Text = Converter.DoubleToText(DefaultValue);
                }

                return IsValid()
                       ? Converter.TextToDouble (Text, DefaultValue)
                       : throw new InvalidValueException(Name, Text);
            }

            set
            {
                Text = Converter.DoubleToText(value);
            }
        }

        // IConfigParam

        public void SetConfig(Config config)
        {
            _config = config;
        }

        public void LoadConfig()
        {
            var value = _config.Get(Name, DefaultValue);

            if (value < MinValue)
            {
                value = MinValue;
            }

            if (value > MaxValue)
            {
                value = MaxValue;
            }

            Value = value;
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
            Background = Brushes.Tomato;
        }

        //

        public string ToXml()
        {
            string name = Config.PrepareParamName(Name);
            return $"<{name} Value=\"{Value}\" /> \n";
        }
    }
}


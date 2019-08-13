using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public class QDouble : TextBox, IConfigValue
    {
        event Action OnChanged = delegate { };

        public Double? DefaultValue
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

        public QDouble()
        {
            Width = 60;
            Height = 18;
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

            return Converter.TryTextToDouble(Text, out double? value) && value >= MinimumValue && value <= MaximumValue;
        }

        public bool IsNull()
        {
            return String.IsNullOrEmpty(Text);
        }

        public double Value
        {
            get
            {
                return IsValid() ? (IsNull() ? throw new InvalidValueException(Name, "null") : Converter.TextToDouble(Text).Value) : throw new InvalidValueException(Name, Text);
            }

            set
            {
                Text = Converter.DoubleToText(value);
                DoubleBox_TextChanged(null, null);
            }
        }

        public double? ValueOrNull
        {
            get
            {
                return IsNull() && IsNullAllowed ? (double?)null : IsValid() ? Converter.TextToDouble(Text) : throw new InvalidValueException(Name, Text);
            }

            set
            {
                Text = Converter.DoubleToText(value);
                DoubleBox_TextChanged(null, null);
            }
        }

        public void Load(Config config)
        {
            if (IsNullAllowed)
                ValueOrNull = config.GetDouble(Name, DefaultValue);
            else
                Value = config.GetDouble(Name, DefaultValue).Value;
        }

        public void Save(Config config)
        {
            config.Set(Name, IsNullAllowed ? ValueOrNull : Value);
        }

        public void Vanish(Config config)
        {
            config.Remove(Name);
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

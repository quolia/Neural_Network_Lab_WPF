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
        public event Action Changed = delegate { };

        public Const.Param ConfigParameter
        {
            get;
            set;
        }

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
                Changed();
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

        public double Value => IsValid() ? (IsNull() ? throw new InvalidValueException(ConfigParameter, "null") : Converter.TextToDouble(Text).Value) : throw new InvalidValueException(ConfigParameter, Text);
        public double? ValueOrNull => IsNull() && IsNullAllowed ? (double?)null : IsValid() ? Converter.TextToDouble(Text) : throw new InvalidValueException(ConfigParameter, Text);

        public void Load(Config config)
        {
            Text = Converter.DoubleToText(config.GetDouble(ConfigParameter, DefaultValue));
        }

        public void Save(Config config)
        {
            config.Set(ConfigParameter, IsNullAllowed ? ValueOrNull : Value);
        }

        public void Vanish(Config config)
        {
            config.Remove(ConfigParameter);
        }

        public void SetChangeEvent(Action action)
        {
            Changed -= action;
            Changed += action;
        }
    }
}

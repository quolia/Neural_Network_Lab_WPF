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
    public class QInt : TextBox, IConfigValue
    {
        event Action OnChanged = delegate { };

        public int? DefaultValue
        {
            get;
            set;
        }

        public int MinimumValue
        {
            get;
            set;
        }

        public int MaximumValue
        {
            get;
            set;
        }

        public bool IsNullAllowed
        {
            get;
            set;
        }

        public QInt()
        {
            Width = 60;
            Height = 18;
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
            if (IsNull() && IsNullAllowed)
            {
                return true;
            }

            return Converter.TryTextToInt(Text, out int? value) && value >= MinimumValue && value <= MaximumValue;
        }

        public bool IsNull()
        {
            return String.IsNullOrEmpty(Text);
        }

        public int Value
        {
            get
            {
                return IsValid() ? (IsNull() ? throw new InvalidValueException(Name, "null") : Converter.TextToInt(Text).Value) : throw new InvalidValueException(Name, Text);
            }

            set
            {
                Text = Converter.IntToText(value);
                IntBox_TextChanged(null, null);
            }
        }

        public int? ValueOrNull
        {
            get
            {
                return IsNull() && IsNullAllowed ? (int?)null : IsValid() ? Converter.TextToInt(Text) : throw new InvalidValueException(Name, Text);
            }

            set
            {
                Text = Converter.IntToText(value);
                IntBox_TextChanged(null, null);
            }
        }

        public void Load(Config config)
        {
            if (IsNullAllowed)
                ValueOrNull = config.GetInt(Name, DefaultValue);
            else
                Value = config.GetInt(Name, DefaultValue).Value;
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
    }
}

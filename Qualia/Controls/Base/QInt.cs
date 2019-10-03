using Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    public class QInt : TextBox, IConfigValue
    {
        Config Config;

        event Action OnChanged = delegate { };

        public long? DefaultValue
        {
            get;
            set;
        }

        public long MinimumValue
        {
            get;
            set;
        }

        public long MaximumValue
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

        public QInt()
        {
            //Width = 60;
            //Height = 18;
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

            var ok = Converter.TryTextToInt(Text, out long? value);
            ok &= value != null || IsNullAllowed;

            return ok && (IsUnranged || (value >= MinimumValue && value <= MaximumValue));
        }

        public bool IsNull()
        {
            return String.IsNullOrEmpty(Text);
        }

        public long Value
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

        public long? ValueOrNull
        {
            get
            {
                return IsNull() && IsNullAllowed ? null : IsValid() ? Converter.TextToInt(Text) : throw new InvalidValueException(Name, Text);
            }

            set
            {
                Text = Converter.IntToText(value);
                IntBox_TextChanged(null, null);
            }
        }

        public void SetConfig(Config config)
        {
            Config = config;
        }

        public void LoadConfig()
        {
            var value = Config.GetInt(Name, DefaultValue);
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

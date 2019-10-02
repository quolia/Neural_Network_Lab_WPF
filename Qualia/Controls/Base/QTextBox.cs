using Tools;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    public class QTextBox : TextBox, IConfigValue
    {
        Config Config;

        event Action OnChanged = delegate { };

        public string DefaultValue
        {
            get;
            set;
        }

        public bool IsNullAllowed
        {
            get;
            set;
        }

        public QTextBox()
        {
            InvalidateValue();
            TextChanged += QTextBox_TextChanged;
        }

        private void QTextBox_TextChanged(object sender, EventArgs e)
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
            if (IsNullAllowed)
            {
                return true;
            }
            else
            {
                return !String.IsNullOrEmpty(Text);
            }
        }

        public bool IsNull()
        {
            return String.IsNullOrEmpty(Text);
        }

        public string Value
        {
            get
            {
                return IsValid() ? Text : throw new InvalidValueException(Name, Text);
            }

            set
            {
                Text = value;
                QTextBox_TextChanged(null, null);
            }
        }

        public void SetConfig(Config config)
        {
            Config = config;
        }

        public void LoadConfig()
        {
            Value = Config.GetString(Name, DefaultValue);
        }

        public void SaveConfig()
        {
            Config.Set(Name, Value);
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
            if (IsValid())
            {
                Background = Brushes.White;
            }
            else
            {
                Background = Brushes.Tomato;
            }
        }
    }
}

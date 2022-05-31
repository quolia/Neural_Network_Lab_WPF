using System;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public class QTextBox : TextBox, IConfigValue
    {
        private Config _config;

        private event Action OnChanged = delegate { };

        public string DefaultValue { get; set; }

        public bool IsNullAllowed { get; set; }

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
                return !string.IsNullOrEmpty(Text);
            }
        }

        public bool IsNull()
        {
            return string.IsNullOrEmpty(Text);
        }

        public string Value
        {
            get => IsValid() ? Text : throw new InvalidValueException(Name, Text);

            set
            {
                Text = value;
                QTextBox_TextChanged(null, null);
            }
        }

        public void SetConfig(Config config)
        {
            _config = config;
        }

        public void LoadConfig()
        {
            Value = _config.GetString(Name, DefaultValue);
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
            Background = IsValid() ? Brushes.White : Brushes.Tomato;
        }
    }
}

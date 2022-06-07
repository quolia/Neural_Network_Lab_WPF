using System;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public class QCheckBox : CheckBox, IConfigParam
    {
        private Config _config;

        private event Action Changed = delegate { };

        public bool DefaultValue { get; set; }

        public bool IsOn
        {
            get => IsChecked == true;
            set => IsChecked = value;
        }

        public QCheckBox()
        {
            Checked += OnOffBox_Changed;
            Unchecked += OnOffBox_Changed;
        }

        private void OnOffBox_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            Changed();
        }

        public void SetConfig(Config config)
        {
            _config = config;
        }

        public void LoadConfig()
        {
            IsOn = _config.GetBool(Name, DefaultValue);
        }

        public void SaveConfig()
        {
            _config.Set(Name, IsOn);
        }

        public void VanishConfig()
        {
            _config.Remove(Name);
        }

        public bool IsValid() => true;

        public void SetChangeEvent(Action action)
        {
            Changed = action;
        }

        public void InvalidateValue() => throw new InvalidOperationException();
    }
}

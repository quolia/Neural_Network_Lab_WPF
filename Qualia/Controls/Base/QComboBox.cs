using System;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public class QComboBox : ComboBox, IConfigParam
    {
        private Config _config;

        private event Action OnChanged = delegate { };

        string DefaultValue { get; set; }

        public QComboBox()
        {
            Padding = new System.Windows.Thickness(2);
            Margin = new System.Windows.Thickness(1);
            MinWidth = 60;

            SelectionChanged += SelectBox_SelectionChanged;
        }

        private void SelectBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnChanged();
        }

        public void SetConfig(Config config)
        {
            _config = config;
        }

        public void LoadConfig()
        {
            //
        }

        public void SaveConfig()
        {
            _config.Set(Name, SelectedItem.ToString());
        }

        public void VanishConfig()
        {
            _config.Remove(Name);
        }

        public bool IsValid() => true;

        public void SetChangeEvent(Action onChanged)
        {
            OnChanged -= onChanged;
            OnChanged += onChanged;
        }

        public void InvalidateValue()
        {
            throw new NotImplementedException();
        }
    }
}

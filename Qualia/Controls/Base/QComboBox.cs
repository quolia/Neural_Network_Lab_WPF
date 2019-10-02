using Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Qualia.Controls
{
    public class QComboBox : ComboBox, IConfigValue
    {
        Config Config;

        event Action OnChanged = delegate { };

        string DefaultValue
        {
            get;
            set;
        }

        public QComboBox()
        {
            //Padding = new System.Windows.Thickness(3, 2, 3, 2);
            SelectionChanged += SelectBox_SelectionChanged;
        }

        private void SelectBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnChanged();
        }

        public void SetConfig(Config config)
        {
            Config = config;
        }

        public void LoadConfig()
        {
            //
        }

        public void SaveConfig()
        {
            Config.Set(Name, SelectedItem.ToString());
        }

        public void VanishConfig()
        {
            Config.Remove(Name);
        }

        public bool IsValid()
        {
            return true;
        }

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

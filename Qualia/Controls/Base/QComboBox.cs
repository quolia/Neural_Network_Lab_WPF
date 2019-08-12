using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public class QComboBox : ComboBox, IConfigValue
    {
        event Action OnChanged = delegate { };

        string DefaultValue
        {
            get;
            set;
        }

        public QComboBox()
        {
            Padding = new System.Windows.Thickness(3, 2, 3, 2);
            SelectionChanged += SelectBox_SelectionChanged;
        }

        private void SelectBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnChanged();
        }

        public void Load(Config config)
        {
            throw new InvalidOperationException();
        }

        public void Save(Config config)
        {
            config.Set(Name, SelectedItem.ToString());
        }

        public void Vanish(Config config)
        {
            config.Remove(Name);
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
    }
}

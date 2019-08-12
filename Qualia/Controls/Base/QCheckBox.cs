using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public class QCheckBox : CheckBox, IConfigValue
    {
        event Action Changed = delegate { };

        public bool DefaultValue
        {
            get;
            set;
        }

        public bool IsOn
        {
            get
            {
                return IsChecked == true;
            }

            set
            {
                IsChecked = value;
            }
        }

        public QCheckBox()
        {
            Height = 18;
            Checked += OnOffBox_Changed;
            Unchecked += OnOffBox_Changed;
        }

        private void OnOffBox_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            Changed();
        }

        public void Load(Config config)
        {
            IsOn = config.GetBool(Name, DefaultValue);
        }

        public void Save(Config config)
        {
            config.Set(Name, IsOn);
        }

        public void Vanish(Config config)
        {
            config.Remove(Name);
        }

        public bool IsValid()
        {
            return true;
        }

        public void SetChangeEvent(Action action)
        {
            Changed = action;
        }
    }
}

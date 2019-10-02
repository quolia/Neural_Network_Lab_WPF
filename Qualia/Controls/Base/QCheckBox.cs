using Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Qualia.Controls
{
    public class QCheckBox : CheckBox, IConfigValue
    {
        Config Config;

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
            //Height = 18;
            Checked += OnOffBox_Changed;
            Unchecked += OnOffBox_Changed;
        }

        private void OnOffBox_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            Changed();
        }

        public void SetConfig(Config config)
        {
            Config = config;
        }

        public void LoadConfig()
        {
            IsOn = Config.GetBool(Name, DefaultValue);
        }

        public void SaveConfig()
        {
            Config.Set(Name, IsOn);
        }

        public void VanishConfig()
        {
            Config.Remove(Name);
        }

        public bool IsValid()
        {
            return true;
        }

        public void SetChangeEvent(Action action)
        {
            Changed = action;
        }

        public void InvalidateValue()
        {
            throw new NotImplementedException();
        }
    }
}

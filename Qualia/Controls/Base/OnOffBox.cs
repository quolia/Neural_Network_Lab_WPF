using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Qualia.Controls
{
    public class OnOffBox : CheckBox
    {
        public event Action<bool> CheckedChanged = delegate { };

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

        public OnOffBox()
        {
            Checked += OnOffBox_Checked;
            Unchecked += OnOffBox_Unchecked;
        }

        private void OnOffBox_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            CheckedChanged(false);
        }

        private void OnOffBox_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            CheckedChanged(true);
        }
    }
}

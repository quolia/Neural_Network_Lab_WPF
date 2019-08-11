using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Qualia.Controls
{
    public class QComboBox : ComboBox
    {
        public event Action<int> SelectedIndexChanged = delegate { };

        public QComboBox()
        {
            Padding = new System.Windows.Thickness(3, 2, 3, 2);
            SelectionChanged += SelectBox_SelectionChanged;
        }

        private void SelectBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedIndexChanged(SelectedIndex);
        }
    }
}

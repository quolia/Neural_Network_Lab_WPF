using System.Windows.Controls;

namespace Qualia.Controls
{
    public partial class DefaultSelectableItemPresenter : UserControl, ISelectableItem
    {
        public DefaultSelectableItemPresenter()
        {
            InitializeComponent();
        }

        public string Text => CtlText.Text;

        public string Value => Text;

        public Control Control => this;
    }
}

using System.Windows.Controls;
using Qualia.Controls.Base.Values;

namespace Qualia.Controls.Presenter;

public partial class DefaultSelectableItemPresenter : UserControl, ISelectableItem
{
    public DefaultSelectableItemPresenter()
    {
        InitializeComponent();
    }

    public DefaultSelectableItemPresenter(string name)
    {
        InitializeComponent();

        CtlText.Text = name;
    }

    // ISelectableItem

    public string Text => CtlText.Text;
    public string Value => Text;
    public Control Control => this;

    //
}
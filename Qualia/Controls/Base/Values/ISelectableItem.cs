using System.Windows.Controls;

namespace Qualia.Controls.Base.Values;

public interface ISelectableItem
{
    string Text { get; }
    string Value { get; }
    Control Control { get; }
}
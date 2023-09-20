using Qualia.Controls.ToolTips;
using Qualia.Tools.Functions;

namespace Qualia.Controls.Base.Values;

public sealed class SelectorControlWrapper
{
    private SelectorControl _selector;

    private SelectorControlWrapper(SelectorControl selector)
    {
        _selector = selector;
    }

    public static SelectorControlWrapper Wrap(SelectorControl selector)
    {
        return new SelectorControlWrapper(selector);
    }

    public static ISelectableItem GetSelectableItemForName<T>(string name) where T : class
    {
        var instance = BaseFunction<T>.GetInstanceByName(name);
        var type = typeof(T);
        return type == typeof(ActivationFunction) ? PresenterProvider.GetPresenter(instance as ActivationFunction, name) : PresenterProvider.GetDefaultSelectableItemPresenter(name);
    }

    public void Clear()
    {
        _selector.Items.Clear();
    }

    public void AddItem(ISelectableItem item)
    {
        _selector.Items.Add(item);
    }

    public string DefaultValue
    {
        get => _selector.DefaultValue;
    }

    public string Name
    {
        get => _selector.Name;
    }

    public int Count
    {
        get => _selector.Items.Count;
    }

    public bool Contains(ISelectableItem item)
    {
        return _selector.Items.Contains(item);
    }

    public ISelectableItem GetItemAt(int index)
    {
        return _selector.Items.GetItemAt(index) as ISelectableItem;
    }

    public ISelectableItem SelectedItem
    {
        get => _selector.SelectedItem;
        set => _selector.SelectedItem = value;
    }
}
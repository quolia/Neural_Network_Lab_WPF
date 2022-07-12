using Qualia.Controls;
using System;

namespace Qualia.Tools
{
    public static class SelectableItemsProvider
    {
        unsafe public static SelectableItem GetSelectableFunctionItem<T>(string name) where T : class
        {
            var instance = BaseFunction<T>.GetInstance(name);

            var control = new SelectableFunctionControl(name);
            control.Loaded += (sender, e) =>
            {
                control.DrawBase();
                //control.DrawFunction(x => (instance as BaseFunction<T>).Do(x, 1), in ColorsX.Red);
            };

            return new SelectableItem(control);
        }
    }
}

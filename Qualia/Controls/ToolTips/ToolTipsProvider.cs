using Qualia.Controls;
using System;

namespace Qualia.Tools
{
    public static class PresenterProvider
    {
        unsafe public static ISelectableItem GetPresenter(ActivationFunction instance, string name)
        {
            var control = new FunctionPresenter(name);
            control.Loaded += (sender, e) =>
            {
                control.CtlDescription.Text = name;
                control.DrawBase();
                control.DrawFunction(x => instance.Do(x, 1), in ColorsX.Red);
                control.DrawFunction(x => instance.Derivative(x, instance.Do(x, 1), 1), in ColorsX.Blue);
            };

            return control;
        }

        unsafe public static ISelectableItem GetPresenter(RandomizeFunction instance, string name)
        {
            var control = new FunctionPresenter(name);
            control.Loaded += (sender, e) =>
            {
                control.DrawBase();
                //control.DrawFunction(x => instance.Do(x, 1), in ColorsX.Red);
            };

            return control;
        }

        public static ISelectableItem GetDefaultSelectableItemPresenter(string name)
        {
            var control = new DefaultSelectableItemPresenter();
            control.CtlText.Text = name;
            return control;
        }
    }
}

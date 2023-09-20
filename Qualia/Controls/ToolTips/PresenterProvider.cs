using Qualia.Controls.Base.Values;
using Qualia.Controls.Presenter;
using Qualia.Tools;
using Qualia.Tools.Functions;

namespace Qualia.Controls.ToolTips;

public static class PresenterProvider
{
    public static unsafe ISelectableItem GetPresenter(ActivationFunction instance, string name)
    {
        var control = new FunctionPresenter(name);
        control.Loaded += (sender, e) =>
        {
            control.CtlDescription.Text = name;
            control.DrawBase();
            control.DrawFunction(x => instance.Do(x, 1), in ColorsX.Red, 0);
            control.DrawFunction(x => instance.Derivative(x, instance.Do(x, 1), 1), in ColorsX.Blue, 1);
        };

        return control;
    }

    public static unsafe ISelectableItem GetPresenter(RandomizeFunction instance, string name)
    {
        var control = new FunctionPresenter(name);
        control.Loaded += (sender, e) =>
        {
            control.DrawBase();
        };

        return control;
    }

    public static ISelectableItem GetDefaultSelectableItemPresenter(string name)
    {
        return new DefaultSelectableItemPresenter(name);
    }
}
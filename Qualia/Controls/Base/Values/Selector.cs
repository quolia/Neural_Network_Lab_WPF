using Qualia.Tools;
using System;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public class SelectorControl : ComboBox, IConfigParam
    {
        private Config _config;
        private event Action<Notification.ParameterChanged> _onChanged = delegate {};

        public string DefaultValue { get; set; }

        public SelectorControl Initialize(string defaultValue)
        {
            if (!string.IsNullOrEmpty(defaultValue))
            {
                DefaultValue = defaultValue;
            }

            return this;
        }

        public Notification.ParameterChanged UIParam { get; private set; }

        public SelectorControl SetUIParam(Notification.ParameterChanged param)
        {
            UIParam = param;
            return this;
        }

        //public SelectValueControl SetToolTip(SelectableItem toolTip)
        //{
            //ToolTip = toolTip;
        //    return this;
        //}

        public SelectorControl()
        {
            //ItemTemplate = Main.Instance.Resources["SelectableItemTemplate"] as DataTemplate;
            //Style = Main.Instance.Resources["SelectValueStyle"] as Style;
                 
            Padding = new(1);
            Margin = new(3);
            MinWidth = 60;

            //Background = Draw.GetBrush(ColorsX.Lime);

            //Background = Draw.GetBrush(in ColorsX.Yellow);
            //Foreground = Draw.GetBrush(in ColorsX.Green);
            //Resources.Add(SystemColors.WindowBrushKey, Draw.GetBrush(ColorsX.Yellow));
            //Resources.Add(SystemColors.HighlightBrushKey, Draw.GetBrush(ColorsX.Red));

            SelectionChanged += Value_OnChanged;
        }

        private void Value_OnChanged(object sender, SelectionChangedEventArgs e)
        {
             _onChanged(UIParam);
        }

        public new ISelectableItem SelectedItem
        {
            get => base.SelectedItem as ISelectableItem;
            set => base.SelectedItem = value;
        }

        public bool IsValid() => !IsNull();

        private bool IsNull() => SelectedItem == null;

        public ISelectableItem Value
        {
            get => IsValid() ? SelectedItem : throw new InvalidValueException(Name, Text);

            set
            {
                SelectedItem = value;
            }
        }

        // IConfigParam

        public void SetConfig(Config config)
        {
            _config = config;
        }

        public void LoadConfig()
        {
            Value = SelectedItem;
        }

        public void SaveConfig()
        {
            _config.Set(Name, Value.Text);
        }

        public void RemoveFromConfig()
        {
            _config.Remove(Name);
        }

        public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            _onChanged = onChanged;
        }

        public void InvalidateValue() => throw new InvalidOperationException();

        //

        public string ToXml()
        {
            string name = Config.PrepareParamName(Name);
            return $"<{name} Value=\"{Value}\" /> \n";
        }
    }

    sealed public class SelectorControlWrapper
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
            var instance = BaseFunction<T>.GetInstance(name);

            var type = typeof(T);

            if (type == typeof(ActivationFunction))
            {
                return PresenterProvider.GetPresenter(instance as ActivationFunction, name);
            }
            else
            {
                return PresenterProvider.GetDefaultSelectableItemPresenter(name);
                //throw new Exception($"Unknown type '{type.Name}' is trying to get presenter.");
            }
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

    public interface ISelectableItem
    {
        string Text { get; }
        string Value { get; }
        Control Control { get; }
    }
}

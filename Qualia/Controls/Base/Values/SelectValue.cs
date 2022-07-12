using Qualia.Tools;
using System;
using System.Windows.Controls;
using System.Linq;
using System.Windows;

namespace Qualia.Controls
{
    sealed public class SelectValueControl : ComboBox, IConfigParam
    {
        private Config _config;
        private event Action<Notification.ParameterChanged> _onChanged = delegate {};

        public string DefaultValue { get; set; }

        public SelectValueControl Initialize(string defaultValue)
        {
            if (!string.IsNullOrEmpty(defaultValue))
            {
                DefaultValue = defaultValue;
            }

            return this;
        }

        public Notification.ParameterChanged UIParam { get; private set; }

        public SelectValueControl SetUIParam(Notification.ParameterChanged param)
        {
            UIParam = param;
            return this;
        }

        public SelectValueControl SetToolTip(SelectableItem toolTip)
        {
            //ToolTip = toolTip;
            return this;
        }

        public SelectValueControl()
        {
            ItemTemplate = Main.Instance.Resources["SelectableItemTemplate"] as DataTemplate;

            Padding = new(1);
            Margin = new(3);
            MinWidth = 60;

            SelectionChanged += Value_OnChanged;
        }

        private void Value_OnChanged(object sender, SelectionChangedEventArgs e)
        {
             _onChanged(UIParam);
        }

        public new SelectableItem SelectedItem
        {
            get => base.SelectedItem as SelectableItem;
            set => base.SelectedItem = value;
        }

        public bool IsValid() => !IsNull();

        private bool IsNull() => SelectedItem == null;

        public SelectableItem Value
        {
            get => IsValid() ? SelectedItem : throw new InvalidValueException(Name, Text);

            set
            {
                SelectedItem = value as SelectableItem;
            }
        }


        public void SetConfig(Config config)
        {
            _config = config;
        }

        public void LoadConfig()
        {
            Value = SelectedItem;//.ToString();
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
            _onChanged -= onChanged;
            _onChanged += onChanged;
        }

        public void InvalidateValue() => throw new InvalidOperationException();

        public string ToXml()
        {
            string name = Config.PrepareParamName(Name);
            return $"<{name} Value=\"{Value}\" /> \n";
        }
    }

    sealed public class SelectValueWrapper
    {
        private SelectValueControl _control;

        private SelectValueWrapper(SelectValueControl control)
        {
            _control = control;
        }

        public static SelectValueWrapper Wrap(SelectValueControl control)
        {
            return new SelectValueWrapper(control);
        }

        public static SelectableItem GetSelectableItem<T>(string name) where T : class
        {
            return SelectableItemsProvider.GetSelectableFunctionItem<T>(name);
        }

        public void Clear()
        {
            _control.Items.Clear();
        }

        public void AddItem(SelectableItem item)
        {
            _control.Items.Add(item);
        }

        public string DefaultValue
        {
            get => _control.DefaultValue;
        }

        public string Name
        {
            get => _control.Name;
        }

        public int Count
        {
            get => _control.Items.Count;
        }

        public bool Contains(SelectableItem item)
        {
            return _control.Items.Contains(item);
        }

        public SelectableItem GetItemAt(int index)
        {
            return null;
        }

        public SelectableItem SelectedItem
        {
            get => _control.SelectedItem;
            set => _control.SelectedItem = value;
        }
    }

    public interface ISelectableItem
    {
        string Text { get; }
        string Value { get; }
        Control Control { get; }
    }

    sealed public class SelectableItem
    {
        public string Text => _item.Text;
        public string Value => _item.Value;

        public Control Control => _item.Control;


        private ISelectableItem _item;

        public SelectableItem(ISelectableItem item)
        {
            _item = item;
        }
    }
}

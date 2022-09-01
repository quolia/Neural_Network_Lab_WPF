using Qualia.Tools;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public class StringValueControl : TextBox, IConfigParam
    {
        public string DefaultValue { get; set; } = string.Empty;

        public bool IsEmptyAllowed { get; set; } = false;

        public StringValueControl Initialize(string defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }

        public StringValueControl()
        {
            Padding = new(0);
            Margin = new(3);

            TextChanged += Value_OnChanged;
        }

        private void Value_OnChanged(object sender, EventArgs e)
        {
            ApplyAction action = new(this)
            {
                CancelAction = () =>
                {
                    TextChanged -= Value_OnChanged;
                    Undo();
                    TextChanged += Value_OnChanged;

                    InvalidateValue();
                }
            };

            if (RevalidateValue())
            {
                this.InvokeUIHandler(Notification.ParameterChanged.Unknown, action);
            }
            else
            {
                this.InvokeUIHandler(Notification.ParameterChanged.Invalidate, action);
            }
        }

        public bool IsValid() => !IsNull();

        public bool IsNull() => string.IsNullOrEmpty(Text) && !IsEmptyAllowed;

        public string Value
        {
            get => IsValid() ? Text : throw new InvalidValueException(Name, Text);

            set
            {
                Text = value;
            }
        }

        // IConfigParam

        public void SetConfig(Config config)
        {
            this.PutConfig(config);
        }

        public void LoadConfig()
        {
            Value = this.GetConfig().Get(Name, DefaultValue);
        }

        public void SaveConfig()
        {
            this.GetConfig().Set(Name, Value);

            IsUndoEnabled = false;
            IsUndoEnabled = true;
        }

        public void RemoveFromConfig()
        {
            this.GetConfig().Remove(Name);
        }

        public void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChanged)
        {
            this.SetUIHandler(onChanged);
        }

        public bool RevalidateValue()
        {
            InvalidateValue();
            return IsValid();
        }

        public void InvalidateValue()
        {
            Background = IsValid() ? Brushes.White : Brushes.Tomato;
        }

        //

        public string ToXml()
        {
            string name = Config.PrepareParamName(Name);
            return $"<{name} Value=\"{Value}\" /> \n";
        }
    }
}

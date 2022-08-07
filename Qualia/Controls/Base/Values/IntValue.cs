using Qualia.Tools;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public class IntValueControl : TextBox, IConfigParam
    {
        public long DefaultValue { get; set; } = Constants.InvalidLong;

        public IntValueControl Initialize(long? defaultValue = null, long? minValue = null, long? maxValue = null)
        {
            if (defaultValue.HasValue)
            {
                DefaultValue = defaultValue.Value;
            }

            if (minValue.HasValue)
            {
                MinValue = minValue.Value;
            }

            if (maxValue.HasValue)
            {
                MaxValue = maxValue.Value;
            }

            return this;
        }

        public long MinValue { get; set; } = long.MinValue;

        public long MaxValue { get; set; } = long.MaxValue;

        public IntValueControl()
        {
            Padding = new(0);
            Margin = new(3);
            //MinWidth = 30;

            TextChanged += Value_OnChanged;
        }

        private void Value_OnChanged(object sender, EventArgs e)
        {
            ApplyAction action = new()
            {
                CancelAction = () =>
                {
                    TextChanged -= Value_OnChanged;
                    Undo();
                    TextChanged += Value_OnChanged;
                }
            };

            if (IsValidInput(Constants.InvalidLong))
            {
                this.InvokeUIHandler(action: action);
                return;
            }

            ActionsManager.Instance.Add(action);
        }

        private bool IsValidInput(long defaultValue)
        {
            try
            {
                if (!IsNull(defaultValue))
                {
                    var value = Converter.TextToInt(Text, defaultValue);
                    if (value >= MinValue && value <= MaxValue)
                    {
                        Background = Brushes.White;
                        return true;
                    }
                }
            }
            catch
            {
                //
            }

            InvalidateValue();
            return false;
        }

        public bool IsValid()
        {
            return IsValidInput(DefaultValue);
        }

        private bool IsNull(long defaultValue) => string.IsNullOrEmpty(Text) && Constants.IsInvalid(defaultValue);

        public long Value
        {
            get
            {
                if (string.IsNullOrEmpty(Text) && !Constants.IsInvalid(DefaultValue))
                {
                    Text = Converter.IntToText(DefaultValue);
                }

                return IsValid()
                       ? Converter.TextToInt(Text, DefaultValue)
                       : throw new InvalidValueException(Name, Text);
            }

            set
            {
                Text = Converter.IntToText(value);
            }
        }

        // IConfigParam

        public void SetConfig(Config config)
        {
            this.PutConfig(config);
        }

        public void LoadConfig()
        {
            var value = this.GetConfig().Get(Name, DefaultValue);

            if (value < MinValue)
            {
                value = MinValue;
            }

            if (value > MaxValue)
            {
                value = MaxValue;
            }

            Value = value;
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

        public void SetOnChangeEvent(ActionsManager.ApplyActionDelegate onChanged)
        {
            this.SetUIHandler(onChanged);
        }

        public void InvalidateValue()
        {
            Background = Brushes.Tomato;
        }

        //

        public string ToXml()
        {
            string name = Config.PrepareParamName(Name);
            return $"<{name} Value=\"{Value}\" /> \n";
        }
    }
}


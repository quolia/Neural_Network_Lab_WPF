using Qualia.Tools;
using System;
using System.Linq;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public partial class DotsCountControl : BaseUserControl
    {
        public DotsCountControl()
        {
            InitializeComponent();
        }

        public int InputCount => (int)CtlInputCount.Value;
        public int MaxNumber => (int)CtlMaxNumber.Value;
        public int MinNumber => (int)CtlMinNumber.Value;

        private void Parameter_OnChanged(Notification.ParameterChanged param)
        {
            if (IsValid())
            {
                OnChanged(param);
            }
        }

        public override void SetConfig(Config config)
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetConfig(config));
        }

        public override void LoadConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.LoadConfig());
        }

        public override void SaveConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.RemoveFromConfig());
        }

        public override bool IsValid()
        {
            if (this.FindVisualChildren<IConfigParam>().All(param => param.IsValid()))
            {
                if (CtlInputCount.Value >= CtlMaxNumber.Value)
                {
                    return true;
                }

                CtlInputCount.InvalidateValue();
            }

            return false;
        }

        public override void SetOnChangeEvent(Action<Notification.ParameterChanged> onChange)
        {
            _onChanged -= onChange;
            _onChanged += onChange;

            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetOnChangeEvent(Parameter_OnChanged));
        }

        public override void InvalidateValue() => throw new InvalidOperationException();
    }
}

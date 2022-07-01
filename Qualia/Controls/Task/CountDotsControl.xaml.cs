using Qualia.Tools;
using System;
using System.Linq;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public partial class CountDotsControl : BaseUserControl
    {
        private event Action OnChange = delegate { };

        public CountDotsControl()
        {
            InitializeComponent();
        }

        public int InputCount => (int)CtlInputCount.Value;
        public int MaxNumber => (int)CtlMaxNumber.Value;
        public int MinNumber => (int)CtlMinNumber.Value;

        private void Parameter_OnChanged()
        {
            if (IsValid())
            {
                OnChange();
            }
        }

        public void SetConfig(Config config)
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetConfig(config));
        }

        public void LoadConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.LoadConfig());
        }

        public void SaveConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SaveConfig());
        }

        public void RemoveFromConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.RemoveFromConfig());
        }

        public bool IsValid()
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

        public void AddChangeEventListener(Action onChange)
        {
            OnChange -= onChange;
            OnChange += onChange;

            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.AddChangeEventListener(Parameter_OnChanged));
        }

        public void InvalidateValue() => throw new InvalidOperationException();
    }
}

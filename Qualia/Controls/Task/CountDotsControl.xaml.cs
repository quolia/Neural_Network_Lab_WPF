using Qualia.Tools;
using System;
using System.Linq;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public partial class CountDotsControl : UserControl, IConfigParam
    {
        private event Action OnChange = delegate { };

        public CountDotsControl()
        {
            InitializeComponent();
        }

        public int InputCount => (int)CtlTask_CountDots_InputCount.Value;
        public bool IsGaussianDistribution => CtlTask_CountDots_IsGaussianDistribution.IsOn;
        public int MaxNumber => (int)CtlTask_CountDots_MaxNumber.Value;
        public int MinNumber => (int)CtlTask_CountDots_MinNumber.Value;

        private void Changed()
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

        public void VanishConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.VanishConfig());
        }

        public bool IsValid()
        {
            if (this.FindVisualChildren<IConfigParam>().All(param => param.IsValid()))
            {
                if (CtlTask_CountDots_InputCount.Value >= CtlTask_CountDots_MaxNumber.Value)
                {
                    return true;
                }

                CtlTask_CountDots_InputCount.InvalidateValue();
            }

            return false;
        }

        public void SetChangeEvent(Action onChange)
        {
            OnChange -= onChange;
            OnChange += onChange;

            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetChangeEvent(Changed));
        }

        public void InvalidateValue() => throw new InvalidOperationException();
    }
}

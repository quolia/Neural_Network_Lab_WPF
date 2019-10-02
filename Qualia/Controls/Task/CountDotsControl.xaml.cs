using System;
using System.Linq;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public partial class CountDotsControl : UserControl, IConfigValue
    {
        Config Config;

        event Action OnChange = delegate { };

        public CountDotsControl()
        {
            InitializeComponent();
        }

        public int InputCount => (int)CtlTaskInputCount.Value;
        public bool IsGaussianDistribution => CtlIsGaussianDistribution.IsOn;
        public int MaxNumber => (int)CtlTaskMaxNumber.Value;
        public int MinNumber => (int)CtlTaskMinNumber.Value;

        private void Changed()
        {
            if (IsValid())
            {
                OnChange();
            }
        }

        public void SetConfig(Config config)
        {
            Config = config;
            Range.ForEach(this.FindVisualChildren<IConfigValue>(), c => c.SetConfig(config));
        }

        public void LoadConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigValue>(), c => c.LoadConfig());
        }

        public void SaveConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigValue>(), c => c.SaveConfig());
        }

        public void VanishConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigValue>(), c => c.VanishConfig());
        }

        public bool IsValid()
        {
            if (this.FindVisualChildren<IConfigValue>().All(c => c.IsValid()))
            {
                if (CtlTaskInputCount.Value >= CtlTaskMaxNumber.Value)
                {
                    return true;
                }

                CtlTaskInputCount.InvalidateValue();
            }
            return false;
        }

        public void SetChangeEvent(Action onChange)
        {
            OnChange -= onChange;
            OnChange += onChange;
            Range.ForEach(this.FindVisualChildren<IConfigValue>(), c => c.SetChangeEvent(Changed));
        }

        public void InvalidateValue()
        {
            throw new NotImplementedException();
        }
    }
}

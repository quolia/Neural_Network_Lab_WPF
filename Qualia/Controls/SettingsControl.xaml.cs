using System;
using System.Linq;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public partial class SettingsControl : UserControl, IConfigValue
    {
        private Config _config;

        private event Action Changed = delegate { };

        private readonly object _locker = new object();
        private Settings _settings;

        public Settings Settings
        {
            get
            {
                lock (_locker)
                {
                    return _settings;
                }
            }

            set
            {
                lock (_locker)
                {
                    _settings = value;
                }
            }
        }

        public SettingsControl()
        {
            InitializeComponent();
        }

        public void SetConfig(Config config)
        {
            _config = config;
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigValue>(), c => c.SetConfig(config));
        }

        public void LoadConfig()
        {
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigValue>(), c => c.LoadConfig());
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigValue>(), c => c.SetChangeEvent(OnChanged));
            OnChanged();
        }

        public void SaveConfig()
        {
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigValue>(), c => c.SaveConfig());
            _config.FlushToDrive();
        }

        public void VanishConfig()
        {
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigValue>(), c => c.VanishConfig());
            _config.FlushToDrive();
        }

        public bool IsValid()
        {
            return CtlPanel.FindVisualChildren<IConfigValue>().All(c => c.IsValid());
        }

        public void SetChangeEvent(Action action)
        {
            Changed -= action;
            Changed += action;
        }

        private void OnChanged()
        {
            var settings = new Settings
            {
                SkipRoundsToDrawErrorMatrix = (int)CtlSkipRoundsToDrawErrorMatrix.Value,
                SkipRoundsToDrawNetworks = (int)CtlSkipRoundsToDrawNetworks.Value,
                SkipRoundsToDrawStatistics = (int)CtlSkipRoundsToDrawStatistics.Value
            };
            Settings = settings;

            Changed();
        }

        public void InvalidateValue()
        {
            throw new NotImplementedException();
        }
    }

    public class Settings
    {
        public int SkipRoundsToDrawErrorMatrix;
        public int SkipRoundsToDrawNetworks;
        public int SkipRoundsToDrawStatistics;
    }
}

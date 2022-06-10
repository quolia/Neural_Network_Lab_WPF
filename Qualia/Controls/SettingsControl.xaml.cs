using System;
using System.Linq;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    sealed public partial class SettingsControl : UserControl, IConfigParam
    {
        private Config _config;

        private event Action Changed = delegate { };

        private readonly object _locker = new object();
        private Settings _settings;

        public Settings Settings
        {
            get
            {
                //lock (_locker)
                {
                    return _settings;
                }
            }

            set
            {
                //lock (_locker)
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
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigParam>(), param => param.SetConfig(config));
        }

        public void LoadConfig()
        {
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigParam>(), param => param.LoadConfig());
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigParam>(), param => param.SetChangeEvent(OnChanged));

            OnChanged();
        }

        public void SaveConfig()
        {
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigParam>(), param => param.SaveConfig());
            _config.FlushToDrive();
        }

        public void VanishConfig()
        {
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigParam>(), param => param.VanishConfig());
            _config.FlushToDrive();
        }

        public bool IsValid()
        {
            return CtlPanel.FindVisualChildren<IConfigParam>().All(param => param.IsValid());
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

        public void InvalidateValue() => throw new InvalidOperationException();
    }

    sealed public class Settings
    {
        public int SkipRoundsToDrawErrorMatrix;
        public int SkipRoundsToDrawNetworks;
        public int SkipRoundsToDrawStatistics;
    }
}

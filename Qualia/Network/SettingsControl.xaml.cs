using Qualia.Tools;
using System;
using System.Linq;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public partial class SettingsControl : UserControl, IConfigParam
    {
        private Config _config;

        private event Action Changed = delegate { };

        private readonly object _locker = new();
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

            CtlSkipRoundsToDrawErrorMatrix.Initialize(defaultValue: 10000);
            CtlSkipRoundsToDrawNetworks.Initialize(defaultValue: 10000);
            CtlSkipRoundsToDrawStatistics.Initialize(defaultValue: 10000);
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

        public void RemoveFromConfig()
        {
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigParam>(), param => param.RemoveFromConfig());
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
            Settings settings = new()
            {
                SkipRoundsToDrawErrorMatrix = (int)CtlSkipRoundsToDrawErrorMatrix.Value,
                SkipRoundsToDrawNetworks = (int)CtlSkipRoundsToDrawNetworks.Value,
                SkipRoundsToDrawStatistics = (int)CtlSkipRoundsToDrawStatistics.Value
            };
            Settings = settings;

            Changed();
        }

        public void InvalidateValue() => throw new InvalidOperationException();

        public string ToXml()
        {
            string name = Config.PrepareParamName(Name);
            return $"<{name} /> \n";
        }
    }

    sealed public class Settings
    {
        public int SkipRoundsToDrawErrorMatrix;
        public int SkipRoundsToDrawNetworks;
        public int SkipRoundsToDrawStatistics;
    }
}

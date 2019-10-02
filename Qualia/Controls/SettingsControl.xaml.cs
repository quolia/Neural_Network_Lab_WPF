using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tools;

namespace Qualia.Controls
{
    public partial class SettingsControl : UserControl, IConfigValue
    {
        Config Config;

        event Action Changed = delegate { };

        object Locker = new object();
        Settings _Settings;
        public Settings Settings
        {
            get
            {
                lock (Locker)
                {
                    return _Settings;
                }
            }

            set
            {
                lock (Locker)
                {
                    _Settings = value;
                }
            }
        }

        public SettingsControl()
        {
            InitializeComponent();
        }

        public void SetConfig(Config config)
        {
            Config = config;
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
            Config.FlushToDrive();
        }

        public void VanishConfig()
        {
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigValue>(), c => c.VanishConfig());
            Config.FlushToDrive();
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

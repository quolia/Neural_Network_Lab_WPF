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

        public void Load(Config config)
        {
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigValue>(), c => c.Load(config));
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigValue>(), c => c.SetChangeEvent(OnChanged));
            OnChanged();
        }

        public void Save(Config config)
        {
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigValue>(), c => c.Save(config));
            config.FlushToDrive();
        }

        public void Vanish(Config config)
        {
            Range.ForEach(CtlPanel.FindVisualChildren<IConfigValue>(), c => c.Vanish(config));
            config.FlushToDrive();
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
            var settings = new Settings();
            settings.SkipRoundsToDrawErrorMatrix = CtlSkipRoundsToDrawErrorMatrix.Value;
            settings.SkipRoundsToDrawNetworks = CtlSkipRoundsToDrawNetworks.Value;
            settings.SkipRoundsToDrawStatistic = CtlSkipRoundsToDrawStatistic.Value;
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
        public int SkipRoundsToDrawStatistic;
    }
}

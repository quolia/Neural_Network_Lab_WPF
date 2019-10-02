using Microsoft.Win32;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public partial class MNISTControl : UserControl, IConfigValue
    {
        Config Config;

        event Action OnChange = delegate { };

        public MNISTControl()
        {
            InitializeComponent();
        }

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
            return this.FindVisualChildren<IConfigValue>().All(c => c.IsValid());
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

        private void CtlBrowseImagesPath_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BrowseFile(CtlMNISTImagesPath, "images.bin");
        }

        private void CtlBrowseLabelsPath_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BrowseFile(CtlMNISTLabelsPath, "labels.bin");
        }

        private void BrowseFile(TextBox control, string targetName)
        {
            var file = BrowseFile();
            if (!String.IsNullOrEmpty(file))
            {
                try
                {
                    Decompress(file, Path.GetDirectoryName(file) + Path.DirectorySeparatorChar + targetName);
                    control.Text = file;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot unzip file with the following message:\r\n\r\n" + ex.Message);
                }
            }
        }

        private string BrowseFile()
        {
            var loadDialog = new OpenFileDialog
            {
                InitialDirectory = Path.GetFullPath("."),
                DefaultExt = "gz",
                Filter = "WinZip files (*.gz)|*.gz|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            };
            {
                if (loadDialog.ShowDialog() == true)
                {
                    return loadDialog.FileName;
                }
            }

            return null;
        }

        private void Decompress(string sourceGz, string destBin)
        {
            using (var srcStream = File.OpenRead(sourceGz))
            using (var targetStream = File.OpenWrite(destBin))
            {
                using (GZipStream decompressionStream = new GZipStream(srcStream, CompressionMode.Decompress, false))
                {
                    decompressionStream.CopyTo(targetStream);
                }
            }
        }
    }
}

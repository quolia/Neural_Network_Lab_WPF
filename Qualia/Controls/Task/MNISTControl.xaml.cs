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
        event Action OnChange = delegate { };

        public MNISTControl()
        {
            InitializeComponent();
        }

        public int MaxNumber => CtlTaskMaxNumber.Value;
        public int MinNumber => CtlTaskMinNumber.Value;

        private void Changed()
        {
            if (IsValid())
            {
                OnChange();
            }
        }

        public void Load(Config config)
        {
            Range.ForEach(this.FindVisualChildren<IConfigValue>(), c => c.Load(config));
        }

        public void Save(Config config)
        {
            Range.ForEach(this.FindVisualChildren<IConfigValue>(), c => c.Save(config));
        }

        public void Vanish(Config config)
        {
            Range.ForEach(this.FindVisualChildren<IConfigValue>(), c => c.Vanish(config));
        }

        public bool IsValid()
        {
            if (this.FindVisualChildren<IConfigValue>().All(c => c.IsValid()))
            {
                return true;
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

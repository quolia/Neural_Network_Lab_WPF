using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Tools;

namespace Qualia.Controls
{
    public partial class MNISTControl : UserControl, IConfigValue
    {
        public List<MNISTImage> Images = new List<MNISTImage>();

        private Config _config;

        private event Action OnChange = delegate { };

        public MNISTControl()
        {
            InitializeComponent();
        }

        public int MaxNumber => (int)CtlMNISTMaxNumber.Value;
        public int MinNumber => (int)CtlMNISTMinNumber.Value;

        private void Changed()
        {
            if (IsValid())
            {
                OnChange();
            }
        }

        public void SetConfig(Config config)
        {
            _config = config;
            Range.ForEach(this.FindVisualChildren<IConfigValue>(), c => c.SetConfig(config));
        }

        public void LoadConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigValue>(), c => c.LoadConfig());

            if (!File.Exists(CtlMNISTImagesPath.Text) || !File.Exists(CtlMNISTLabelsPath.Text))
            {
                CtlMNISTImagesPath.Text = string.Empty;
                CtlMNISTLabelsPath.Text = string.Empty;
            }

            if (string.IsNullOrEmpty(CtlMNISTImagesPath.Text))
            {
                return;
            }

            var fileName = Path.GetDirectoryName(CtlMNISTImagesPath.Text) + Path.DirectorySeparatorChar + "images.bin";
            if (!File.Exists(fileName))
            {
                try
                {
                    Decompress(CtlMNISTImagesPath.Text, Path.GetDirectoryName(CtlMNISTImagesPath.Text) + Path.DirectorySeparatorChar + "images.bin");
                }
                catch (Exception ex)
                {
                    CtlMNISTImagesPath.Text = string.Empty;
                    MessageBox.Show("Cannot unzip file with the following message:\r\n\r\n" + ex.Message);
                }
            }

            LoadImages(fileName);

            if (string.IsNullOrEmpty(CtlMNISTLabelsPath.Text))
            {
                return;
            }

            fileName = Path.GetDirectoryName(CtlMNISTLabelsPath.Text) + Path.DirectorySeparatorChar + "labels.bin";
            if (!File.Exists(fileName))
            {
                try
                {
                    Decompress(CtlMNISTLabelsPath.Text, Path.GetDirectoryName(CtlMNISTLabelsPath.Text) + Path.DirectorySeparatorChar + "labels.bin");
                }
                catch (Exception ex)
                {
                    CtlMNISTLabelsPath.Text = string.Empty;
                    MessageBox.Show("Cannot unzip file with the following message:\r\n\r\n" + ex.Message);
                }
            }

            LoadLabels(fileName);
        }

        private void LoadImages(string fileName)
        {
            Images.Clear();

            if (!File.Exists(fileName))
            {
                return;
            }

            var buffer = new byte[4];
            using (var file = File.OpenRead(fileName))
            {
                if (file.Read(buffer, 0, buffer.Length) != buffer.Length)
                {
                    throw new Exception("Invalid MNIST images file format.");
                }
                    
                if (BitConverter.IsLittleEndian)
                {
                    buffer = buffer.Reverse().ToArray();
                }

                int magicNumber = BitConverter.ToInt32(buffer, 0);
                if (magicNumber != 2051)
                {
                    throw new Exception("Invalid MNIST images file format.");
                }

                if (file.Read(buffer, 0, buffer.Length) != buffer.Length)
                {
                    throw new Exception("Invalid MNIST images file format.");
                }

                if (BitConverter.IsLittleEndian)
                {
                    buffer = buffer.Reverse().ToArray();
                }

                int numberOfImages = BitConverter.ToInt32(buffer, 0);

                if (file.Read(buffer, 0, buffer.Length) != buffer.Length)
                {
                    throw new Exception("Invalid MNIST images file format.");
                }

                if (BitConverter.IsLittleEndian)
                {
                    buffer = buffer.Reverse().ToArray();
                }

                int numberOfRows = BitConverter.ToInt32(buffer, 0);
                if (numberOfRows != 28)
                {
                    throw new Exception("Invalid MNIST images file format.");
                }

                if (file.Read(buffer, 0, buffer.Length) != buffer.Length)
                {
                    throw new Exception("Invalid MNIST images file format.");
                }

                if (BitConverter.IsLittleEndian)
                {
                    buffer = buffer.Reverse().ToArray();
                }

                int numberOfColumns = BitConverter.ToInt32(buffer, 0);
                if (numberOfColumns != 28)
                {
                    throw new Exception("Invalid MNIST images file format.");
                }

                for (int ind = 0; ind < numberOfImages; ++ind)
                {
                    var image = new MNISTImage();
                    if (file.Read(image.Image, 0, image.Image.Length) != image.Image.Length)
                    {
                        Images.Clear();
                        throw new Exception("Invalid MNIST images file format.");
                    }

                    Images.Add(image);
                }
            }
        }

        private void LoadLabels(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            var buffer = new byte[4];
            using (var file = File.OpenRead(fileName))
            {
                if (file.Read(buffer, 0, buffer.Length) != buffer.Length)
                {
                    throw new Exception("Invalid MNIST labels file format.");
                }

                if (BitConverter.IsLittleEndian)
                {
                    buffer = buffer.Reverse().ToArray();
                }

                int magicNumber = BitConverter.ToInt32(buffer, 0);
                if (magicNumber != 2049)
                {
                    throw new Exception("Invalid MNIST labels file format.");
                }

                if (file.Read(buffer, 0, buffer.Length) != buffer.Length)
                {
                    throw new Exception("Invalid MNIST labels file format.");
                }

                if (BitConverter.IsLittleEndian)
                {
                    buffer = buffer.Reverse().ToArray();
                }

                int numberOfImages = BitConverter.ToInt32(buffer, 0);
                if (numberOfImages != Images.Count)
                {
                    throw new Exception("Invalid MNIST labels file format.");
                }

                for (int ind = 0; ind < numberOfImages; ++ind)
                {
                    var image = Images[ind];
                    image.Label = (byte)file.ReadByte();
                }
            }
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
            if (string.IsNullOrEmpty(file))
            {
                return;
            }

            try
            {
                Decompress(file, Path.GetDirectoryName(file) + Path.DirectorySeparatorChar + targetName);
                control.Text = file;
            }
            catch (Exception ex)
            {
                control.Text = string.Empty;
                MessageBox.Show("Cannot unzip file with the following message:\r\n\r\n" + ex.Message);
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
            using (var decompressionStream = new GZipStream(srcStream, CompressionMode.Decompress, false))
            {
                decompressionStream.CopyTo(targetStream);
            }
        }
    }

    public class MNISTImage
    {
        public byte[] Image = new byte[28 * 28];
        public byte Label;
    }
}

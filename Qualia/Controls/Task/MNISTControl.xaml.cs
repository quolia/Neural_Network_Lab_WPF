using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    sealed public partial class MNISTControl : UserControl, IConfigParam
    {
        public List<MNISTImage> Images = new();

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
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetConfig(config));
        }

        public void LoadConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.LoadConfig());

            var fileNameImagesBin = Path.GetDirectoryName(CtlMNISTImagesPath.Text) + Path.DirectorySeparatorChar + "images.bin";
            if (!File.Exists(fileNameImagesBin))
            {
                if (!File.Exists(CtlMNISTImagesPath.Text))
                {
                    CtlMNISTImagesPath.Text = App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "train-images-idx3-ubyte.gz";
                }

                fileNameImagesBin = Path.GetDirectoryName(CtlMNISTImagesPath.Text) + Path.DirectorySeparatorChar + "images.bin";
                if (!File.Exists(fileNameImagesBin))
                {
                    fileNameImagesBin = App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "images.bin";
                    if (!File.Exists(fileNameImagesBin))
                    {
                        try
                        {
                            if (!File.Exists(CtlMNISTImagesPath.Text))
                            {
                                throw new Exception($"Cannot find file '{CtlMNISTImagesPath.Text}'.");
                            }

                            Decompress(CtlMNISTImagesPath.Text, fileNameImagesBin);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Cannot open MNIST images file.\r\n\r\n" + ex.Message);
                            return;
                        }
                    }
                }
            }

            LoadImages(fileNameImagesBin);


            var fileNameLabelsBin = Path.GetDirectoryName(CtlMNISTLabelsPath.Text) + Path.DirectorySeparatorChar + "labels.bin";
            if (!File.Exists(fileNameLabelsBin))
            {
                if (!File.Exists(CtlMNISTLabelsPath.Text))
                {
                    CtlMNISTLabelsPath.Text = App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "train-labels-idx1-ubyte.gz";
                }

                fileNameLabelsBin = Path.GetDirectoryName(CtlMNISTLabelsPath.Text) + Path.DirectorySeparatorChar + "labels.bin";
                if (!File.Exists(fileNameLabelsBin))
                {
                    fileNameLabelsBin = App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "labels.bin";
                    if (!File.Exists(fileNameLabelsBin))
                    {
                        try
                        {
                            if (!File.Exists(CtlMNISTLabelsPath.Text))
                            {
                                throw new Exception($"Cannot find file '{CtlMNISTLabelsPath.Text}'.");
                            }

                            Decompress(CtlMNISTLabelsPath.Text, fileNameLabelsBin);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Cannot open MNIST labels file.\r\n\r\n" + ex.Message);
                            return;
                        }
                    }
                }
            }

            LoadLabels(fileNameLabelsBin);
        }

        private void LoadImages(string fileName)
        {
            Images.Clear();

            if (!File.Exists(fileName))
            {
                return;
            }

            var buffer = new byte[4];

            using var file = File.OpenRead(fileName);

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

            for (int i = 0; i < numberOfImages; ++i)
            {
                MNISTImage image = new();
                if (file.Read(image.Image, 0, image.Image.Length) != image.Image.Length)
                {
                    Images.Clear();
                    throw new Exception("Invalid MNIST images file format.");
                }

                Images.Add(image);
            }
        }

        private void LoadLabels(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            var buffer = new byte[4];

            using var file = File.OpenRead(fileName);

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

            for (int i = 0; i < numberOfImages; ++i)
            {
                var image = Images[i];
                image.Label = (byte)file.ReadByte();
            }
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
            return this.FindVisualChildren<IConfigParam>().All(param => param.IsValid());
        }

        public void SetChangeEvent(Action onChange)
        {
            OnChange -= onChange;
            OnChange += onChange;

            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetChangeEvent(Changed));
        }

        public void InvalidateValue() => throw new InvalidOperationException();

        private void CtlBrowseImagesPath_Click(object sender, RoutedEventArgs e)
        {
            BrowseFile(CtlMNISTImagesPath, "images.bin");
        }

        private void CtlBrowseLabelsPath_Click(object sender, RoutedEventArgs e)
        {
            BrowseFile(CtlMNISTLabelsPath, "labels.bin");
        }

        private void BrowseFile(TextBox ctlTextBox, string targetFileName)
        {
            var fileName = BrowseGzFile();
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            try
            {
                Decompress(fileName, Path.GetDirectoryName(fileName) + Path.DirectorySeparatorChar + targetFileName);
                ctlTextBox.Text = fileName;
            }
            catch (Exception ex)
            {
                ctlTextBox.Text = string.Empty;
                MessageBox.Show("Cannot unzip file with the following message:\r\n\r\n" + ex.Message);
            }
        }

        private string BrowseGzFile()
        {
            OpenFileDialog loadDialog = new()
            {
                InitialDirectory = Path.GetFullPath("."),
                DefaultExt = "gz",
                Filter = "WinZip files (*.gz)|*.gz|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (loadDialog.ShowDialog() == true)
            {
                return loadDialog.FileName;
            }

            return null;
        }

        private void Decompress(string sourceGz, string destBin)
        {
            using var srcStream = File.OpenRead(sourceGz);
            using var targetStream = File.OpenWrite(destBin);
            using GZipStream decompressionStream = new(srcStream, CompressionMode.Decompress, false);
            decompressionStream.CopyTo(targetStream);
        }
    }

    sealed public class MNISTImage
    {
        public byte[] Image = new byte[28 * 28];
        public byte Label;
    }
}

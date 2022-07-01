using Microsoft.Win32;
using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public partial class MNISTControl : BaseUserControl
    {
        public readonly List<MNISTImage> Images = new();

        public MNISTControl()
        {
            InitializeComponent();

            CtlImagesPath.Initialize(App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "train-images-idx3-ubyte.gz");
            CtlLabelsPath.Initialize(App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "train-labels-idx1-ubyte.gz");
        }

        public int MaxNumber => (int)CtlMaxNumber.Value;
        public int MinNumber => (int)CtlMinNumber.Value;

        private void Parameter_OnChanged(Notification.ParameterChanged param)
        {
            if (IsValid())
            {
                OnChanged(param);
            }
        }

        public override void SetConfig(Config config)
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetConfig(config));
        }

        public override void LoadConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.LoadConfig());

            var fileNameImagesBin = Extension.GetDirectoryName(CtlImagesPath.Text,
                                                               App.WorkingDirectory + "MNIST") + Path.DirectorySeparatorChar + "images.bin";
            
            if (!File.Exists(fileNameImagesBin))
            {
                if (!File.Exists(CtlImagesPath.Text))
                {
                    CtlImagesPath.Text = App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "train-images-idx3-ubyte.gz";
                }

                fileNameImagesBin = Extension.GetDirectoryName(CtlImagesPath.Text,
                                                               App.WorkingDirectory + "MNIST") + Path.DirectorySeparatorChar + "images.bin";

                if (!File.Exists(fileNameImagesBin))
                {
                    fileNameImagesBin = App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "images.bin";
                    if (!File.Exists(fileNameImagesBin))
                    {
                        try
                        {
                            if (!File.Exists(CtlImagesPath.Text))
                            {
                                throw new Exception($"Cannot find file '{CtlImagesPath.Text}'.");
                            }

                            Decompress(CtlImagesPath.Text, fileNameImagesBin);
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
            var fileNameImagesGz = Extension.GetDirectoryName(fileNameImagesBin,
                                                              App.WorkingDirectory + "MNIST") + Path.DirectorySeparatorChar + "train-images-idx3-ubyte.gz";

            CtlImagesPath.Text = fileNameImagesGz;

            //


            var fileNameLabelsBin = Extension.GetDirectoryName(CtlLabelsPath.Text,
                                                               App.WorkingDirectory + "MNIST") + Path.DirectorySeparatorChar + "labels.bin";

            if (!File.Exists(fileNameLabelsBin))
            {
                if (!File.Exists(CtlLabelsPath.Text))
                {
                    CtlLabelsPath.Text = App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "train-labels-idx1-ubyte.gz";
                }

                fileNameLabelsBin = Extension.GetDirectoryName(CtlLabelsPath.Text,
                                                               App.WorkingDirectory + "MNIST") + Path.DirectorySeparatorChar + "labels.bin";

                if (!File.Exists(fileNameLabelsBin))
                {
                    fileNameLabelsBin = App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "labels.bin";
                    if (!File.Exists(fileNameLabelsBin))
                    {
                        try
                        {
                            if (!File.Exists(CtlLabelsPath.Text))
                            {
                                throw new Exception($"Cannot find file '{CtlLabelsPath.Text}'.");
                            }

                            Decompress(CtlLabelsPath.Text, fileNameLabelsBin);
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
            var fileNameLabelsGz = Extension.GetDirectoryName(fileNameLabelsBin,
                                                              App.WorkingDirectory + "MNIST") + Path.DirectorySeparatorChar + "train-labels-idx1-ubyte.gz";

            CtlLabelsPath.Text = fileNameLabelsGz;
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

        public override void SaveConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.RemoveFromConfig());
        }

        public override bool IsValid()
        {
            return this.FindVisualChildren<IConfigParam>().All(param => param.IsValid());
        }

        public override void SetOnChangeEvent(Action<Notification.ParameterChanged> onChange)
        {
            _onChanged -= onChange;
            _onChanged += onChange;

            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetOnChangeEvent(Parameter_OnChanged));
        }

        public override void InvalidateValue() => throw new InvalidOperationException();

        private void BrowseImagesPath_OnClick(object sender, RoutedEventArgs e)
        {
            BrowseFile(CtlImagesPath, "images.bin");
        }

        private void BrowseLabelsPath_OnClick(object sender, RoutedEventArgs e)
        {
            BrowseFile(CtlLabelsPath, "labels.bin");
        }

        private void BrowseFile(TextBox textBox, string targetFileName)
        {
            var fileName = BrowseGzFile();
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            try
            {
                Decompress(fileName, Path.GetDirectoryName(fileName) + Path.DirectorySeparatorChar + targetFileName);
                textBox.Text = fileName;
            }
            catch (Exception ex)
            {
                textBox.Text = string.Empty;
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
        public readonly byte[] Image = new byte[28 * 28];
        public byte Label;
    }
}

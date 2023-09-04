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

        private string _imagesGzPath = App.WorkingDirectory + "Datasets" + Path.DirectorySeparatorChar + "MNIST" + Path.DirectorySeparatorChar + "train-images-idx3-ubyte.gz";
        private string _labelsGzPath = App.WorkingDirectory + "Datasets" + Path.DirectorySeparatorChar + "MNIST" + Path.DirectorySeparatorChar + "train-labels-idx1-ubyte.gz";

        private string _imagesPath = App.WorkingDirectory + "Datasets" + Path.DirectorySeparatorChar + "MNIST" + Path.DirectorySeparatorChar + "images.bin";
        private string _labelsPath = App.WorkingDirectory + "Datasets" + Path.DirectorySeparatorChar + "MNIST" + Path.DirectorySeparatorChar + "labels.bin";

        public MNISTControl()
            : base(0)
        {
            InitializeComponent();
        }

        public int MaxNumber => (int)CtlMaxNumber.Value;
        public int MinNumber => (int)CtlMinNumber.Value;

        private void Parameter_OnChanged(ApplyAction action)
        {
            if (action.Param == Notification.ParameterChanged.Invalidate)
            {
                InvalidateValue();
                OnChanged(action);
                return;
            }

            bool isValid = IsValid();
            if (!isValid)
            {
                action.Param = Notification.ParameterChanged.Invalidate;
            }

            OnChanged(action);
        }

        // IConfigParam

        override public void SetConfig(Config config)
        {
            Qualia.Tools.Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetConfig(config));
        }

        override public void LoadConfig()
        {
            Qualia.Tools.Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.LoadConfig());

            if (!File.Exists(_imagesPath))
            {
                try
                {
                    if (!File.Exists(_imagesGzPath))
                    {
                        throw new Exception($"Cannot find file \"{_imagesGzPath}\".");
                    }

                    Decompress(_imagesGzPath, _imagesPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot open MNIST images file.\r\n\r\n" + ex.Message);
                    return;
                }
            }

            LoadImages(_imagesPath);

            //


            if (!File.Exists(_labelsPath))
            {
                try
                {
                    if (!File.Exists(_labelsGzPath))
                    {
                        throw new Exception($"Cannot find file \"{_labelsGzPath}\".");
                    }

                    Decompress(_labelsGzPath, _labelsPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot open MNIST labels file.\r\n\r\n" + ex.Message);
                    return;
                }
            }

            LoadLabels(_labelsPath);
        }

        override public void SaveConfig()
        {
            Qualia.Tools.Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SaveConfig());
        }

        override public void RemoveFromConfig()
        {
            Qualia.Tools.Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.RemoveFromConfig());
        }

        override public bool IsValid()
        {
            return this.FindVisualChildren<IConfigParam>().All(param => param.IsValid());
        }

        override public void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChange)
        {
            this.SetUIHandler(onChange);
            Qualia.Tools.Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetOnChangeEvent(Parameter_OnChanged));
        }

        override public void InvalidateValue()
        {
            this.GetConfigParams().ForEach(p => p.InvalidateValue());
        }

        //

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

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
    sealed public partial class IsCrossPresentControl : BaseUserControl
    {
        public IsCrossPresentControl()
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlMaxPointsCount
                    .Initialize(defaultValue: 100)
            };
        }

        public int MaxPointsCount => (int)CtlMaxPointsCount.Value;

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
    }
}

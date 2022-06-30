using Qualia.Tools;
using System;
using System.Linq;
using System.Windows.Controls;

namespace Qualia.Controls
{
    public partial class BaseUserControl : UserControl, IConfigParam
    {
        protected Config _config;
        protected event Action _onChanged = delegate { };

        public BaseUserControl()
        {

        }

        public virtual bool IsValid()
        {
            return this.FindVisualChildren<IConfigParam>().All(param => param.IsValid());
        }

        public virtual void SetConfig(Config config)
        {
            _config = config.Extend(Name);

            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetConfig(_config));
        }

        public virtual void LoadConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.LoadConfig());
        }

        public virtual void SaveConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SaveConfig());
        }

        public virtual void RemoveFromConfig()
        {
            _config.Remove(Name);

            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.RemoveFromConfig());
        }

        public void SetChangeEvent(Action onChanged)
        {
            _onChanged -= onChanged;
            _onChanged += onChanged;
        }

        public void OnChanged()
        {
            _onChanged();
        }

        public void InvalidateValue() => throw new InvalidOperationException();
    }
}

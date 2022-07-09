using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Qualia.Controls
{
    public partial class BaseUserControl : UserControl, IConfigParam
    {
        protected Config _config;
        protected List<IConfigParam> _configParams;
        protected event Action<Notification.ParameterChanged> _onChanged = delegate {};

        public Notification.ParameterChanged UIParam { get; private set; }

        public BaseUserControl SetUIParam(Notification.ParameterChanged param)
        {
            UIParam = param;
            return this;
        }

        public BaseUserControl()
        {

        }

        public virtual bool IsValid()
        {
            return _configParams.TrueForAll(p => p.IsValid());
        }

        public virtual void SetConfig(Config config)
        {
            _config = config.Extend(Name);
            _configParams.ForEach(p => p.SetConfig(_config));
        }

        public virtual void LoadConfig()
        {
            _configParams.ForEach(p => p.LoadConfig());
        }

        public virtual void SaveConfig()
        {
            _configParams.ForEach(p => p.SaveConfig());
        }

        public virtual void RemoveFromConfig()
        {
            _config.Remove(Name);
            _configParams.ForEach(p => p.RemoveFromConfig());
        }

        public virtual void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            _onChanged -= onChanged;
            _onChanged += onChanged;

            _configParams.ForEach(p => p.SetOnChangeEvent(onChanged));
        }

        public void OnChanged(Notification.ParameterChanged param)
        {
            _onChanged(param == Notification.ParameterChanged.Unknown ? UIParam : param);
        }

        public virtual void InvalidateValue() => throw new InvalidOperationException();
    }
}

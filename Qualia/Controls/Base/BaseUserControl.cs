using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Qualia.Controls
{
    abstract public partial class BaseUserControl : UserControl, IConfigParam
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

        public void OnChanged(Notification.ParameterChanged param)
        {
            _onChanged(param == Notification.ParameterChanged.Unknown ? UIParam : param);
        }

        virtual public bool IsValid()
        {
            return _configParams.TrueForAll(p => p.IsValid());
        }

        // IConfigParam

        virtual public void SetConfig(Config config)
        {
            _config = config.Extend(Name);
            _configParams.ForEach(p => p.SetConfig(_config));
        }

        virtual public void LoadConfig()
        {
            _configParams.ForEach(p => p.LoadConfig());
        }

        virtual public void SaveConfig()
        {
            _configParams.ForEach(p => p.SaveConfig());
        }

        virtual public void RemoveFromConfig()
        {
            _config.Remove(Name);
            _configParams.ForEach(p => p.RemoveFromConfig());
        }

        virtual public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            _onChanged -= onChanged;
            _onChanged += onChanged;

            _configParams.ForEach(p => p.SetOnChangeEvent(onChanged));
        }

        virtual public void InvalidateValue()
        {
            _configParams.ForEach(p => p.InvalidateValue());
        }

        //
    }
}

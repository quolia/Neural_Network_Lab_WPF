using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Qualia.Controls
{
    abstract public partial class BaseUserControl : UserControl, IConfigParam
    {
        protected Config _config;
        protected List<IConfigParam> _configParams = new();
        protected event Action<Notification.ParameterChanged> _onChanged = delegate {};

        public Notification.ParameterChanged UIParam { get; private set; }

        public BaseUserControl SetUIParam(Notification.ParameterChanged param)
        {
            UIParam = param;
            return this;
        }

        public BaseUserControl()
        {
            //
        }

        public void OnChanged(Notification.ParameterChanged param)
        {
            _onChanged(param == Notification.ParameterChanged.Unknown ? UIParam : param);
        }

        // IConfigParam

        virtual public bool IsValid()
        {
            return _configParams.TrueForAll(cp => cp.IsValid());
        }

        virtual public void SetConfig(Config config)
        {
            _config = config.Extend(Name);
            _configParams.ForEach(cp => cp.SetConfig(_config));
        }

        virtual public void LoadConfig()
        {
            _configParams.ForEach(cp => cp.LoadConfig());
        }

        virtual public void SaveConfig()
        {
            _configParams.ForEach(cp => cp.SaveConfig());
        }

        virtual public void RemoveFromConfig()
        {
            _config.Remove(Name);
            _configParams.ForEach(cp => cp.RemoveFromConfig());
        }

        virtual public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            _onChanged -= onChanged;
            _onChanged += onChanged;

            _configParams.ForEach(cp => cp.SetOnChangeEvent(onChanged));
        }

        virtual public void InvalidateValue()
        {
            _configParams.ForEach(cp => cp.InvalidateValue());
        }

        //
    }
}

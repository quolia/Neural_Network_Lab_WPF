using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Qualia.Controls
{
    abstract public partial class BaseUserControl : UserControl, IConfigParam
    {
        protected Config _config;
        //protected List<IConfigParam> _configParams = new();

        protected Action<Notification.ParameterChanged> _onChanged;

        public BaseUserControl()
        {
            //
        }

        public void OnChanged(Notification.ParameterChanged param)
        {
            _onChanged(param == Notification.ParameterChanged.Unknown ? this.GetUIParam() : param);
        }

        // IConfigParam

        virtual public bool IsValid()
        {
            return this.GetConfigParams().TrueForAll(cp => cp.IsValid());
        }

        virtual public void SetConfig(Config config)
        {
            _config = config.Extend(Name);
            this.GetConfigParams().ForEach(cp => cp.SetConfig(_config));
        }

        virtual public void LoadConfig()
        {
            this.GetConfigParams().ForEach(cp => cp.LoadConfig());
        }

        virtual public void SaveConfig()
        {
            this.GetConfigParams().ForEach(cp => cp.SaveConfig());
        }

        virtual public void RemoveFromConfig()
        {
            _config.Remove(Name);
            this.GetConfigParams().ForEach(cp => cp.RemoveFromConfig());
        }

        virtual public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            if (_onChanged != null)
            {
                throw new InvalidOperationException();
            }

            _onChanged = onChanged;
            this.GetConfigParams().ForEach(cp => cp.SetOnChangeEvent(onChanged));
        }

        virtual public void InvalidateValue()
        {
            this.GetConfigParams().ForEach(cp => cp.InvalidateValue());
        }

        //
    }
}

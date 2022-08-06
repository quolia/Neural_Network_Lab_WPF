﻿using Qualia.Tools;
using System;
using System.Windows.Controls;

namespace Qualia.Controls
{
    abstract public partial class BaseUserControl : UserControl, IConfigParam
    {
        public BaseUserControl()
        {
            //
        }

        public void OnChanged(Notification.ParameterChanged param)
        {
            this.InvokeUIHandler(param);
        }

        // IConfigParam

        virtual public bool IsValid()
        {
            return this.GetConfigParams().TrueForAll(cp => cp.IsValid());
        }

        virtual public void SetConfig(Config config)
        {
            this.PutConfig(config.Extend(Name));
            this.GetConfigParams().ForEach(cp => cp.SetConfig(this.GetConfig()));
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
            this.GetConfig().Remove(Name);
            this.GetConfigParams().ForEach(cp => cp.RemoveFromConfig());
        }

        virtual public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            if (this.GetUIHandler() != ExtendedInfo.DefaultHandler)
            {
                throw new InvalidOperationException();
            }

            this.SetUIHandler(onChanged);
            this.GetConfigParams().ForEach(cp => cp.SetOnChangeEvent(onChanged));
        }

        virtual public void InvalidateValue()
        {
            this.GetConfigParams().ForEach(cp => cp.InvalidateValue());
        }

        //
    }
}

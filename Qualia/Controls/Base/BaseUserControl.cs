using Qualia.Tools;
using System;
using System.Windows.Controls;

namespace Qualia.Controls
{
    public partial class BaseUserControl : UserControl, IConfigParam
    {
        protected Config _config;
        protected event Action OnChanged = delegate { };

        public BaseUserControl()
        {

        }


        public bool IsValid() => true;

        public void SetConfig(Config config)
        {
            _config = config;

            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetConfig(config));
        }

        public void LoadConfig()
        {
            //var paramConfig = _config.Extend(Name);
            //ParamValue = paramConfig.GetDouble(Constants.Param.Value, 777);

            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.LoadConfig());
        }

        public void SaveConfig()
        {
            //var paramConfig = _config.Extend(Name);
            //paramConfig.Set(Constants.Param.Value, ParamValue);

            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SaveConfig());
        }

        public void VanishConfig()
        {
            _config.Remove(Name);

            //var paramConfig = _config.Extend(Name);
            //paramConfig.Remove(Constants.Param.Value);

            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.VanishConfig());
        }

        public void SetChangeEvent(Action onChanged)
        {
            OnChanged -= onChanged;
            OnChanged += onChanged;
        }

        public void InvalidateValue() => throw new InvalidOperationException();
    }
}

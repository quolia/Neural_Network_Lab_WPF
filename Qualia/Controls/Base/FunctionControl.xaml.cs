using Qualia.Tools;
using System;

namespace Qualia.Controls
{
    public partial class FunctionControl : BaseUserControl
    {
        public FunctionControl Initialize(string defaultFunction, double? defaultParam = null, double? paramMinValue = null, double? paramMaxValue = null)
        {
            CtlFunction
                .Initialize(defaultFunction);

            CtlParam
                .Initialize(defaultValue: defaultParam,
                            minValue: paramMinValue,
                            maxValue: paramMaxValue);

            return this;
        }

        public FunctionControl SetUIParam(Notification.ParameterChanged functionUIParam, Notification.ParameterChanged functionParamUIParam)
        {
            CtlFunction.SetUIParam(functionUIParam);
            CtlParam.SetUIParam(functionParamUIParam);

            return this;
        }

        public FunctionControl()
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlFunction,
                CtlParam
            };
        }

        private void Function_OnChanged(Notification.ParameterChanged param)
        {
            OnChanged(param);
        }

        private void Param_OnChanged(Notification.ParameterChanged param)
        {
            OnChanged(param);
        }

        public override void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            _onChanged -= onChanged;
            _onChanged += onChanged;

            CtlFunction.SetOnChangeEvent(Function_OnChanged);
            CtlParam.SetOnChangeEvent(Param_OnChanged);
        }

        public override void SetConfig(Config config)
        {
            _config = config;

            config = config.Extend(Name);

            CtlFunction.SetConfig(config);
            CtlParam.SetConfig(config.Extend(SelectedFunction.Name));
        }

        public override void LoadConfig()
        {
            CtlFunction.LoadConfig();
            CtlParam.LoadConfig();
        }

        public override void SaveConfig()
        {
            _config.Set(Name, CtlFunction.Value);
            CtlParam.SaveConfig();
        }

        public T SetConfig<T>(Config config) where T : class
        {
            try
            {
                return CtlFunction.Fill<T>(config, Name);
            }
            finally
            {
                SetConfig(config);
            }
        }

        public SelectedFunction SelectedFunction
        {
            get
            {
                if (CtlFunction.SelectedItem == null)
                {
                    return null;
                }

                return new SelectedFunction(CtlFunction.Value, CtlParam.Value);
            }
        }
        
        public T GetInstance<T>() where T : class
        {
            return BaseFunction<T>.GetInstance(SelectedFunction.Name);
        }
    }

    public class SelectedFunction
    {
        public readonly string Name;
        public readonly double Param;

        public SelectedFunction(string name, double param)
        {
            Name = name;
            Param = param;
        }
    }
}

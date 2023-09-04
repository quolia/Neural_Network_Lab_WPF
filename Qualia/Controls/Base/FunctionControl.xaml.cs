using Qualia.Tools;

namespace Qualia.Controls
{
    public partial class FunctionControl : BaseUserControl
    {
        public FunctionControl Initialize(string defaultFunction,
                                          double? defaultParam = null,
                                          double? paramMinValue = null,
                                          double? paramMaxValue = null)
        {
            CtlFunction
                .Initialize(defaultFunction);

            CtlParam
                .Initialize(defaultValue: defaultParam,
                            minValue: paramMinValue,
                            maxValue: paramMaxValue);

            return this;
        }

        public FunctionControl SetUIParam(Notification.ParameterChanged functionUIParam,
                                          Notification.ParameterChanged functionParamUIParam)
        {
            CtlFunction.SetUIParam(functionUIParam);
            CtlParam.SetUIParam(functionParamUIParam);

            return this;
        }

        public FunctionControl()
            : base(0)
        {
            InitializeComponent();

            this.SetConfigParams(new()
            {
                CtlFunction,
                CtlParam
            });
        }

        private void Function_OnChanged(ApplyAction action)
        {
            OnChanged(action);
        }

        private void Param_OnChanged(ApplyAction action)
        {
            OnChanged(action);
        }

        // IConfigParam

        override public void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChanged)
        {
            this.SetUIHandler(onChanged);

            CtlFunction.SetOnChangeEvent(Function_OnChanged);
            CtlParam.SetOnChangeEvent(Param_OnChanged);
        }

        override public void SetConfig(Config config)
        {
            this.PutConfig(config);

            config = config.Extend(Name);

            CtlFunction.SetConfig(config);
            CtlParam.SetConfig(config.Extend(SelectedFunction.Name));
        }

        override public void LoadConfig()
        {
            CtlFunction.LoadConfig();
            CtlParam.LoadConfig();
        }

        override public void SaveConfig()
        {
            this.GetConfig().Set(Name, CtlFunction.Value.Text);
            CtlParam.SaveConfig();
        }

        //

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

                return new SelectedFunction(CtlFunction.Value.Text, CtlParam.Value);
            }
        }
        
        public T GetInstance<T>() where T : class
        {
            return BaseFunction<T>.GetInstanceByName(SelectedFunction.Name);
        }
    }

    public class SelectedFunction(string name, double param)
    {
        public readonly string Name = name;
        public readonly double Param = param;
    }
}

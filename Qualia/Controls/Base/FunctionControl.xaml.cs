using Qualia.Tools;
using System;
using System.Windows.Controls;

namespace Qualia.Controls
{
    public partial class FunctionControl : BaseUserControl
    {
        public FunctionControl Initialize(string defaultFunctionName, double? defaultParamValue = null)
        {
            CtlFunction.Initialize(defaultFunctionName);
            CtlParam.Initialize(defaultValue: defaultParamValue);

            return this;
        }

        public FunctionControl()
        {
            InitializeComponent();

            CtlFunction.AddChangeEventListener(Function_OnChanged);
            CtlParam.AddChangeEventListener(Param_OnChanged);
        }

        private void Function_OnChanged()
        {
            if (!IsLoading)
            { 
                OnChanged();
            }
        }

        private void Param_OnChanged()
        {
            if (!IsLoading)
            {
                OnChanged();
            }
        }

        public override void AddChangeEventListener(Action onChanged)
        {
            _onChanged -= onChanged;
            _onChanged += onChanged;
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
            //CtlFunction.SelectionChanged -= Function_OnSelected;

            if (!IsLoading)
            {
                IsLoading = true;

                CtlFunction.LoadConfig();
                CtlParam.LoadConfig();

                IsLoading = false;
            }
            else
            {
                throw new InvalidOperationException();
            }

           // CtlFunction.SelectionChanged += Function_OnSelected;
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

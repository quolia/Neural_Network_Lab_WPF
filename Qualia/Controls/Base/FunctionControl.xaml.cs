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
            CtlFunctionParam.Initialize(defaultValue: defaultParamValue);

            return this;
        }

        public FunctionControl()
        {
            InitializeComponent();
        }

        private void Function_OnSelected(object sender, SelectionChangedEventArgs e)
        {
            OnChanged();
        }

        public new void SetConfig(Config config)
        {
            config = config.Extend(Name);

            CtlFunction.SetConfig(config);
            CtlFunctionParam.SetConfig(config.Extend(SelectedFunction.Name));
        }

        public new void LoadConfig()
        {
            CtlFunction.LoadConfig();
            CtlFunctionParam.LoadConfig();

            //CtlFunction.SelectionChanged += SelectBox_SelectionChanged;
        }

        public T Fill<T>(Config config) where T : class
        {
            //_setConfig(config);
            //LoadConfig();

            return CtlFunction.Fill<T>(config, Name);
        }

        public SelectedFunction SelectedFunction
        {
            get
            {
                if (CtlFunction.SelectedItem == null)
                {
                    return new(null, 0);
                }

                var selectedFunctionName = CtlFunction.SelectedItem.ToString();
                return new SelectedFunction(selectedFunctionName, CtlFunctionParam.Value);
            }
        }
        
        public T GetInstance<T>() where T : class
        {
            return BaseFunction<T>.GetInstance(SelectedFunction.Name);
        }
    }

    public class SelectedFunction
    {
        public string Name;
        public double Param;

        public SelectedFunction(string name, double param)
        {
            Name = name;
            Param = param;
        }
    }
}

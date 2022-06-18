using Qualia.Tools;
using System;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public partial class FunctionControl : BaseUserControl
    {
        public FunctionControl SetDefaultValues(string functionName, double functionParam)
        {
            CtlFunction.DefaultValue = functionName;
            CtlFunctionParam.DefaultValue = functionParam;

            return this;
        }

        public FunctionControl()
        {
            InitializeComponent();

            CtlFunction.SelectionChanged += SelectBox_SelectionChanged;
        }

        private void SelectBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //OnChanged();
        }
    }
}

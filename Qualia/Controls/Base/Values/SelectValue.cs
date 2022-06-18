using Qualia.Tools;
using System;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public class SelectValueControl : ComboBox, IConfigParam
    {
        private Config _config;
        private event Action OnChanged = delegate { };

        public string DefaultValue { get; set; }

        public SelectValueControl SetDefaultValue(string value)
        {
            DefaultValue = value;
            return this;
        }

        public SelectValueControl()
        {
            //ItemTemplate = Main.Instance.Resources["QComboBoxTemplate"] as DataTemplate;

            Padding = new(2);
            Margin = new(1);
            MinWidth = 60;

            SelectionChanged += SelectBox_SelectionChanged;
        }

        private void SelectBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnChanged();
        }

        public void SetConfig(Config config)
        {
            _config = config;
        }

        public void LoadConfig()
        {
            //var paramConfig = _config.Extend(Name);
            //ParamValue = paramConfig.GetDouble(Constants.Param.Value, 777);
        }

        public void SaveConfig()
        {
            _config.Set(Name, SelectedItem.ToString());

            //var paramConfig = _config.Extend(Name);
            //paramConfig.Set(Constants.Param.Value, ParamValue);
        }

        public void VanishConfig()
        {
            _config.Remove(Name);

            //var paramConfig = _config.Extend(Name);
            //paramConfig.Remove(Constants.Param.Value);
        }

        public bool IsValid() => true;

        public void SetChangeEvent(Action onChanged)
        {
            OnChanged -= onChanged;
            OnChanged += onChanged;
        }

        public void InvalidateValue() => throw new InvalidOperationException();
    }
}

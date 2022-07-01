using Qualia.Controls;
using Qualia.Tools;

namespace Qualia.Model
{
    public class WorkingModel
    {
        public SettingsModel Settings;

        private static WorkingModel _current = new();

        public static WorkingModel Current()
        {
            return _current;
        }

        private WorkingModel()
        {
            //
        }

        public WorkingModel Refresh(Main main)
        {
            WorkingModel current = new();

            current.RefreshSettings(main);

            _current = current;
            return _current;
        }

        public void RefreshSettings(Main main)
        {
            main.Dispatch(() =>
            {
                Settings = main.CtlSettings.GetModel();

            }).Wait();
        }
    }
}

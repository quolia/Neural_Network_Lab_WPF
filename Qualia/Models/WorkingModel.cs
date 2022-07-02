using Qualia.Controls;
using Qualia.Tools;

namespace Qualia.Model
{
    public class WorkingModel
    {
        public SettingsModel Settings;
        public TaskModel Task;

        private static WorkingModel _current = new(null);

        private Main _main;

        public static WorkingModel Current()
        {
            return _current;
        }

        private WorkingModel(Main main)
        {
            _main = main;
        }

        public WorkingModel Refresh(Main main)
        {
            WorkingModel current = new(main);

            _current = current;
            return _current;
        }

        public WorkingModel RefreshSettings()
        {
            _main.Dispatch(() =>
            {
                Settings = _main.CtlSettings.GetModel();

            }).Wait();

            return this;
        }

        public WorkingModel RefreshDataPresenter()
        {
            _main.Dispatch(() =>
            {
                Task = _main.CtlInputDataPresenter.GetModel();

            }).Wait();

            return this;
        }
    }

    sealed public class SettingsModel
    {
        public int SkipRoundsToDrawErrorMatrix;
        public int SkipRoundsToDrawNetworks;
        public int SkipRoundsToDrawStatistics;
    }

    sealed public class TaskModel
    {
        public TaskFunction TaskFunction;
    }
}

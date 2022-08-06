using Qualia.Controls;
using Qualia.Tools;
using System;

namespace Qualia.Model
{
    public class WorkingModel
    {
        //public Settings Settings;
        public NetworkDataModel Network;
        public NetworkDataModel SelectedNetwork;

        private static WorkingModel _current = new(null);

        private Main _main;

        public static WorkingModel Current => _current; 

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

        public WorkingModel RefreshAll(Main main, NetworksManager manager)
        {
            WorkingModel current = Refresh(main)
                                    //.RefreshSettings()
                                    .RefreshNetworks(manager);

            _current = current;
            return _current;
        }

        /*
        public WorkingModel RefreshSettings()
        {
            _main.Dispatch(() =>
            {
                Settings = _main.CtlSettings.Get();

            }).Wait();

            return this;
        }
        */

        public WorkingModel RefreshNetworks(NetworksManager manager)
        {
            _main.Dispatch(() =>
            {
                Network = manager.NetworkModels.First;
                SelectedNetwork = manager.SelectedNetworkModel;

            }).Wait();

            return this;
        }
    }

    sealed public class Settings
    {
        public int SkipRoundsToDrawErrorMatrix;
        public int SkipRoundsToDrawNetworks;
        public int SkipRoundsToDrawStatistics;
        public bool IsNoSleepMode;
    }
}

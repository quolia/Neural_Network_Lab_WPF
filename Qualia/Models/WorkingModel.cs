using Qualia.Controls;
using Qualia.Tools;
using System;

namespace Qualia.Model
{
    public class WorkingModel
    {
        public SettingsModel Settings;
        public TaskModel Task;
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
                                    .RefreshSettings()
                                    .RefreshDataPresenter()
                                    .RefreshNetworks(manager);

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

        public WorkingModel RefreshNetworks(NetworksManager manager)
        {
            _main.Dispatch(() =>
            {
                Network = manager.NetworkModels.First;
                SelectedNetwork = manager.SelectedNetworkModel;

            }).Wait();

            return this;
        }

        unsafe public void PrepareModelsForLoop()
        {
            NetworkDataModel network = Network;

            while (network != null)
            {
                if (!network.IsEnabled)
                {
                    network = network.Next;
                    continue;
                }

                network.BackPropagationStrategy.PrepareForLoop(network);
                network = network.Next;
            }
        }

        unsafe public void PrepareModelsForRun()
        {
            Network.ForEach(network => network.ActivateFirstLayer());
            Network.ForEach(network => network.BackPropagationStrategy.PrepareForRun(network));

            ResetModelsDynamicStatistics();
            ResetModelsStatistics();
            ResetErrorMatrix();
        }

        public void FeedForward()
        {
            Network.ForEach(network => network.FeedForward());
        }

        public void ResetModelsStatistics()
        {
            Network.ForEach(network => network.Statistics = new());
        }

        private void ResetModelsDynamicStatistics()
        {
            Network.ForEach(network => network.PlotterStatistics = new());
        }

        public void ResetErrorMatrix()
        {
            Network.ForEach(network =>
            {
                network.ErrorMatrix.ClearData();
                network.ErrorMatrix.Next.ClearData();
            });
        }

        unsafe public void PrepareModelsForRound()
        {
            var baseNetwork = Network;
            Task.TaskFunction.Do(baseNetwork, Task.DistributionFunction, Task.DistributionFunctionParam);

            // copy first layer state and last layer targets to other networks

            var network = baseNetwork.Next;
            while (network != null)
            {
                if (!network.IsEnabled)
                {
                    network = network.Next;
                    continue;
                }

                network.BackPropagationStrategy.PrepareForRound(network);

                var baseNeuron = baseNetwork.Layers.First.Neurons.First;
                var neuron = network.Layers.First.Neurons.First;

                while (neuron != null)
                {
                    if (!neuron.IsBias)
                    {
                        neuron.X = baseNeuron.X;
                        neuron.Activation = baseNeuron.Activation;
                    }

                    neuron = neuron.Next;
                    baseNeuron = baseNeuron.Next;
                }

                baseNeuron = baseNetwork.Layers.Last.Neurons.First;
                neuron = network.Layers.Last.Neurons.First;

                while (neuron != null)
                {
                    neuron.Target = baseNeuron.Target;

                    neuron = neuron.Next;
                    baseNeuron = baseNeuron.Next;
                }

                network.TargetOutputNeuronId = baseNetwork.TargetOutputNeuronId;

                network = network.Next;
            }
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
        public DistributionFunction DistributionFunction;
        public double DistributionFunctionParam;
        public SolutionsData SolutionsData;
    }
}

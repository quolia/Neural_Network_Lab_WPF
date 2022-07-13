using Qualia.Tools;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace Qualia.Controls
{
    abstract public partial class LayerBaseControl : StackPanel, IConfigParam
    {
        public readonly long Id;
        public readonly Config Config;
         
        public readonly Action<Notification.ParameterChanged> _onChanged;

        public ObservableCollection<NeuronBaseControl> Neurons { get; }

        public LayerBaseControl(long configId, Config config, Action<Notification.ParameterChanged> onChanged)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            Neurons = new ObservableCollection<NeuronBaseControl>();
            Neurons.CollectionChanged += Neurons_CollectionChanged;

            _onChanged = onChanged;

            Id = UniqId.GetNextId(configId);
            Config = config.ExtendWithId(Id);

            Loaded += LayerBaseControl_Loaded;
        }

        public void RefreshNeuronsOrdinalNumbers()
        {
            int ordinalNumber = 0;
            Range.ForEach(Neurons, n => n.OrdinalNumber_OnChanged(++ordinalNumber));
        }

        private void LayerBaseControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LayerControl_OnLoaded();
        }

        private void Neurons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

        }

        public void Scroll_OnChanged(object sender, ScrollChangedEventArgs e)
        {
            MaxWidth = (sender as ScrollViewer).ViewportWidth;
        }

        public void AddNeuron() => AddNeuron(Constants.UnknownId);

        virtual public bool CanNeuronBeRemoved() => Neurons.Count > 1;

        abstract public void LayerControl_OnLoaded();
        abstract public void AddNeuron(long id);
        abstract public bool RemoveNeuron(NeuronBaseControl neuron);

        // Layer type.

        virtual public bool IsInput => false;
        virtual public bool IsHidden => false;
        virtual public bool IsOutput => false;

        // IConfigParam

        abstract public void SetConfig(Config config);
        abstract public void LoadConfig();
        abstract public void SaveConfig();
        abstract public void RemoveFromConfig();
        abstract public bool IsValid();
        abstract public void SetOnChangeEvent(Action<Notification.ParameterChanged> action);
        abstract public void InvalidateValue();
    }
}

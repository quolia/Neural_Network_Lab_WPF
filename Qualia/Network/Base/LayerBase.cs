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

        public virtual bool CanNeuronBeRemoved() => Neurons.Count > 1;

        public abstract void LayerControl_OnLoaded();
        public abstract void AddNeuron(long id);
        public abstract bool RemoveNeuron(NeuronBaseControl neuron);

        // Layer type.

        public virtual bool IsInput => false;
        public virtual bool IsHidden => false;
        public virtual bool IsOutput => false;

        // IConfigParam

        public abstract void SetConfig(Config config);
        public abstract void LoadConfig();
        public abstract void SaveConfig();
        public abstract void RemoveFromConfig();
        public abstract bool IsValid();
        public abstract void SetOnChangeEvent(Action<Notification.ParameterChanged> action);
        public abstract void InvalidateValue();
    }
}

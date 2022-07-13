using Qualia.Tools;
using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Qualia.Controls
{
    abstract public partial class LayerBaseControl : StackPanel, IConfigParam
    {
        public readonly long Id;
        public readonly Config Config;
         
        public readonly Action<Notification.ParameterChanged> NetworkUI_OnChanged;

        public LayerBaseControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
        {
            Neurons = new ObservableCollection<NeuronBaseControl>();

            Neurons.CollectionChanged += Neurons_CollectionChanged;

            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            NetworkUI_OnChanged = onNetworkUIChanged;

            Id = UniqId.GetNextId(id);
            Config = config.ExtendWithId(Id);
        }

        private void Neurons_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            
        }

        public ObservableCollection<NeuronBaseControl> Neurons { get; set; }

        public void RefreshOrdinalNumbers()
        {
            int ordinalNumber = 0;
            Range.ForEach(Neurons, n => n.OrdinalNumber_OnChanged(++ordinalNumber));
        }

        public virtual bool IsInput => false;
        public virtual bool IsHidden => false;
        public virtual bool IsOutput => false;

        //public virtual ItemsControl NeuronsHolder => throw new InvalidOperationException();
        //public virtual int NeuronsCount => Neurons.Count;// GetNeuronsControls().Count();

        //public IEnumerable<NeuronBaseControl> GetNeuronsControls() => Neurons;// NeuronsHolder.Items.OfType<NeuronBaseControl>();

        public void AddNeuron() => AddNeuron(Constants.UnknownId);

        public bool CanNeuronBeRemoved()
        {
            return Neurons.Count > 1;
        }

        public abstract bool RemoveNeuron(NeuronBaseControl neuron);

        public virtual void AddNeuron(long id) => throw new InvalidOperationException();
        public virtual bool IsValid() => throw new InvalidOperationException();
        public virtual void SaveConfig() => throw new InvalidOperationException();
        public virtual void RemoveFromConfig() => throw new InvalidOperationException();

        public void Scroll_OnChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            MaxWidth = (sender as ScrollViewer).ViewportWidth;
        }

        public void SetConfig(Config config)
        {
            throw new NotImplementedException();
        }

        public void LoadConfig()
        {
            throw new NotImplementedException();
        }

        public void SetOnChangeEvent(Action<Notification.ParameterChanged> action)
        {
            throw new NotImplementedException();
        }

        public void InvalidateValue()
        {
            throw new NotImplementedException();
        }
    }
}

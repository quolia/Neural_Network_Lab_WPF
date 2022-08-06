using Qualia.Tools;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Qualia.Controls
{
    abstract public partial class LayerBaseControl : BaseUserControl
    {
        public readonly long Id;

        public ObservableCollection<NeuronBaseControl> Neurons { get; }

        public LayerBaseControl(long configId,
                                Config config,
                                Action<Notification.ParameterChanged> onChanged)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            Neurons = new ObservableCollection<NeuronBaseControl>();

            Id = UniqId.GetNextId(configId);
            this.PutConfig(config.ExtendWithId(Id));

            SetOnChangeEvent(onChanged);

            Loaded += LayerBaseControl_Loaded;
        }

        public void RefreshNeuronsOrdinalNumbers()
        {
            int ordinalNumber = 0;
            Range.ForEach(Neurons, n => n.SetOrdinalNumber(++ordinalNumber));
        }

        private void LayerBaseControl_Loaded(object sender, RoutedEventArgs e)
        {
            LayerControl_OnLoaded();
        }

        public void Scroll_OnChanged(object sender, ScrollChangedEventArgs e)
        {
            MaxWidth = (sender as ScrollViewer).ViewportWidth;
        }

        public NeuronBaseControl AddNeuron() => AddNeuron(Constants.UnknownId);

        virtual public bool CanNeuronBeAdded() => true;
        virtual public bool CanNeuronBeRemoved() => Neurons.Count > 1;

        abstract public void LayerControl_OnLoaded();
        abstract public NeuronBaseControl AddNeuron(long id);
        abstract public int RemoveNeuron(NeuronBaseControl neuron);

        // Layer type.

        virtual public bool IsInput => false;
        virtual public bool IsHidden => false;
        virtual public bool IsOutput => false;

        //

        abstract public void SetAllNeuronsSelected(bool isSelected);
    }
}

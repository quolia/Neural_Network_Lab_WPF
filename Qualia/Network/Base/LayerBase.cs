using Qualia.Tools;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace Qualia.Controls
{
    abstract public partial class LayerBaseControl : BaseUserControl
    {
        public ObservableCollection<NeuronBaseControl> Neurons { get; } = new();

        public LayerBaseControl(long configId,
                                Config config,
                                ActionManager.ApplyActionDelegate onChanged)
            : base(UniqId.GetNextId(configId))
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.PutConfig(config.ExtendWithId(VisualId));

            SetOnChangeEvent(onChanged);

            Loaded += LayerBaseControl_Loaded;
        }

        public void RefreshNeuronsOrdinalNumbers()
        {
            int ordinalNumber = 0;
            Qualia.Tools.Range.ForEach(Neurons, n => n.SetOrdinalNumber(++ordinalNumber));
        }

        private void LayerBaseControl_Loaded(object sender, RoutedEventArgs e)
        {
            LayerControl_OnLoaded();
        }

        public void Scroll_OnChanged(object sender, ScrollChangedEventArgs e)
        {
            MaxWidth = (sender as ScrollViewer).ViewportWidth;
        }

        public NeuronBaseControl AddNeuron()
        {
            return AddNeuron(Constants.UnknownId);
        }

        virtual public bool CanNeuronBeAdded() => true;
        virtual public bool CanNeuronBeRemoved(NeuronBaseControl neuron)
        {
            int count = Neurons.Count;

            if (count < 2)
            {
                return false;
            }

            if (!neuron.IsSelected)
            {
                return true;
            }

            var selected = Neurons.Where(n => n.IsSelected).ToList();
            return count - selected.Count > 0;
        }

        abstract public void LayerControl_OnLoaded();
        abstract public NeuronBaseControl AddNeuron(long id);
        abstract public int RemoveNeuron(NeuronBaseControl neuron);

        // Layer type.

        virtual public bool IsInput => false;
        virtual public bool IsHidden => false;
        virtual public bool IsOutput => false;

        //

        abstract public void SetAllNeuronsSelected(bool isSelected);

        virtual public void CopyTo(LayerBaseControl newLayer)
        {
            if (GetType() != newLayer.GetType())
            {
                throw new InvalidOperationException();
            }

            int newNeuronsCount = newLayer.Neurons.Count;

            for (int i = 0; i < Neurons.Count; ++i)
            {
                var neuron = Neurons[i];
                var newNeuron = i < newNeuronsCount ? newLayer.Neurons[i] : newLayer.AddNeuron();
                neuron.CopyTo(newNeuron);
            }
        }
    }
}

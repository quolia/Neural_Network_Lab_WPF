using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public partial class LayerBase : StackPanel
    {
        public readonly long Id;
        public readonly Config Config;
         
        public Action<Notification.ParameterChanged> OnNetworkUIChanged;

        public LayerBase(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            OnNetworkUIChanged = onNetworkUIChanged;

            Id = UniqId.GetNextId(id);
            Config = config.Extend(Id);
        }

        public void RefreshOrdinalNumbers()
        {
            int ordinalNumber = 0;
            var neurons = GetNeuronsControls();

            Range.ForEach(neurons, n => n.OrdinalNumberChanged(++ordinalNumber));
        }

        public virtual bool IsInput => false;
        public virtual bool IsHidden => false;
        public virtual bool IsOutput => false;
        public virtual Panel NeuronsHolder => throw new InvalidOperationException();
        public virtual int NeuronsCount => GetNeuronsControls().Count();

        public IEnumerable<NeuronBase> GetNeuronsControls() => NeuronsHolder.Children.OfType<NeuronBase>();

        public void AddNeuron() => AddNeuron(Constants.UnknownId);

        public virtual void AddNeuron(long id) => throw new InvalidOperationException();
        public virtual bool IsValid() => throw new InvalidOperationException();
        public virtual void SaveConfig() => throw new InvalidOperationException();
        public virtual void VanishConfig() => throw new InvalidOperationException();

        public void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            MaxWidth = (sender as ScrollViewer).ViewportWidth;
        }
    }
}

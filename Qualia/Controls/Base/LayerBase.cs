using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Qualia.Controls
{
    public partial class LayerBaseControl : StackPanel
    {
        public readonly long Id;
        public readonly Config Config;
         
        public readonly Action<Notification.ParameterChanged> NetworkUI_OnChanged;

        public LayerBaseControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            NetworkUI_OnChanged = onNetworkUIChanged;

            Id = UniqId.GetNextId(id);
            Config = config.Extend(Id);
        }

        public void RefreshOrdinalNumbers()
        {
            int ordinalNumber = 0;
            var neurons = GetNeuronsControls();

            Range.ForEach(neurons, n => n.OrdinalNumber_OnChanged(++ordinalNumber));
        }

        public virtual bool IsInput => false;
        public virtual bool IsHidden => false;
        public virtual bool IsOutput => false;
        public virtual Panel NeuronsHolder => throw new InvalidOperationException();
        public virtual int NeuronsCount => GetNeuronsControls().Count();

        public IEnumerable<NeuronBaseControl> GetNeuronsControls() => NeuronsHolder.Children.OfType<NeuronBaseControl>();

        public void AddNeuron() => AddNeuron(Constants.UnknownId);

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
    }
}

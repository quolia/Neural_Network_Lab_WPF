using System;
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

            Id = UniqId.GetId(id);
            Config = config.Extend(Id);
        }

        public void RefreshOrdinalNumbers()
        {
            int ordinalNumber = 0;
            GetNeuronsControls().ForEach(c => c.OrdinalNumberChanged(++ordinalNumber));
        }

        public virtual bool IsInput => false;
        public virtual bool IsHidden => false;
        public virtual bool IsOutput => false;

        public virtual Panel NeuronsHolder => throw new NotImplementedException();

        public virtual int NeuronsCount => GetNeuronsControls().Count;

        public ListX<NeuronBase> GetNeuronsControls()
        {
            return new ListX<NeuronBase>(NeuronsHolder.Children.OfType<NeuronBase>());
        }

        public void AddNeuron()
        {
            AddNeuron(Const.UnknownId);
        }

        public virtual void AddNeuron(long id)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsValid()
        {
            throw new NotImplementedException();
        }

        public virtual void SaveConfig()
        {
            throw new NotImplementedException();
        }

        public virtual void VanishConfig()
        {
            throw new NotImplementedException();
        }

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

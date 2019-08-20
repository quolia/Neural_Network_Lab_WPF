using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
            OnNetworkUIChanged = onNetworkUIChanged;
            LayoutUpdated += LayerBase_LayoutUpdated;

            Id = UniqId.GetId(id);
            Config = config.Extend(Id);
        }

        private void LayerBase_LayoutUpdated(object sender, EventArgs e)
        {
            int ordinalNumber = 0;
            GetNeuronsControls().ForEach(c => c.OrdinalNumberChanged(++ordinalNumber));
        }

        public virtual bool IsInput => false;
        public virtual bool IsHidden => false;
        public virtual bool IsOutput => false;

        public virtual Panel NeuronsHolder => throw new NotImplementedException();

        public virtual int NeuronsCount => GetNeuronsControls().Count;

        public List<NeuronBase> GetNeuronsControls()
        {
            return NeuronsHolder.Children.OfType<NeuronBase>().ToList();
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
            MaxWidth = (sender as ScrollViewer).ViewportWidth;
        }
    }
}

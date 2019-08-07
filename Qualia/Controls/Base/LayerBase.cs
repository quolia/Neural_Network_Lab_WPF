using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Tools;

namespace Qualia.Controls
{
    public partial class LayerBase : StackPanel
    {
        public readonly long Id;
        public readonly Config Config;
        public readonly Action<Notification.ParameterChanged, object> OnNetworkUIChanged;

        public LayerBase()
        {
            
        }

        public LayerBase(long id, Config config, Action<Notification.ParameterChanged, object> onNetworkUIChanged)
        {
            OnNetworkUIChanged = onNetworkUIChanged;

            Id = UniqId.GetId(id);
            Config = config.Extend(Id);
        }

        public virtual bool IsInput => false;
        public virtual bool IsHidden => false;
        public virtual bool IsOutput => false;

        public virtual int NeuronsCount => GetNeuronsControls().Count;

        public List<NeuronBase> GetNeuronsControls()
        {
            return Children.OfType<NeuronBase>().ToList();
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
        /*
        private void CtlFlow_Layout(object sender, LayoutEventArgs e)
        {
            if (CtlFlow.Controls.Count > 0)
            {
                CtlFlow.SuspendLayout();
                int ordinalNumber = 0;
                foreach (NeuronBase control in CtlFlow.Controls)
                {
                    control.OrdinalNumberChanged(++ordinalNumber);
                    control.Width = CtlFlow.Width - (CtlFlow.VerticalScroll.Visible ? System.Windows.Forms.SystemInformation.VerticalScrollBarWidth : 0);
                }
                CtlFlow.ResumeLayout();
            }
        }
        */
        /*
        private void CtlFlow_ControlAdded(object sender, ControlEventArgs e)
        {
            CtlFlow.ScrollControlIntoView(e.Control);
        }
        */
    }
}

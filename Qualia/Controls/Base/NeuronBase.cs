using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public partial class NeuronBase : UserControl
    {
        public readonly long Id;
        public Config Config;
        public readonly Action<Notification.ParameterChanged, object> OnNetworkUIChanged;

        public NeuronBase()
        {

        }

        public NeuronBase(long id, Config config, Action<Notification.ParameterChanged, object> onNetworkUIChanged)
        {
            OnNetworkUIChanged = onNetworkUIChanged;

            Id = UniqId.GetId(id);
            if (config != null)
            {
                Config = config.Extend(Id);
            }
        }

        public void StateChanged()
        {
            Background = IsBias ? Draw.GetBrush(Draw.GetRandomColor(20, Draw.GetColor(240, 250, 240))) : Draw.GetBrush(Draw.GetRandomColor(20, Colors.Lavender));
        }

        public virtual string ActivationInitializer
        {
            get { throw new NotImplementedException(); }
        }

        public virtual double? ActivationInitializerParamA
        {
            get { throw new NotImplementedException(); }
        }

        public virtual string WeightsInitializer
        {
            get { throw new NotImplementedException(); }
        }

        public virtual double? WeightsInitializerParamA
        {
            get { throw new NotImplementedException(); }
        }

        public virtual string ActivationFunc
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual double? ActivationFuncParamA
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual bool IsBias
        {
            get { throw new NotImplementedException(); }
        }

        public virtual bool IsBiasConnected
        {
            get { throw new NotImplementedException(); }
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

        public virtual void OrdinalNumberChanged(int number)
        {
            throw new NotImplementedException();
        }

        private void CtlMenuDeleteNeuron_Click(object sender, EventArgs e)
        {
            DeleteNeuron();
        }

        private void DeleteNeuron()
        {
            if ((Parent as Panel).Children.OfType<NeuronBase>().Count() == 1)
            {
                MessageBox.Show("At least one neuron must exist.", "Warning", MessageBoxButton.OK);
                return;
            }

            var color = Background;
            Background = Brushes.Tomato;

            if (MessageBox.Show("Would you really like to delete the neuron?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                (Parent as Panel).Children.Remove(this);
                VanishConfig();
                OnNetworkUIChanged(Notification.ParameterChanged.NeuronsCount, null);
            }
            else
            {
                Background = color;
            }
        }

        private void CtlMenuAddNeuron_Click(object sender, EventArgs e)
        {
            ((Parent as Panel).Parent as LayerBase).AddNeuron();
        }
        /*
        private void NeuronBase_Layout(object sender, LayoutEventArgs e)
        {
            bool designMode = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);

            if (!designMode)
            {
                int height = 0;
                SuspendLayout();
                foreach (Control c in Controls)
                {
                    if (c.Visible)
                    {
                        height += c.Height;
                    }
                }
                Height = height;
                ResumeLayout();
            }
        }
        */

            /*
        private void CtlContextMenu_Opening(object sender, CancelEventArgs e)
        {
            CtlMenuDeleteNeuron.Enabled = Parent.Controls.OfType<NeuronBase>().Count() > 1;
        }
        */
    }
}

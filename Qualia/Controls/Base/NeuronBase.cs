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

        public Action<Notification.ParameterChanged> OnNetworkUIChanged;

        public NeuronBase(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
        {
            ContextMenu = new ContextMenu();
            ContextMenu.Opened += ContextMenu_Opened;
            ContextMenu.Items.Add(new MenuItem() { Header = "Add" });
            (ContextMenu.Items[0] as MenuItem).Click += AddNeuron_Click;
            ContextMenu.Items.Add(new MenuItem() { Header = "Delete..." });
            (ContextMenu.Items[1] as MenuItem).Click += DeleteNeuron_Click;

            OnNetworkUIChanged = onNetworkUIChanged;

            Id = UniqId.GetId(id);
            if (config != null)
            {
                Config = config.Extend(Id);
            }            
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            (ContextMenu.Items[1] as MenuItem).IsEnabled = this.GetParentOfType<LayerBase>().NeuronsCount > 1;
        }

        private void AddNeuron_Click(object sender, RoutedEventArgs e)
        {
            this.GetParentOfType<LayerBase>().AddNeuron();

            // The code below is needed to refresh Tabcontrol.
            // Without it newly added neuron control is not visible for hit test (some WPF issue).

            int selectedIndex = this.GetParentOfType<TabControl>().SelectedIndex;
            this.GetParentOfType<TabControl>().SelectedIndex = 0;
            this.GetParentOfType<TabControl>().SelectedIndex = selectedIndex;
        }

        private void DeleteNeuron_Click(object sender, RoutedEventArgs e)
        {
            DeleteNeuron();
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            var sv = this.GetParentOfType<ScrollViewer>();
            if (sv != null)
            {
                sv.ScrollToBottom();
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

        private void DeleteNeuron()
        {
            if (this.GetParentOfType<LayerBase>().NeuronsCount < 2)
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
                OnNetworkUIChanged(Notification.ParameterChanged.NeuronsCount);
            }
            else
            {
                Background = color;
            }
        }
    }
}

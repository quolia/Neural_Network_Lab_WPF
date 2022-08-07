using Qualia.Tools;
using System;
using System.Linq;
using System.Windows;

namespace Qualia.Controls
{
    sealed public partial class HiddenLayerControl : LayerBaseControl
    {
        public HiddenLayerControl()
            : base(0, null, null)
        {
            InitializeComponent();
        }

        public HiddenLayerControl(long id, Config config, Action<Notification.ParameterChanged> onChanged)
            : base(id, config, onChanged)
        {
            InitializeComponent();

            LoadConfig();
        }

        // IConfigParam

        public override void LoadConfig()
        {
            var neuronsIds = this.GetConfig().Get(Constants.Param.Neurons, new long[] { Constants.UnknownId });
            foreach (var neuronId in neuronsIds)
            {
                AddNeuron(neuronId);
            }
        }

        public override void SetConfig(Config config)
        {
            throw new InvalidOperationException();
        }

        public override bool IsValid()
        {
            return Neurons.All(neuron => neuron.IsValid());
        }

        public override void SaveConfig()
        {
            var ids = Neurons.Select(n => n.Id);
            this.GetConfig().Set(Constants.Param.Neurons, ids);

            Neurons.ToList().ForEach(n => n.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            this.GetConfig().Remove(Constants.Param.Neurons);
            Neurons.ToList().ForEach(n => n.RemoveFromConfig());
        }

        public override void InvalidateValue()
        {
            throw new InvalidOperationException();
        }

        //

        public override void LayerControl_OnLoaded()
        {
            RefreshContent();
        }

        public void RefreshContent()
        {
            CtlContent.Content = null;
            CtlContent.Content = CtlNeurons;
        }

        public override bool IsHidden => true;

        public override NeuronBaseControl AddNeuron(long id)
        {
            NeuronControl neuron = new(id, this.GetConfig(), this.GetUIHandler(), this);

            Neurons.Add(neuron);
            CtlNeurons.Items.Add(neuron);

            RefreshContent();

            if (id == Constants.UnknownId)
            {
                ActionsManager.Instance.Add(new()
                {
                    CancelAction = () =>
                    {
                        Neurons.Remove(neuron);
                        CtlNeurons.Items.Remove(neuron);
                        RefreshNeuronsOrdinalNumbers();
                    }
                });

                OnChanged(Notification.ParameterChanged.NeuronsCount);
            }

            RefreshNeuronsOrdinalNumbers();

            return neuron;
        }

        public override int RemoveNeuron(NeuronBaseControl neuron)
        {
            int result = 0;

            if (!CanNeuronBeRemoved(neuron))
            {
                MessageBox.Show("At least one neuron must exist.", "Warning", MessageBoxButton.OK);
                return result;
            }

            var selectedNeurons = Neurons.Where(n => n.IsSelected).ToList();

            if (NeuronsSelector.Instance.IsSelected(neuron))
            {
                selectedNeurons.ForEach(n => n.SetRemovingState(true));
            }
            else
            {
                neuron.SetRemovingState(true);
            }

            if (MessageBoxResult.OK == 
                    MessageBox.Show("Would you really like to remove the neuron(s)?", "Confirm", MessageBoxButton.OKCancel))
            {
                if (NeuronsSelector.Instance.IsSelected(neuron))
                {
                    selectedNeurons.ForEach(RemoveNeuronWithoutConfirmation);
                    result += selectedNeurons.Count;
                }
                else
                {
                    RemoveNeuronWithoutConfirmation(neuron);
                    ++result;
                }

                return result;
            }

            if (NeuronsSelector.Instance.IsSelected(neuron))
            {
                selectedNeurons.ForEach(n => n.SetRemovingState(false));
            }
            else
            {
                neuron.SetRemovingState(false);
            }

            return result;
        }

        private void RemoveNeuronWithoutConfirmation(NeuronBaseControl neuron)
        {
            Neurons.Remove(neuron);
            CtlNeurons.Items.Remove(neuron);

            neuron.RemoveFromConfig();
            neuron.SaveConfig();

            RefreshNeuronsOrdinalNumbers();

            OnChanged(Notification.ParameterChanged.NeuronsCount);
        }

        public override void SetAllNeuronsSelected(bool isSelected)
        {
            Range.ForEach(Neurons, n => n.IsSelected = isSelected);
        }
    }
}

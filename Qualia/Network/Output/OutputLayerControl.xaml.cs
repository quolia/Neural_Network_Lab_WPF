using Qualia.Tools;
using System;
using System.Linq;
using System.Windows;

namespace Qualia.Controls
{
    sealed public partial class OutputLayerControl : LayerBaseControl
    {
        public OutputLayerControl(long id, Config config, ActionManager.ApplyActionDelegate onChanged)
            : base(id, config, onChanged)
        {
            InitializeComponent();

            LoadConfig();
        }

        public override void LoadConfig()
        {
            var neuronIds = this.GetConfig().Get(Constants.Param.Neurons, Array.Empty<long>());
            neuronIds.ToList().ForEach(id => AddNeuron(id));

            //if (neuronIds.Length == 0)
            //{
            //    Range.For(Constants.DefaultOutputNeuronsCount, _ => AddNeuron(Constants.UnknownId));
            //}
        }

        public override void LayerControl_OnLoaded()
        {
            RefreshContent();
        }

        public void RefreshContent()
        {
            CtlContent.Content = null;
            CtlContent.Content = CtlNeurons;
        }

        public override bool IsOutput => true;

        private void MenuAddNeuron_OnClick(object sender, EventArgs e)
        {
            if (!ActionManager.Instance.IsValid)
            {
                Messages.ShowError("Cannot add neuron. Editor has invalid value.");
                return;
            }

            AddNeuron(Constants.UnknownId);
        }

        public override NeuronBaseControl AddNeuron(long neuronId)
        {
            OutputNeuronControl neuron = new(neuronId, this.GetConfig(), this.GetUIHandler(), this);
            
            Neurons.Add(neuron);
            CtlNeurons.Items.Add(neuron);

            RefreshContent();

            if (neuronId == Constants.UnknownId)
            {
                OnChanged(new(this, Notification.ParameterChanged.NeuronsAdded));
            }

            RefreshNeuronsOrdinalNumbers();
            
            return neuron;
        }

        public override bool CanNeuronBeAdded() => false;
        public override bool CanNeuronBeRemoved(NeuronBaseControl neuron) => false;

        public override int RemoveNeuron(NeuronBaseControl neuron)
        {
            MessageBox.Show("Output neuron cannot be removed.", "Warning", MessageBoxButton.OK);
            return 0;
        }

        //

        public override bool IsValid()
        {
            return Neurons.All(n => n.IsValid());
        }

        public override void SaveConfig()
        {
            this.GetConfig().Set(Constants.Param.Neurons, Neurons.Select(n => n.VisualId));
            Neurons.ToList().ForEach(n => n.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            this.GetConfig().Remove(Constants.Param.Neurons);
            Neurons.ToList().ForEach(n => n.RemoveFromConfig());
        }

        public void SetTaskFunction(TaskFunction taskFunction)
        {
            var newCount = taskFunction != null ? taskFunction.VisualControl.GetOutputClasses().Count : 0;
            var count = Neurons.Count;

            if (newCount > count)
            {
                for (int i = count; i < newCount; ++i)
                {
                    AddNeuron();
                }
            }
            else if (newCount < count)
            {
                for (int i = newCount; i < count; ++i)
                {
                    CtlNeurons.Items.RemoveAt(CtlNeurons.Items.Count - 1);
                    Neurons.Remove(Neurons.Last());
                }
            }
        }

        public override void SetConfig(Config config)
        {
            throw new InvalidOperationException();
        }

        public override void InvalidateValue()
        {
            throw new InvalidOperationException();
        }

        //

        public override void SetAllNeuronsSelected(bool isSelected)
        {
            Range.ForEach(Neurons, n => n.IsSelected = isSelected);
        }
    }
}

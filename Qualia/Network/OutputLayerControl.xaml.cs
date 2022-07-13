using Qualia.Tools;
using System;
using System.Linq;
using System.Windows;

namespace Qualia.Controls
{
    sealed public partial class OutputLayerControl : LayerBaseControl
    {
        public OutputLayerControl(long id, Config config, Action<Notification.ParameterChanged> networkUI_OnChanged)
            : base(id, config, networkUI_OnChanged)
        {
            InitializeComponent();

            LoadConfig();
        }

        public override void LoadConfig()
        {
            var neuronIds = Config.Get(Constants.Param.Neurons, Array.Empty<long>());
            neuronIds.ToList().ForEach(AddNeuron);

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
            AddNeuron(Constants.UnknownId);
        }

        public override void AddNeuron(long neuronId)
        {
            OutputNeuronControl neuron = new(neuronId, Config, _onChanged, this);
            
            Neurons.Add(neuron);
            CtlNeurons.Items.Add(neuron);

            RefreshContent();

            if (neuronId == Constants.UnknownId)
            {
                _onChanged(Notification.ParameterChanged.NeuronsCount);
            }

            RefreshNeuronsOrdinalNumbers();
        }

        public override bool RemoveNeuron(NeuronBaseControl neuron)
        {
            MessageBox.Show("Output neuron cannot be removed.", "Warning", MessageBoxButton.OK);
            return false;
        }

        public override bool IsValid()
        {
            return Neurons.All(n => n.IsValid());
        }

        public override void SaveConfig()
        {
            Config.Set(Constants.Param.Neurons, Neurons.Select(n => n.Id));
            Neurons.ToList().ForEach(n => n.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            Config.Remove(Constants.Param.Neurons);
            Neurons.ToList().ForEach(n => n.RemoveFromConfig());
        }

        public void NetworkTask_OnChanged(TaskFunction taskFunction)
        {
            for (int i = Neurons.Count; i < taskFunction.ITaskControl.GetClasses().Count; ++i)
            {
                AddNeuron();
            }

            for (int i = taskFunction.ITaskControl.GetClasses().Count; i < Neurons.Count; ++i)
            {
                CtlNeurons.Items.RemoveAt(CtlNeurons.Items.Count - 1);
                Neurons.RemoveAt(Neurons.Count - 1);
            }
        }

        public override void SetConfig(Config config)
        {
            throw new InvalidOperationException();
        }

        public override void SetOnChangeEvent(Action<Notification.ParameterChanged> action)
        {
            throw new InvalidOperationException();
        }

        public override void InvalidateValue()
        {
            throw new InvalidOperationException();
        }
    }
}

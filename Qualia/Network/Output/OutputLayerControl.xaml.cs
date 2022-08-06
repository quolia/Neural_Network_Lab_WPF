﻿using Qualia.Tools;
using System;
using System.Linq;
using System.Windows;

namespace Qualia.Controls
{
    sealed public partial class OutputLayerControl : LayerBaseControl
    {
        public OutputLayerControl(long id, Config config, Action<Notification.ParameterChanged> onChanged)
            : base(id, config, onChanged)
        {
            InitializeComponent();

            LoadConfig();
        }

        public override void LoadConfig()
        {
            var neuronIds = _config.Get(Constants.Param.Neurons, Array.Empty<long>());
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
            OutputNeuronControl neuron = new(neuronId, _config, _onChanged, this);
            
            Neurons.Add(neuron);
            CtlNeurons.Items.Add(neuron);

            RefreshContent();

            if (neuronId == Constants.UnknownId)
            {
                OnChanged(Notification.ParameterChanged.NeuronsCount);
            }

            RefreshNeuronsOrdinalNumbers();
        }

        public override bool CanNeuronBeAdded() => false;
        public override bool CanNeuronBeRemoved() => false;

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
            _config.Set(Constants.Param.Neurons, Neurons.Select(n => n.Id));
            Neurons.ToList().ForEach(n => n.SaveConfig());
        }

        public override void RemoveFromConfig()
        {
            _config.Remove(Constants.Param.Neurons);
            Neurons.ToList().ForEach(n => n.RemoveFromConfig());
        }

        public void NetworkTask_OnChanged(TaskFunction taskFunction)
        {
            var newCount = taskFunction.VisualControl.GetOutputClasses().Count;
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
                    Neurons.Remove(Neurons.Last());// At(Neurons.Count - 1);
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

        public override void SelectAllNeurons()
        {
            foreach (var neuron in Neurons)
            {
                neuron.IsSelected = true;
            }
        }

        public override void DeselectAllNeurons()
        {
            foreach (var neuron in Neurons)
            {
                neuron.IsSelected = false;
            }
        }
    }
}

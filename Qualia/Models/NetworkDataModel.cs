using ILGPU;
using ILGPU.Runtime;
using Qualia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Tools;

namespace Qualia
{
    public class NetworkDataModel
    {
        public long VisualId;
        public ListX<LayerDataModel> Layers;
        public double[] TargetValues;
        public int TargetOutput;
        public List<string> Classes;

        public bool IsEnabled;

        public Color Color;
        public double LearningRate;
        public string RandomizeMode;
        public double? RandomizerParamA;
        public double InputInitial0;
        public double InputInitial1;

        public ICostFunction CostFunction;

        public Statistics Statistics;
        public DynamicStatistics DynamicStatistics;
        public ErrorMatrix ErrorMatrix;
        public Dictionary<string, string> LastStatistics;
        
        public NetworkDataModel(long visualId, int[] layersSize)
        {
            VisualId = visualId;
            Layers = new ListX<LayerDataModel>(layersSize.Length);
            Range.For(layersSize.Length, n => CreateLayer(layersSize[n], n < layersSize.Length - 1 ? layersSize[n + 1] : 0));

            TargetValues = new double[Layers.Last().Height];
        }

        public void CreateLayer(int neuronCount, int weightsCount)
        {
            Layers.Add(new LayerDataModel(Layers.Count, neuronCount, weightsCount));
        }

        public double InputThreshold => (InputInitial0 + InputInitial1) / 2;

        public NeuronDataModel GetMaxActivatedOutputNeuron()
        {
            var neurons = Layers.Last().Neurons;
            var max = neurons.First();
            foreach (var neuron in neurons)
            {
                if (neuron.Activation > max.Activation)
                {
                    max = neuron;
                }
            };

            return max;
        }

        public void InitState()
        {
            Layers.First().Neurons.ForEach(n => n.Activation = InputInitial1);
            Tools.RandomizeMode.Helper.Invoke(RandomizeMode, this, RandomizerParamA);
        }

        public void Activate()
        {
            Layers.ForEach(layer => layer.Neurons.ForEach(n => n.Activation = InputInitial1));
            Tools.RandomizeMode.Helper.Invoke(RandomizeMode, this, RandomizerParamA);
        }

        public int GetNumberOfFirstLayerActiveNeurons()
        {
            return Layers.First().Neurons.Count(n => !n.IsBias && n.Activation > InputThreshold);
        }

        public int GetTarget()
        {
            for (int i = 0; i < TargetValues.Length; ++i)
            {
                if (TargetValues[i] == 1)
                {
                    return i;
                }
            }
            return -1;
        }

        public void FeedForward()
        {
            foreach (var layer in Layers)
            {
                if (layer == Layers.Last())
                {
                    break;
                }

                foreach (var nextNeuron in layer.Next.Neurons)
                {
                    if (nextNeuron.IsBiasConnected && nextNeuron.IsBias)
                    {
                        nextNeuron.Activation = 0;

                        foreach (var neuron in layer.Neurons)
                        {
                            nextNeuron.Activation += neuron.IsBias ? neuron.AxW(nextNeuron) : 0;
                        }

                        nextNeuron.Activation = nextNeuron.ActivationFunction.Do(nextNeuron.Activation, nextNeuron.ActivationFuncParamA);
                    }

                    if (!nextNeuron.IsBias)
                    {
                        nextNeuron.Activation = 0;

                        foreach (var neuron in layer.Neurons)
                        {
                            nextNeuron.Activation += neuron.Activation == 0 ? 0 : neuron.AxW(nextNeuron);
                        }

                        nextNeuron.Activation = nextNeuron.ActivationFunction.Do(nextNeuron.Activation, nextNeuron.ActivationFuncParamA);
                    }

                    // not connected bias doesn't change it's activation
                }
            }
        }

        public void BackPropagation()
        {
            // backpropogation

            //ClearErrors();
            foreach (var neuron in Layers.Last().Neurons)
            {
                neuron.Error = CostFunction.Derivative(this, neuron) * neuron.ActivationFunction.Derivative(neuron.Activation, neuron.ActivationFuncParamA);
            }

            var layer = Layers.Last();

            while (layer != Layers.First())
            {
                foreach (var neuronPrev in layer.Previous.Neurons)
                {
                    neuronPrev.Error = 0;

                    foreach (var neuron in layer.Neurons)
                    {
                        neuronPrev.Error +=
                        
                            neuronPrev.IsBias
                            ?
                                neuron.IsBiasConnected
                                ? (neuron.Error * neuronPrev.WeightTo(neuron).Weight * neuronPrev.ActivationFunction.Derivative(neuronPrev.Activation, neuronPrev.ActivationFuncParamA))
                                : 0 // neuron.Error * neuronPrev.WeightTo(neuron).Weight * neuronPrev.ActivationFunction.Derivative(neuronPrev.Activation, neuronPrev.ActivationFuncParamA);
                            
                            : neuron.Error * neuronPrev.WeightTo(neuron).Weight * neuronPrev.ActivationFunction.Derivative(neuronPrev.Activation, neuronPrev.ActivationFuncParamA);
                    }
                }

                layer = layer.Previous;
            }

            // update weights

            layer = Layers.Last();

            while (layer != Layers.First())
            {
                foreach (var neuronPrev in layer.Previous.Neurons)
                {
                    foreach (var neuron in layer.Neurons)
                    {
                        neuronPrev.WeightTo(neuron).Add(neuron.Error * neuronPrev.Activation * LearningRate);
                    }
                }

                layer = layer.Previous;
            }
        }

        public NetworkDataModel Merge(NetworkDataModel newModel)
        {
            newModel.Statistics = Statistics;
            newModel.DynamicStatistics = DynamicStatistics;
            //newModel.ErrorMatrix = ErrorMatrix;

            foreach (var newLayer in newModel.Layers)
            {
                var layer = Layers.Find(l => l.VisualId == newLayer.VisualId);
                if (layer != null)
                {
                    foreach (var newNeuron in newLayer.Neurons)
                    {
                        var neuron = layer.Neurons.Find(n => n.VisualId == newNeuron.VisualId);
                        if (neuron != null)
                        {
                            double initValue = InitializeMode.Helper.Invoke(newNeuron.WeightsInitializer, newNeuron.WeightsInitializerParamA);
                            if (InitializeMode.Helper.IsSkipValue(initValue))
                            {
                                foreach (var newWeight in newNeuron.Weights)
                                {
                                    var weight = neuron.Weights.Find(w => w.Id == newWeight.Id);
                                    if (weight != null)
                                    {
                                        newWeight.Weight = weight.Weight;
                                    }
                                }
                            }

                            if (newNeuron.IsBias)
                            {
                                initValue = InitializeMode.Helper.Invoke(newNeuron.ActivationInitializer, newNeuron.ActivationInitializerParamA);
                                if (InitializeMode.Helper.IsSkipValue(initValue))
                                {
                                    newNeuron.Activation = neuron.Activation;
                                }
                            }
                        }
                    }
                }
            }

            return newModel;
        }
    }
}

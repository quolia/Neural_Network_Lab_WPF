using Qualia.Controls;
using System.Collections.Generic;
using System.Windows.Media;
using Tools;

namespace Qualia
{
    public class NetworkDataModel : ListNode<NetworkDataModel>
    {
        public long VisualId;
        public ListX<LayerDataModel> Layers;
        public int TargetOutput;
        public List<string> Classes;

        public bool IsEnabled;

        public Color Color;
        public double LearningRate;
        public string RandomizeMode;
        public double? RandomizerParamA;
        public double InputInitial0;
        public double InputInitial1;
        public bool IsAdjustFirstLayerWeights;

        public ICostFunction CostFunction;

        public Statistics Statistics;
        public DynamicStatistics DynamicStatistics;
        public ErrorMatrix ErrorMatrix;
        public Dictionary<string, string> LastStatistics;

        public NetworkDataModel Copy { private get; set; }

        public NetworkDataModel(long visualId, int[] layersSize)
        {
            VisualId = visualId;
            Layers = new ListX<LayerDataModel>(layersSize.Length);
            Range.For(layersSize.Length, n => CreateLayer(layersSize[n], n < layersSize.Length - 1 ? layersSize[n + 1] : 0));
        }

        public void CreateLayer(int neuronCount, int weightsCount)
        {
            Layers.Add(new LayerDataModel(Layers.Count, neuronCount, weightsCount));
        }

        public double InputThreshold => (InputInitial0 + InputInitial1) / 2;

        public NeuronDataModel GetMaxActivatedOutputNeuron()
        {
            var neurons = Layers.Last().Neurons;
            var max = neurons[0];
            var neuron = max;
            while (neuron != null)
            {
                if (neuron.Activation > max.Activation)
                {
                    max = neuron;
                }

                neuron = neuron.Next;
            };

            return max;
        }

        public void ActivateFirstLayer()
        {
            Layers[0].Neurons.ForEach(n => n.Activation = InputInitial1);
            Tools.RandomizeMode.Helper.Invoke(RandomizeMode, this, RandomizerParamA);
        }

        public void ActivateNetwork()
        {
            Layers.ForEach(layer => layer.Neurons.ForEach(n => n.Activation = InputInitial1));
            Tools.RandomizeMode.Helper.Invoke(RandomizeMode, this, RandomizerParamA);
        }

        
        public void FeedForward2()
        {
            var layer = Layers[0];
            while (layer != Layers.Last())
            {
                var nextNeuron = layer.Next.Neurons[0];
                while (nextNeuron != null)
                {
                    if (nextNeuron.IsBiasConnected && nextNeuron.IsBias)
                    {
                        nextNeuron.Activation = 0;

                        var neuron = layer.Neurons[0];
                        while (neuron != null)
                        {
                            if (neuron.IsBias)
                            {
                                nextNeuron.Activation += neuron.AxW(nextNeuron);
                            }
                            neuron = neuron.Next;
                        }

                        nextNeuron.Activation = nextNeuron.ActivationFunction.Do(nextNeuron.Activation, nextNeuron.ActivationFuncParamA);
                    }

                    if (!nextNeuron.IsBias)
                    {
                        nextNeuron.Activation = 0;

                        var neuron = layer.Neurons[0];
                        while (neuron != null)
                        {
                            if (neuron.Activation != 0)
                            {
                                if (neuron.Activation == 1)
                                {
                                    nextNeuron.Activation += neuron.WeightTo(nextNeuron).Weight;
                                }
                                else
                                {
                                    nextNeuron.Activation += neuron.AxW(nextNeuron);
                                }
                            }
                            neuron = neuron.Next;
                        }

                        nextNeuron.Activation = nextNeuron.ActivationFunction.Do(nextNeuron.Activation, nextNeuron.ActivationFuncParamA);
                    }

                    nextNeuron = nextNeuron.Next;
                }

                // not connected bias doesn't change it's activation

                layer = layer.Next;
            }
        }

        unsafe public void FeedForward()
        {
            var layer = Layers[0];
            while (layer != Layers.Last())
            {
                var nextNeuron = layer.Next.Neurons[0];
                while (nextNeuron != null)
                {
                    double sum = 0;
                    var AxW = nextNeuron.ForwardHelper[0];
                    while (AxW != null)
                    {
                        sum += AxW.AxW;
                        AxW = AxW.Next;
                    }

                    nextNeuron.Activation = nextNeuron.ActivationFunction.Do(sum, nextNeuron.ActivationFuncParamA);
                    nextNeuron = nextNeuron.Next;
                }

                layer = layer.Next;
            }
        }

        unsafe public void BackPropagation()
        {
            var lastLayer = Layers.Last();
            var neuron = lastLayer.Neurons[0];
            while (neuron != null)
            {
                neuron.Error = CostFunction.Derivative(this, neuron) * neuron.ActivationFunction.Derivative(neuron.Activation, neuron.ActivationFuncParamA);
                neuron = neuron.Next;
            }

            var layer = lastLayer;
            var finalLayer = Layers[IsAdjustFirstLayerWeights ? 0 : 1];
            while (layer != finalLayer)
            {
                neuron = layer.Neurons[0];
                while (neuron != null)
                {
                    var AxW = neuron.ForwardHelper[0];
                    while (AxW != null)
                    {
                        var prevNeuron = AxW.Neuron;
                        if (prevNeuron.Activation != 0)
                        {
                            prevNeuron.Error += neuron.Error * AxW.Weight.Weight * prevNeuron.ActivationFunction.Derivative(prevNeuron.Activation, prevNeuron.ActivationFuncParamA);
                        }
                        
                        AxW = AxW.Next;
                    }

                    neuron = neuron.Next;
                }

                layer = layer.Previous;
            }

            // update weights

            layer = lastLayer;
            while (layer != finalLayer)
            {
                neuron = layer.Neurons[0];
                while (neuron != null)
                {
                    var AxW = neuron.ForwardHelper[0];
                    while (AxW != null)
                    {
                        if (AxW.Neuron.Activation == 1)
                        {
                            AxW.Weight.Weight += neuron.Error * LearningRate;
                        }
                        else if (AxW.Neuron.Activation == 0)
                        {
                            // nothing
                        }
                        else
                        {
                            AxW.Weight.Weight += neuron.Error * AxW.Neuron.Activation * LearningRate;
                        }
                        AxW = AxW.Next;
                    }

                    neuron.Error = 0;
                    neuron = neuron.Next;
                }

                layer = layer.Previous;
            }
        }

        unsafe public void BackPropagation2()
        {
            var lastLayer = Layers.Last();
            var neuron = lastLayer.Neurons[0];
            while (neuron != null)
            {
                neuron.Error = CostFunction.Derivative(this, neuron) * neuron.ActivationFunction.Derivative(neuron.Activation, neuron.ActivationFuncParamA);
                neuron = neuron.Next;
            }

            var layer = lastLayer;
            var finalLayer = Layers[IsAdjustFirstLayerWeights ? 0 : 1];
            while (layer != finalLayer)
            {
                var neuronPrev = layer.Previous.Neurons[0];
                while (neuronPrev != null)
                {
                    neuronPrev.Error = 0;

                    neuron = layer.Neurons[0];
                    while (neuron != null)
                    {
                        if (neuronPrev.IsBias)
                        {
                            if (neuron.IsBiasConnected)
                            {
                                neuronPrev.Error += neuron.Error * neuronPrev.WeightTo(neuron).Weight * neuronPrev.ActivationFunction.Derivative(neuronPrev.Activation, neuronPrev.ActivationFuncParamA);
                            }
                        }
                        else
                        {
                            neuronPrev.Error += neuron.Error * neuronPrev.WeightTo(neuron).Weight * neuronPrev.ActivationFunction.Derivative(neuronPrev.Activation, neuronPrev.ActivationFuncParamA);
                        }

                        neuron = neuron.Next;
                    }

                    neuronPrev = neuronPrev.Next;
                }

                layer = layer.Previous;
            }

            // update weights

            layer = lastLayer;
            while (layer != finalLayer)
            {
                var neuronPrev = layer.Previous.Neurons[0];
                while (neuronPrev != null)
                {
                    neuron = layer.Neurons[0];
                    while (neuron != null)
                    {
                        if (neuronPrev.Activation != 0)
                        {
                            if (neuronPrev.Activation == 1)
                            {
                                neuronPrev.WeightTo(neuron).Add(neuron.Error * LearningRate);
                            }
                            else
                            {
                                neuronPrev.WeightTo(neuron).Add(neuron.Error * neuronPrev.Activation * LearningRate);
                            }
                        }

                        neuron = neuron.Next;
                    }

                    neuronPrev = neuronPrev.Next;
                }

                layer = layer.Previous;
            }
        }

        public NetworkDataModel Merge(NetworkDataModel newModel)
        {
            newModel.Statistics = Statistics;
            newModel.DynamicStatistics = DynamicStatistics;

            foreach (var newLayer in newModel.Layers)
            {
                var layer = Layers.Find(l => l.VisualId == newLayer.VisualId);
                if (layer != null)
                {
                    foreach (var newNeuron in newLayer.Neurons)
                    {
                        var neuron = layer.Neurons.Find(n => n.VisualId == newNeuron.VisualId);
                        if (neuron == null)
                        {
                            double initValue = InitializeMode.Helper.Invoke(newNeuron.WeightsInitializer, newNeuron.WeightsInitializerParamA);
                            if (!InitializeMode.Helper.IsSkipValue(initValue))
                            {
                                foreach (var newWeight in newNeuron.Weights)
                                {
                                    newWeight.Weight = initValue;
                                }
                            }
                        }
                        else
                        {
                            foreach (var newWeight in newNeuron.Weights)
                            {
                                var weight = neuron.Weights.Find(w => w.Id == newWeight.Id);
                                if (weight != null)
                                {
                                    newWeight.Weight = weight.Weight;
                                }
                            }

                            if (newNeuron.IsBias)
                            {
                                double initValue = InitializeMode.Helper.Invoke(newNeuron.ActivationInitializer, newNeuron.ActivationInitializerParamA);
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

        public NetworkDataModel GetCopyForRender()
        {
            var layer = Layers[0];
            var layer2 = Copy.Layers[0];
            while (layer != null)
            {
                var neuron = layer.Neurons[0];
                var neuron2 = layer2.Neurons[0];
                while (neuron != null)
                {
                    neuron2.Activation = neuron.Activation;

                    if (neuron.Activation != 0 && layer != Layers.Last())
                    {
                        var weight = neuron.Weights[0];
                        var weight2 = neuron2.Weights[0];
                        while (weight != null)
                        {
                            weight2.Weight = weight.Weight;

                            weight = weight.Next;
                            weight2 = weight2.Next;
                        }
                    }

                    neuron = neuron.Next;
                    neuron2 = neuron2.Next;
                }

                layer = layer.Next;
                layer2 = layer2.Next;
            }

            return Copy;
        }
    }
}

using Qualia.Controls;
using Qualia.Tools;
using System.Collections.Generic;
using System.Windows.Media;

namespace Qualia.Model
{
    unsafe sealed public class NetworkDataModel : ListXNode<NetworkDataModel>
    {
        public readonly long VisualId;
        public readonly ListX<LayerDataModel> Layers;
        public int TargetOutputNeuronId;
        public List<string> OutputClasses;

        public bool IsEnabled;

        public Color Color;
        public double LearningRate;
        public RandomizeFunction RandomizeMode;
        public double RandomizerParam;
        public double InputInitial0;
        public double InputInitial1;
        public bool IsAdjustFirstLayerWeights;

        public CostFunction CostFunction;
        public BackPropagationStrategy BackPropagationStrategy;

        public Statistics Statistics;
        public PlotterStatistics PlotterStatistics;
        public ErrorMatrix ErrorMatrix;

        private NetworkDataModel _copy;

        public NetworkDataModel(long visualId, int[] layersSizes)
        {
            VisualId = visualId;
            Layers = new(layersSizes.Length);
            Range.For(layersSizes.Length, i => CreateLayer(layersSizes[i], i < layersSizes.Length - 1 ? layersSizes[i + 1] : 0));
        }

        public void SetCopy(NetworkDataModel networkModelCopy)
        {
            _copy = networkModelCopy;
        }

        public void CreateLayer(int neuronCount, int weightsCount)
        {
            Layers.Add(new(Layers.Count, neuronCount, weightsCount));
        }

        public double InputThreshold => (InputInitial0 + InputInitial1) / 2;

        public NeuronDataModel GetMaxActivatedOutputNeuron()
        {
            var maxNeuron = Layers.Last.Neurons.First;
            var neuron = maxNeuron;

            while (neuron != null)
            {
                if (neuron.Activation > maxNeuron.Activation)
                {
                    maxNeuron = neuron;
                }

                neuron = neuron.Next;
            };

            return maxNeuron;
        }

        public void ActivateFirstLayer()
        {
            Layers.First.Neurons.ForEach(neuron => neuron.Activation = InputInitial1);
            RandomizeMode.Do(this, RandomizerParam);
        }

        public void ActivateNetwork()
        {
            Layers.ForEach(layer => layer.Neurons.ForEach(neuron => neuron.Activation = InputInitial1));
            RandomizeMode.Do(this, RandomizerParam);
        }

        unsafe public void FeedForward()
        {
            var layer = Layers.First;
            var lastLayer = Layers.Last;
            double sum;

            NeuronDataModel nextLayerNeuron;
            ForwardNeuron AxW;

            while (layer != lastLayer)
            {
                nextLayerNeuron = layer.Next.Neurons.First;
                while (nextLayerNeuron != null)
                {
                    sum = 0;
                    AxW = nextLayerNeuron.WeightsToPreviousLayer.First;
                    while (AxW != null)
                    {
                        sum += AxW.AxW;
                        AxW = AxW.Next;
                    }

                    nextLayerNeuron.X = sum;
                    nextLayerNeuron.Activation = nextLayerNeuron.ActivationFunction.Do(sum, nextLayerNeuron.ActivationFunctionParam);
                    nextLayerNeuron = nextLayerNeuron.Next;
                }

                layer = layer.Next;
            }
        }

        unsafe public void BackPropagation()
        {
            var lastLayer = Layers.Last;
            var neuron = lastLayer.Neurons.First;

            while (neuron != null)
            {
                neuron.Error = CostFunction.Derivative(this, neuron) * neuron.ActivationFunction.Derivative(neuron.X, neuron.Activation, neuron.ActivationFunctionParam);
                neuron = neuron.Next;
            }

            var layer = lastLayer;
            var finalLayer = IsAdjustFirstLayerWeights ? Layers.First : Layers.First.Next;

            ForwardNeuron AxW;
            NeuronDataModel prevNeuron;

            while (layer != finalLayer)
            {
                neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    AxW = neuron.WeightsToPreviousLayer.First;
                    while (AxW != null)
                    {
                        prevNeuron = AxW.Neuron;
                        //if (prevNeuron.Activation != 0)
                        {
                            prevNeuron.Error += neuron.Error * AxW.Weight.Weight * prevNeuron.ActivationFunction.Derivative(prevNeuron.X, prevNeuron.Activation, prevNeuron.ActivationFunctionParam);
                        }

                        AxW = AxW.Next;
                    }

                    neuron = neuron.Next;
                }

                // update weights

                neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    AxW = neuron.WeightsToPreviousLayer.First;
                    while (AxW != null)
                    {
                        if (AxW.Neuron.Activation != 0)
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

        public NetworkDataModel Merge(NetworkDataModel newNetwork)
        {
            newNetwork.PlotterStatistics = PlotterStatistics;

            var newLayer = newNetwork.Layers.First;
            while (newLayer != null)
            {
                var nextNewLayer = newLayer.Next;

                var layer = Layers.Find(l => l.VisualId == newLayer.VisualId);
                if (layer != null)
                {
                    var newNeuron = newLayer.Neurons.First;
                    while (newNeuron != null)
                    {
                        var neuron = layer.Neurons.Find(n => n.VisualId == newNeuron.VisualId);
                        var newWeight = newNeuron.Weights.First;
                        while (newWeight != null)
                        {
                            var weight = neuron?.Weights.Find(w => w.Id == newWeight.Id);
                            if (weight != null)
                            {
                                newWeight.Weight = weight.Weight;
                            }
                            else
                            {
                                newWeight.Weight = 0; // The new neuron weight.
                            }

                            newWeight = newWeight.Next;
                        }

                        if (nextNewLayer != null)
                        {
                            var nextNewNeuron = nextNewLayer.Neurons.First;
                            while (nextNewNeuron != null)
                            {
                                var weightToPrev = nextNewNeuron.WeightsToPreviousLayer.Find(w => w.Neuron == newNeuron);
                                weightToPrev.Weight.Weight = newNeuron.WeightTo(nextNewNeuron).Weight;

                                nextNewNeuron = nextNewNeuron.Next;
                            }
                        }


                        newNeuron = newNeuron.Next;
                    }
                }

                newLayer = newLayer.Next;
            }

            return newNetwork;
        }

        public NetworkDataModel GetCopyToDraw()
        {
            var layer = Layers.First;
            var layerCopy = _copy.Layers.First;

            while (layer != null)
            {
                var neuron = layer.Neurons.First;
                var neuronCopy = layerCopy.Neurons.First;

                while (neuron != null)
                {
                    neuronCopy.X = neuron.X;
                    neuronCopy.Activation = neuron.Activation;

                    if (layer.Next == null) // Output layer.
                    {
                        neuronCopy.Label = neuron.Label;
                        neuronCopy.PositiveTargetValue = neuron.PositiveTargetValue;
                        neuronCopy.NegativeTargetValue = neuron.NegativeTargetValue;
                    }

                    if (neuron.Activation != 0 && layer != Layers.Last)
                    {
                        var weight = neuron.Weights.First;
                        var weightCopy = neuronCopy.Weights.First;

                        while (weight != null)
                        {
                            weightCopy.Weight = weight.Weight;
                           
                            weight = weight.Next;
                            weightCopy = weightCopy.Next;
                        }
                    }

                    neuron = neuron.Next;
                    neuronCopy = neuronCopy.Next;
                }

                layer = layer.Next;
                layerCopy = layerCopy.Next;
            }

            _copy.TargetOutputNeuronId = TargetOutputNeuronId;

            return _copy;
        }
    }
}

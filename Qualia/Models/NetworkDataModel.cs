using System;
using System.Collections.Generic;
using System.Windows.Media;
using Qualia.Controls;
using Tools;

namespace Qualia
{
    sealed public class NetworkDataModel : ListXNode<NetworkDataModel>
    {
        public readonly long VisualId;
        public readonly ListX<LayerDataModel> Layers;
        public int TargetOutput;
        public List<string> Classes;

        public bool IsEnabled;

        public Color Color;
        public double LearningRate;
        public string RandomizeMode;
        public double? RandomizerParam;
        public double InputInitial0;
        public double InputInitial1;
        public bool IsAdjustFirstLayerWeights;

        public ICostFunction CostFunction;

        public Statistics Statistics;
        public DynamicStatistics DynamicStatistics;
        public ErrorMatrix ErrorMatrix;
        public Dictionary<string, string> LastStatistics;

        private NetworkDataModel _copy;

        public NetworkDataModel(long visualId, int[] layerSizes)
        {
            VisualId = visualId;
            Layers = new ListX<LayerDataModel>(layerSizes.Length);
            Range.For(layerSizes.Length, i => CreateLayer(layerSizes[i], i < layerSizes.Length - 1 ? layerSizes[i + 1] : 0));
        }

        public void SetCopy(NetworkDataModel networkModelCopy)
        {
            _copy = networkModelCopy;
        }

        public void CreateLayer(int neuronCount, int weightsCount)
        {
            Layers.Add(new LayerDataModel(Layers.Count, neuronCount, weightsCount));
        }

        public double InputThreshold => (InputInitial0 + InputInitial1) / 2;

        public NeuronDataModel GetMaxActivatedOutputNeuron()
        {
            var maxNeuronModel = Layers.Last.Neurons.First;
            var neuronModel = maxNeuronModel;

            while (neuronModel != null)
            {
                if (neuronModel.Activation > maxNeuronModel.Activation)
                {
                    maxNeuronModel = neuronModel;
                }

                neuronModel = neuronModel.Next;
            };

            return maxNeuronModel;
        }

        public void ActivateFirstLayer()
        {
            Layers.First.Neurons.ForEach(neuronModel => neuronModel.Activation = InputInitial1);
            Tools.RandomizeMode.Helper.Invoke(RandomizeMode, this, RandomizerParam);
        }

        public void ActivateNetwork()
        {
            Layers.ForEach(layer => layer.Neurons.ForEach(neuronModel => neuronModel.Activation = InputInitial1));
            Tools.RandomizeMode.Helper.Invoke(RandomizeMode, this, RandomizerParam);
        }

        /*
        public void FeedForward2()
        {
            var layerModel = Layers.First;
            while (layerModel != Layers.Last)
            {
                var nextNeuron = layerModel.Next.Neurons.First;
                while (nextNeuron != null)
                {
                    if (nextNeuron.IsBiasConnected && nextNeuron.IsBias)
                    {
                        nextNeuron.Activation = 0;

                        var neuron = layerModel.Neurons.First;
                        while (neuron != null)
                        {
                            if (neuron.IsBias)
                            {
                                nextNeuron.Activation += neuron.AxW(nextNeuron);
                            }
                            neuron = neuron.Next;
                        }

                        nextNeuron.Activation = nextNeuron.ActivationFunction.Do(nextNeuron.Activation, nextNeuron.ActivationFuncParam);
                    }

                    if (!nextNeuron.IsBias)
                    {
                        nextNeuron.Activation = 0;

                        var neuron = layerModel.Neurons.First;
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

                        nextNeuron.Activation = nextNeuron.ActivationFunction.Do(nextNeuron.Activation, nextNeuron.ActivationFuncParam);
                    }

                    nextNeuron = nextNeuron.Next;
                }

                // not connected bias doesn't change it's activation

                layerModel = layerModel.Next;
            }
        }
        */

        unsafe public void FeedForward()
        {
            var layer = Layers.First;
            var lastLayer = Layers.Last;
            double sum;

            while (layer != lastLayer)
            {
                var neuron = layer.Next.Neurons.First;
                while (neuron != null)
                {
                    sum = 0;
                    var AxW = neuron.WeightsToNextLayer.First;
                    while (AxW != null)
                    {
                        sum += AxW.AxW;
                        AxW = AxW.Next;
                    }

                    neuron.Activation = neuron.ActivationFunction.Do(sum, neuron.ActivationFuncParam);
                    neuron = neuron.Next;
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
                neuron.Error = CostFunction.Derivative(this, neuron) * neuron.ActivationFunction.Derivative(neuron.Activation, neuron.ActivationFuncParam);
                neuron = neuron.Next;
            }

            var layer = lastLayer;
            var finalLayer = IsAdjustFirstLayerWeights ? Layers.First : Layers.First.Next;

            while (layer != finalLayer)
            {
                neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    var AxW = neuron.WeightsToNextLayer.First;
                    while (AxW != null)
                    {
                        var prevNeuron = AxW.Neuron;
                        if (prevNeuron.Activation != 0)
                        {
                            prevNeuron.Error += neuron.Error * AxW.WeightModel.Weight * prevNeuron.ActivationFunction.Derivative(prevNeuron.Activation, prevNeuron.ActivationFuncParam);
                        }

                        AxW = AxW.Next;
                    }

                    neuron = neuron.Next;
                }

                // update weights

                neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    var AxW = neuron.WeightsToNextLayer.First;
                    while (AxW != null)
                    {
                        if (AxW.Neuron.Activation != 0)
                        {
                            AxW.WeightModel.Weight += neuron.Error * AxW.Neuron.Activation * LearningRate;
                        }

                        AxW = AxW.Next;
                    }

                    neuron.Error = 0;
                    neuron = neuron.Next;
                }

                layer = layer.Previous;
            }
        }

        /*unsafe public void BackPropagation2()
        {
            var lastLayer = Layers.Last;
            var neuron = lastLayer.Neurons.First;

            while (neuron != null)
            {
                neuron.Error = CostFunction.Derivative(this, neuron) * neuron.ActivationFunction.Derivative(neuron.Activation, neuron.ActivationFuncParam);
                neuron = neuron.Next;
            }

            var layer = lastLayer;
            var finalLayer = Layers[IsAdjustFirstLayerWeights ? 0 : 1];

            while (layer != finalLayer)
            {
                var neuronPrev = layer.Previous.Neurons.First;
                while (neuronPrev != null)
                {
                    neuronPrev.Error = 0;

                    neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        if (neuronPrev.IsBias)
                        {
                            if (neuron.IsBiasConnected)
                            {
                                neuronPrev.Error += neuron.Error * neuronPrev.WeightTo(neuron).Weight * neuronPrev.ActivationFunction.Derivative(neuronPrev.Activation, neuronPrev.ActivationFuncParam);
                            }
                        }
                        else
                        {
                            neuronPrev.Error += neuron.Error * neuronPrev.WeightTo(neuron).Weight * neuronPrev.ActivationFunction.Derivative(neuronPrev.Activation, neuronPrev.ActivationFuncParam);
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
                var neuronPrev = layer.Previous.Neurons.First;
                while (neuronPrev != null)
                {
                    neuron = layer.Neurons.First;
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
        */

        public NetworkDataModel Merge(NetworkDataModel newNetworkModel)
        {
            newNetworkModel.Statistics = Statistics;
            newNetworkModel.DynamicStatistics = DynamicStatistics;

            var newLayerModel = newNetworkModel.Layers.First;
            while (newLayerModel != null)
            {
                var layerModel = Layers.Find(l => l.VisualId == newLayerModel.VisualId);
                if (layerModel != null)
                {
                    var newNeuronModel = newLayerModel.Neurons.First;
                    while (newNeuronModel != null)
                    {
                        var neuronModel = layerModel.Neurons.Find(n => n.VisualId == newNeuronModel.VisualId);
                        if (neuronModel == null)
                        {
                            double initValue = InitializeMode.Helper.Invoke(newNeuronModel.WeightsInitializer, newNeuronModel.WeightsInitializerParam);
                            if (!InitializeMode.Helper.IsSkipValue(initValue))
                            {
                                var newWeightModel = newNeuronModel.Weights.First;
                                while (newWeightModel != null)
                                {
                                    newWeightModel.Weight = initValue;
                                    newWeightModel = newWeightModel.Next;
                                }
                            }
                        }
                        else
                        {
                            var newWeightModel = newNeuronModel.Weights.First;
                            while (newWeightModel != null)
                            {
                                var weightModel = neuronModel.Weights.Find(w => w.Id == newWeightModel.Id);
                                if (weightModel != null)
                                {
                                    newWeightModel.Weight = weightModel.Weight;
                                }

                                newWeightModel = newWeightModel.Next;
                            }

                            if (newNeuronModel.IsBias)
                            {
                                double initValue = InitializeMode.Helper.Invoke(newNeuronModel.ActivationInitializer, newNeuronModel.ActivationInitializerParam);
                                if (InitializeMode.Helper.IsSkipValue(initValue))
                                {
                                    newNeuronModel.Activation = neuronModel.Activation;
                                }
                            }
                        }

                        newNeuronModel = newNeuronModel.Next;
                    }
                }

                newLayerModel = newLayerModel.Next;
            }

            return newNetworkModel;
        }

        public NetworkDataModel GetCopyForRender()
        {
            var layerModel = Layers.First;
            var layerModelCopy = _copy.Layers.First;

            while (layerModel != null)
            {
                var neuronModel = layerModel.Neurons.First;
                var neuronModelCopy = layerModelCopy.Neurons.First;

                while (neuronModel != null)
                {
                    neuronModelCopy.Activation = neuronModel.Activation;

                    if (neuronModel.Activation != 0 && layerModel != Layers.Last)
                    {
                        var weightModel = neuronModel.Weights.First;
                        var weightModelCopy = neuronModelCopy.Weights.First;

                        while (weightModel != null)
                        {
                            weightModelCopy.Weight = weightModel.Weight;

                            weightModel = weightModel.Next;
                            weightModelCopy = weightModelCopy.Next;
                        }
                    }

                    neuronModel = neuronModel.Next;
                    neuronModelCopy = neuronModelCopy.Next;
                }

                layerModel = layerModel.Next;
                layerModelCopy = layerModelCopy.Next;
            }

            return _copy;
        }
    }
}

using Qualia.Model;
using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Qualia.Controls
{
    sealed public partial class NetworkPresenter : BaseUserControl
    {
        private const int NEURON_MAX_DIST = 40;
        private const int HORIZONTAL_OFFSET = 10;
        private const int TOP_OFFSET = 10;
        private const int BOTTOM_OFFSET = 25;
        private const int NEURON_SIZE = 8;
        private const double NEURON_RADIUS = NEURON_SIZE / 2;
        private const int BIAS_SIZE = 14;
        private const double BIAS_RADIUS = BIAS_SIZE / 2;

        public long ResizeTicks;

        private readonly Dictionary<NeuronDataModel, Point> _coordinator = new();
        private readonly Dictionary<WeightDataModel, double> _prevWeights = new();

        private readonly Pen _penChange = Draw.GetPen(in ColorsX.Lime);
        private readonly Pen _biasPen = Draw.GetPen(in ColorsX.Orange);

        private readonly Typeface _activationLabelsFont = new(new("Tahoma"),
                                                              FontStyles.Normal,
                                                              FontWeights.Bold,
                                                              FontStretches.Normal);

        private readonly Typeface _neuronLabelsFont = new(new("Tahoma"),
                                                          FontStyles.Normal,
                                                          FontWeights.Bold,
                                                          FontStretches.Normal);
        public NetworkPresenter()
        {
            InitializeComponent();
            
            _penChange.Freeze();
            _biasPen.Freeze();

            SizeChanged += NetworkPresenter_OnSizeChanged;
        }

        private void NetworkPresenter_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //ClearCache();
        }

        public void OnSizeChanged(NetworksManager networksManager,
                                  bool isRunning,
                                  bool isUseWeightsColors,
                                  bool isOnlyChangedWeights,
                                  bool isHighlightChangedWeights,
                                  bool isShowOnlyUnchangedWeights,
                                  bool isShowActivationLabels)
        {
            //CtlNetworkPresenter.Height = QMath.Max(CtlNetworkPresenter.Height, 400);

            if (networksManager == null)
            {
                return;
            }

            var ticks = DateTime.UtcNow.Ticks;
            ResizeTicks = ticks;

            this.Dispatch(() =>
            {
                if (ResizeTicks != ticks)
                {
                    return;
                }

                if (isRunning)
                {
                    ClearCache();
                    RenderRunning(networksManager.SelectedNetworkModel,
                                  isUseWeightsColors,
                                  isOnlyChangedWeights,
                                  isHighlightChangedWeights,
                                  isShowOnlyUnchangedWeights,
                                  isShowActivationLabels);
                }
                else
                {
                    RenderStanding(networksManager.SelectedNetworkModel);
                }

            }, DispatcherPriority.Render);
        }

        public int LayerDistance(NetworkDataModel model!!)
        {
            return (int)(ActualWidth - 2 * HORIZONTAL_OFFSET) / (model.Layers.Count - 1);
        }

        private float VerticalDistance(int count)
        {
            return MathX.Min(((float)ActualHeight - TOP_OFFSET - BOTTOM_OFFSET) / count, NEURON_MAX_DIST);
        }

        private int LayerX(NetworkDataModel networkModel, LayerDataModel layerModel)
        {
            return HORIZONTAL_OFFSET + LayerDistance(networkModel) * layerModel.Id;
        }

        private new float MaxHeight(NetworkDataModel networkModel)
        {
            return networkModel.Layers.Max(layer => layer.Neurons.Count * VerticalDistance(layer.Neurons.Count));
        }

        private float VerticalShift(NetworkDataModel networkModel, LayerDataModel layerModel)
        {
            return (MaxHeight(networkModel) - layerModel.Neurons.Count * VerticalDistance(layerModel.Neurons.Count)) / 2;
        }

        private void DrawLayersLinks(bool fullState,
                                     NetworkDataModel networkModel,
                                     LayerDataModel layerModel1,
                                     LayerDataModel layerModel2,
                                     bool isUseColorOfWeight,
                                     bool isShowOnlyChangedWeights,
                                     bool isHighlightChangedWeights,
                                     bool isShowOnlyUnchangedWeights,
                                     bool isShowActivationLabels)
        {
            double threshold = networkModel.Layers.First == layerModel1 ? networkModel.InputThreshold : 0;

            NeuronDataModel prevNeuronModel = null;
            NeuronDataModel neuronModel1 = layerModel1.Neurons.First;

            while (neuronModel1 != null)
            {
                if (!_coordinator.ContainsKey(neuronModel1))
                {
                    _coordinator.Add(neuronModel1,
                                     Points.Get(LayerX(networkModel, layerModel1),
                                                TOP_OFFSET + VerticalShift(networkModel, layerModel1) + neuronModel1.Id * VerticalDistance(layerModel1.Neurons.Count)));
                }

                // Skip intersected neurons on first layer to improove performance.
                if (!fullState && !neuronModel1.IsBias && networkModel.Layers.First == layerModel1 && prevNeuronModel != null)
                {
                    if (_coordinator[neuronModel1].Y - _coordinator[prevNeuronModel].Y < NEURON_SIZE)
                    {
                        neuronModel1 = neuronModel1.Next;
                        continue;
                    }
                }

                if (fullState || neuronModel1.IsBias || neuronModel1.Activation > threshold)
                {
                    var neuronModel2 = layerModel2.Neurons.First;
                    while (neuronModel2 != null)
                    {
                        if (!neuronModel2.IsBias || (neuronModel2.IsBiasConnected && neuronModel1.IsBias))
                        {
                            if (fullState || ((neuronModel1.IsBias || neuronModel1.Activation > threshold) && neuronModel1.AxW(neuronModel2) != 0))
                            {
                                Pen pen = null;
                                double opacity = 1;
                                const double opacityFactor = 1000;

                                bool isWeightChanged = false;
                                var weightModel = neuronModel1.WeightTo(neuronModel2);

                                if (_prevWeights.TryGetValue(weightModel, out double prevWeight))
                                {
                                    if (prevWeight != weightModel.Weight)
                                    {
                                        _prevWeights[weightModel] = weightModel.Weight;
                                        isWeightChanged = true;
                                    }
                                }
                                else
                                {
                                    prevWeight = 0;
                                    //_prevWeights.Add(weightModel, weightModel.Weight);
                                    _prevWeights.Add(weightModel, 0);
                                    isWeightChanged = true;
                                }

                                if (isWeightChanged && isShowOnlyChangedWeights)
                                {
                                    double changeFraction = (prevWeight - weightModel.Weight) / prevWeight;
                                    if (changeFraction < 0.00001 && changeFraction > -0.00001)
                                    {
                                        isWeightChanged = false;
                                        //opacity = 1 / opacityFactor / 2;
                                    }

                                    //opacity = MathX.Max(0, opacity - MathX.Abs(changeFraction * opacityFactor));
                                    //isWeightChanged = false;

                                    if (false)
                                    {
                                        if (changeFraction < 0.000001 && changeFraction > -0.000001)
                                        {
                                            isWeightChanged = false;
                                        }
                                        else if (changeFraction < 0.00001 && changeFraction > -0.00001)
                                        {
                                            isWeightChanged = false;
                                            opacity = 1 / opacityFactor;
                                        }
                                        else if (changeFraction < 0.0001 && changeFraction > -0.0001)
                                        {
                                            isWeightChanged = false;
                                            opacity = 1 / opacityFactor / 2;
                                        }
                                        else if (changeFraction < 0.001 && changeFraction > -0.001)
                                        {
                                            isWeightChanged = false;
                                            opacity = 1 / opacityFactor / 3;
                                        }
                                        else if (changeFraction < 0.01 && changeFraction > -0.01)
                                        {
                                            isWeightChanged = false;
                                            opacity = 1 / opacityFactor / 4;
                                        }
                                    }
                                }

                                if (opacity > 0)
                                {
                                    if (isShowOnlyChangedWeights)
                                    {
                                        if (isWeightChanged)
                                        {
                                            if (!isShowOnlyUnchangedWeights)
                                            {
                                                if (isHighlightChangedWeights)
                                                {
                                                    pen = _penChange;
                                                }
                                                else
                                                {
                                                    if (isUseColorOfWeight)
                                                    {
                                                        pen = Draw.GetPen(weightModel.Weight, 1);
                                                    }
                                                    else
                                                    {
                                                        pen = Draw.GetPen(neuronModel1.AxW(neuronModel2), 1);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (isUseColorOfWeight)
                                            {
                                                pen = Draw.GetPen(weightModel.Weight, 1);
                                            }
                                            else
                                            {
                                                pen = Draw.GetPen(neuronModel1.AxW(neuronModel2), 1);
                                            }
                                        }
                                    }
                                    else // show all weights
                                    {
                                        if (isShowOnlyUnchangedWeights)
                                        {
                                            if (!isWeightChanged)
                                            {
                                                if (isUseColorOfWeight)
                                                {
                                                    pen = Draw.GetPen(weightModel.Weight, 1);
                                                }
                                                else
                                                {
                                                    pen = Draw.GetPen(neuronModel1.AxW(neuronModel2), 1);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (isWeightChanged)
                                            {
                                                if (isHighlightChangedWeights)
                                                {
                                                    pen = _penChange;
                                                }
                                            }

                                            if (pen == null)
                                            {
                                                if (isUseColorOfWeight)
                                                {
                                                    pen = Draw.GetPen(weightModel.Weight, 1);
                                                }
                                                else
                                                {
                                                    pen = Draw.GetPen(neuronModel1.AxW(neuronModel2), 1);
                                                }
                                            }
                                        }
                                    }
                                }

                                if (pen != null && isShowOnlyUnchangedWeights)
                                {
                                    if (weightModel.Weight == prevWeight)
                                    {
                                        pen.Thickness = 1.25;
                                    }

                                    pen.Brush.Opacity = opacity;
                                }

                                if (pen != null)
                                {
                                    if (!_coordinator.ContainsKey(neuronModel2))
                                    {
                                        _coordinator.Add(neuronModel2, Points.Get(LayerX(networkModel, layerModel2),
                                                                                  TOP_OFFSET + VerticalShift(networkModel, layerModel2) + neuronModel2.Id * VerticalDistance(layerModel2.Neurons.Count)));
                                    }

                                    var point1 = _coordinator[neuronModel1];
                                    var point2 = _coordinator[neuronModel2];

                                    CtlCanvas.DrawLine(pen, ref point1, ref point2);
                                    prevNeuronModel = neuronModel1;
                                }
                            }
                        }

                        neuronModel2 = neuronModel2.Next;
                    }
                }

                neuronModel1 = neuronModel1.Next;
            }
        }

        private void DrawLayerNeurons(bool fullState, NetworkDataModel network, LayerDataModel layer)
        {
            double threshold = network.Layers.First == layer ? network.InputThreshold : 0;

            NeuronDataModel prevNeuron = null;
            NeuronDataModel neuron = layer.Neurons.First;

            var output = network.GetMaxActivatedOutputNeuron().Id;

            while (neuron != null)
            {
                if (fullState || neuron.IsBias || neuron.Activation > threshold || layer.Id > 0)
                {
                    if (!_coordinator.ContainsKey(neuron))
                    {
                        _coordinator.Add(neuron,
                                         Points.Get(LayerX(network, layer),
                                         TOP_OFFSET + VerticalShift(network, layer) + neuron.Id * VerticalDistance(layer.Neurons.Count)));
                    }

                    // Skip intersected neurons on the first layer to improve performance.
                    if (!fullState && !neuron.IsBias && network.Layers.First == layer && prevNeuron != null)
                    {
                        if (_coordinator[neuron].Y - _coordinator[prevNeuron].Y < NEURON_SIZE)
                        {
                            neuron = neuron.Next;
                            continue;
                        }
                    }
                    prevNeuron = neuron;

                    var pen = Draw.GetPen(neuron.Activation);
                    var brush = pen.Brush;

                    var centerPoint = _coordinator[neuron];

                    if (neuron.IsBias)
                    {
                        CtlCanvas.DrawEllipse(Brushes.Orange, _biasPen, ref centerPoint, BIAS_RADIUS, BIAS_RADIUS);
                    }

                    var radius = NEURON_RADIUS;

                    if (layer == network.Layers.Last)
                    {
                        if (neuron.Id == network.TargetOutputNeuronId)
                        {
                            brush = Draw.GetBrush(in ColorsX.Yellow);
                        }

                        if (neuron.Id == output)
                        {
                            radius *= 1.5;
                        }
                    }

                    CtlCanvas.DrawEllipse(brush, pen, ref centerPoint, radius, radius);
                    
                    if (layer.Next == null // Output layer.
                        && !string.IsNullOrEmpty(neuron.Label)) 
                    {
                        FormattedText text = new(neuron.Label,
                                                 Culture.Current,
                                                 FlowDirection.LeftToRight,
                                                 _neuronLabelsFont,
                                                 10,
                                                 Draw.GetBrush(in ColorsX.Black),
                                                 RenderSettings.PixelsPerDip);

                        CtlCanvas.DrawText(text, ref Points.Get(centerPoint.X - text.Width / 2, centerPoint.Y + NEURON_RADIUS + 3));
                    }
                }

                neuron = neuron.Next;
            }
        }

        private void DrawNeuronsActivationLabels(bool fullState, NetworkDataModel networkModel, LayerDataModel layerModel, bool isShowActivationLabels)
        {
            const int TEXT_OFFSET = 6;

            double threshold = networkModel.Layers.First == layerModel ? networkModel.InputThreshold : 0;

            NeuronDataModel prevNeuron = null;
            NeuronDataModel neuronModel = layerModel.Neurons.First;

            while (neuronModel != null)
            {
                if (fullState || neuronModel.IsBias || neuronModel.Activation > threshold || layerModel.Id > 0)
                {
                    if (!_coordinator.ContainsKey(neuronModel))
                    {
                        _coordinator.Add(neuronModel,
                                         Points.Get(LayerX(networkModel, layerModel),
                                         TOP_OFFSET + VerticalShift(networkModel, layerModel) + neuronModel.Id * VerticalDistance(layerModel.Neurons.Count)));
                    }

                    // Skip intersected neurons on the first layer to improve performance.
                    if (!fullState && !neuronModel.IsBias && networkModel.Layers.First == layerModel && prevNeuron != null)
                    {
                        if (_coordinator[neuronModel].Y - _coordinator[prevNeuron].Y < NEURON_SIZE)
                        {
                            neuronModel = neuronModel.Next;
                            continue;
                        }
                    }
                    prevNeuron = neuronModel;

                    var centerPoint = _coordinator[neuronModel];

                    FormattedText text = new(Converter.DoubleToText(neuronModel.Activation, "auto"),
                                             Culture.Current,
                                             FlowDirection.LeftToRight,
                                             _activationLabelsFont,
                                             10,
                                             Draw.GetBrush(in ColorsX.Black),
                                             RenderSettings.PixelsPerDip);

                    var x = layerModel.Next == null // Output layer.
                            ? centerPoint.X - text.Width - TEXT_OFFSET * 2 - NEURON_RADIUS * 2
                            : centerPoint.X;

                    CtlCanvas.DrawRectangle(Draw.GetBrush(Draw.GetColor(240, in ColorsX.Yellow)),
                                            null,
                                            ref Rects.Get(x + NEURON_RADIUS * 2,
                                                          centerPoint.Y - NEURON_RADIUS * 1.5,
                                                          text.Width + TEXT_OFFSET,
                                                          text.Height));

                    CtlCanvas.DrawText(text, ref Points.Get(x + NEURON_RADIUS * 2 + TEXT_OFFSET / 2, centerPoint.Y - NEURON_RADIUS * 1.5));
                }

                neuronModel = neuronModel.Next;
            }
        }

        private void Render(bool fullState,
                            NetworkDataModel networkModel,
                            bool isUseWeightsColors,
                            bool isOnlyChangedWeights,
                            bool isHighlightChangedWeights,
                            bool isShowOnlyUnchangedWeights,
                            bool isShowActivationLabels)
        {
            CtlCanvas.Clear();

            if (networkModel == null)
            {
                return;
            }

            //lock (Main.ApplyChangesLocker)
            {
                if (networkModel.Layers.Count == 0)
                {
                    return;
                }

                var lastLayerModel = networkModel.Layers.Last;

                var layerModel = networkModel.Layers.First;
                while (layerModel != lastLayerModel)
                {
                    DrawLayersLinks(fullState,
                                    networkModel,
                                    layerModel,
                                    layerModel.Next,
                                    isUseWeightsColors,
                                    isOnlyChangedWeights,
                                    isHighlightChangedWeights,
                                    isShowOnlyUnchangedWeights,
                                    isShowActivationLabels);
                    
                    layerModel = layerModel.Next;
                }

                layerModel = networkModel.Layers.First;
                while (layerModel != null)
                {
                    DrawLayerNeurons(fullState, networkModel, layerModel);
                    layerModel = layerModel.Next;
                }

                if (isShowActivationLabels)
                {
                    layerModel = networkModel.Layers.First;
                    while (layerModel != null)
                    {
                        DrawNeuronsActivationLabels(fullState, networkModel, layerModel, isShowActivationLabels);
                        layerModel = layerModel.Next;
                    }
                }
            }
        }

        public void ClearCache()
        {
            _coordinator.Clear();
        }

        public void RenderStanding(NetworkDataModel networkModel)
        {
            ClearCache();
            Render(false, networkModel, false, false, false, false, false);
        }

        public void RenderRunning(NetworkDataModel networkModel,
                                  bool isUseWeightsColors,
                                  bool isOnlyChangedWeights,
                                  bool isHighlightChangedWeights,
                                  bool isShowOnlyUnchangedWeights,
                                  bool isShowActivationLabels)
        {
            Render(false,
                   networkModel,
                   isUseWeightsColors,
                   isOnlyChangedWeights,
                   isHighlightChangedWeights,
                   isShowOnlyUnchangedWeights,
                   isShowActivationLabels);
        }
    }
}

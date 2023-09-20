using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Qualia.Controls.Base;
using Qualia.Models;
using Qualia.Tools;

namespace Qualia.Controls.Presenter;

public sealed partial class NetworkPresenter : BaseUserControl
{
    private const int NEURON_MAX_DIST = 40;
    private const int HORIZONTAL_OFFSET = 10;
    private const int TOP_OFFSET = 10;
    private const int BOTTOM_OFFSET = 25;
    private const int NEURON_SIZE = 8;
    private const double NEURON_RADIUS = NEURON_SIZE / 2;

    private long _resizeTicks;

    private NetworkDataModel _networkModel;

    private readonly Dictionary<NeuronDataModel, Point> _coordinator = new();
    private readonly Dictionary<WeightDataModel, double> _prevWeights = new();

    private readonly Pen _penChange = Draw.GetPen(in ColorsX.Lime);
    private readonly Pen _biasPen = Draw.GetPen(in ColorsX.Orange);

    private readonly Typeface _activationLabelsFont = new(new FontFamily("Tahoma"),
        FontStyles.Normal,
        FontWeights.Bold,
        FontStretches.Normal);

    private readonly Typeface _neuronLabelsFont = new(new FontFamily("Tahoma"),
        FontStyles.Normal,
        FontWeights.Bold,
        FontStretches.Normal);
    
    public NetworkPresenter()
        : base(0)
    {
        InitializeComponent();
            
        _penChange.Freeze();
        _biasPen.Freeze();

        SizeChanged += NetworkPresenter_OnSizeChanged;
    }

    private void NetworkPresenter_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        //  
    }

    public void OnSizeChanged(bool isRunning,
        bool isUseWeightsColors,
        bool isOnlyChangedWeights,
        bool isHighlightChangedWeights,
        bool isShowOnlyUnchangedWeights,
        bool isShowActivationLabels)
    {
        if (_networkModel == null)
        {
            return;
        }

        var ticks = DateTime.UtcNow.Ticks;
        _resizeTicks = ticks;

        this.Dispatch(() =>
        {
            if (_resizeTicks != ticks)
            {
                return;
            }

            if (isRunning)
            {
                ClearCache();
                RenderRunning(_networkModel,
                    isUseWeightsColors,
                    isOnlyChangedWeights,
                    isHighlightChangedWeights,
                    isShowOnlyUnchangedWeights,
                    isShowActivationLabels);
            }
            else
            {
                RenderStanding(_networkModel);
            }

        }, DispatcherPriority.Render);
    }

    public void ClearCache()
    {
        _coordinator.Clear();
    }

    public void RenderStanding(NetworkDataModel networkModel)
    {
        ClearCache();
        Render(true, networkModel, false, false, false, false, false);
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
    
    private int GetLayerDistance(NetworkDataModel network)
    {
        return (int)(ActualWidth - 2 * HORIZONTAL_OFFSET) / (network.Layers.Count - 1);
    }

    private float GetVerticalDistance(int count)
    {
        return MathX.Min(((float)ActualHeight - TOP_OFFSET - BOTTOM_OFFSET) / count, NEURON_MAX_DIST);
    }

    private int GetLayerX(NetworkDataModel network, LayerDataModel layer)
    {
        return HORIZONTAL_OFFSET + GetLayerDistance(network) * layer.Id;
    }

    private new float GetMaxHeight(NetworkDataModel network)
    {
        return network.Layers.Max(layer => layer.Neurons.Count * GetVerticalDistance(layer.Neurons.Count));
    }

    private float GetVerticalShift(NetworkDataModel network, LayerDataModel layer)
    {
        return (GetMaxHeight(network) - layer.Neurons.Count * GetVerticalDistance(layer.Neurons.Count)) / 2;
    }

    private void DrawLayersLinks(bool fullState,
        NetworkDataModel network,
        LayerDataModel layer1,
        LayerDataModel layer2,
        bool isUseColorOfWeight,
        bool isShowOnlyChangedWeights,
        bool isHighlightChangedWeights,
        bool isShowOnlyUnchangedWeights)
    {
        NeuronDataModel prevNeuron = null;
        var neuron1 = layer1.Neurons.First;

        while (neuron1 != null)
        {
            if (!_coordinator.ContainsKey(neuron1))
            {
                _coordinator.Add(neuron1,
                    Points.Get(GetLayerX(network, layer1),
                        TOP_OFFSET + GetVerticalShift(network, layer1) + neuron1.Id * GetVerticalDistance(layer1.Neurons.Count)));
            }

            // Skip intersected neurons on first layer to improove performance.
            if (!fullState && layer1.IsInputLayer && prevNeuron != null)
            {
                if (_coordinator[neuron1].Y - _coordinator[prevNeuron].Y < NEURON_SIZE)
                {
                    neuron1 = neuron1.Next;
                    continue;
                }
            }

            if (fullState || layer1.Previous != null || neuron1.Activation == network.InputInitial1)
            {
                var neuron2 = layer2.Neurons.First;
                while (neuron2 != null)
                {
                    Pen pen = null;
                    double opacity = 1;

                    var isWeightChanged = false;
                    var weightModel = neuron1.WeightTo(neuron2);

                    if (_prevWeights.TryGetValue(weightModel, out var prevWeight))
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
                        _prevWeights.Add(weightModel, 0);
                        isWeightChanged = true;
                    }

                    if (isWeightChanged && isShowOnlyChangedWeights)
                    {
                        var changeFraction = (prevWeight - weightModel.Weight) / prevWeight;
                        if (changeFraction < 0.00001 && changeFraction > -0.00001)
                        {
                            isWeightChanged = false;
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
                                            pen = Draw.GetPen(neuron1.AxW(neuron2), 1);
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
                                    pen = Draw.GetPen(neuron1.AxW(neuron2), 1);
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
                                        pen = Draw.GetPen(neuron1.AxW(neuron2), 1);
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
                                        pen = Draw.GetPen(neuron1.AxW(neuron2), 1);
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
                        if (!_coordinator.ContainsKey(neuron2))
                        {
                            _coordinator.Add(neuron2, Points.Get(GetLayerX(network, layer2),
                                TOP_OFFSET + GetVerticalShift(network, layer2) + neuron2.Id * GetVerticalDistance(layer2.Neurons.Count)));
                        }

                        var point1 = _coordinator[neuron1];
                        var point2 = _coordinator[neuron2];

                        CtlCanvas.DrawLine(pen, ref point1, ref point2);
                        prevNeuron = neuron1;
                    }

                    neuron2 = neuron2.Next;
                }
            }

            neuron1 = neuron1.Next;
        }
    }

    private void DrawLayerNeurons(bool fullState, NetworkDataModel network, LayerDataModel layer)
    {
        NeuronDataModel prevNeuron = null;
        var neuron = layer.Neurons.First;

        var output = network.GetMaxActivatedOutputNeuron().Id;

        while (neuron != null)
        {
            if (fullState || layer.Previous != null || neuron.Activation == network.InputInitial1)
            {
                if (!_coordinator.ContainsKey(neuron))
                {
                    _coordinator.Add(neuron,
                        Points.Get(GetLayerX(network, layer),
                            TOP_OFFSET + GetVerticalShift(network, layer) + neuron.Id * GetVerticalDistance(layer.Neurons.Count)));
                }

                // Skip intersected neurons on the first layer to improve performance.
                if (!fullState && network.Layers.First == layer && prevNeuron != null)
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
                    
                if (layer.IsOutputLayer
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

    private void DrawNeuronsActivationLabels(bool fullState, NetworkDataModel networkModel, LayerDataModel layerModel)
    {
        const int TEXT_OFFSET = 6;

        NeuronDataModel prevNeuron = null;
        var neuronModel = layerModel.Neurons.First;

        while (neuronModel != null)
        {
            if (fullState || layerModel.Previous != null || neuronModel.Activation == networkModel.InputInitial1)
            {
                if (!_coordinator.ContainsKey(neuronModel))
                {
                    _coordinator.Add(neuronModel,
                        Points.Get(GetLayerX(networkModel, layerModel),
                            TOP_OFFSET + GetVerticalShift(networkModel, layerModel) + neuronModel.Id * GetVerticalDistance(layerModel.Neurons.Count)));
                }

                // Skip intersected neurons on the first layer to improve performance.
                if (!fullState && networkModel.Layers.First == layerModel && prevNeuron != null)
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

                var x = layerModel.IsOutputLayer
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
        _networkModel = networkModel;

        if (networkModel == null)
        {
            return;
        }

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
                isShowOnlyUnchangedWeights);
                
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
                DrawNeuronsActivationLabels(fullState, networkModel, layerModel);
                layerModel = layerModel.Next;
            }
        }
    }
}
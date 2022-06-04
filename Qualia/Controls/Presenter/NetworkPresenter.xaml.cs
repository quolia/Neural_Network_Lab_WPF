using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public partial class NetworkPresenter : UserControl
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

        private readonly Dictionary<NeuronDataModel, Point> _coordinator = new Dictionary<NeuronDataModel, Point>();
        private readonly Dictionary<WeightDataModel, double> _weightsData = new Dictionary<WeightDataModel, double>();

        public NetworkPresenter()
        {
            InitializeComponent();

            SizeChanged += NetworkPresenter_SizeChanged;
        }

        private void NetworkPresenter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _coordinator.Clear();
        }

        public int LayerDistance(NetworkDataModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return (int)(ActualWidth - 2 * HORIZONTAL_OFFSET) / (model.Layers.Count - 1);
        }

        private float VerticalDistance(int count)
        {
            return Math.Min(((float)ActualHeight - TOP_OFFSET - BOTTOM_OFFSET) / count, NEURON_MAX_DIST);
        }

        private int LayerX(NetworkDataModel networkModel, LayerDataModel layerModel)
        {
            return HORIZONTAL_OFFSET + LayerDistance(networkModel) * layerModel.Id;
        }

        private new float MaxHeight(NetworkDataModel networkModel)
        {
            return networkModel.Layers.Max(layer => layer.Height * VerticalDistance(layer.Height));
        }

        private float VerticalShift(NetworkDataModel networkModel, LayerDataModel layerModel)
        {
            return (MaxHeight(networkModel) - layerModel.Height * VerticalDistance(layerModel.Height)) / 2;
        }

        private void DrawLayersLinks(bool fullState, NetworkDataModel networkModel, LayerDataModel layerModel1, LayerDataModel layerModel2,
                                     bool isOnlyWeights, bool isOnlyChangedWeights, bool isHighlightChangedWeights)
        {
            double threshold = networkModel.Layers[0] == layerModel1 ? networkModel.InputThreshold : 0;

            NeuronDataModel prevNeuron = null;
            foreach (var neuron1 in layerModel1.Neurons)
            {
                if (!_coordinator.ContainsKey(neuron1))
                {
                    _coordinator.Add(neuron1, Points.Get(LayerX(networkModel, layerModel1), TOP_OFFSET + VerticalShift(networkModel, layerModel1) + neuron1.Id * VerticalDistance(layerModel1.Height)));
                }

                // Skip intersected neurons on first layer to improove performance.
                if (!fullState && !neuron1.IsBias && networkModel.Layers[0] == layerModel1 && prevNeuron != null)
                {
                    if (_coordinator[neuron1].Y - _coordinator[prevNeuron].Y < NEURON_SIZE)
                    {
                        continue;
                    }
                }

                if (fullState || neuron1.IsBias || neuron1.Activation > threshold)
                {
                    foreach (var neuron2 in layerModel2.Neurons)
                    {
                        if (!neuron2.IsBias || (neuron2.IsBiasConnected && neuron1.IsBias))
                        {
                            if (fullState || ((neuron1.IsBias || neuron1.Activation > threshold) && neuron1.AxW(neuron2) != 0))
                            {
                                Pen pen = null;
                                Pen penChange = null;

                                bool isWeightChanged = false;
                                var weight = neuron1.WeightTo(neuron2);

                                if (_weightsData.TryGetValue(weight, out double prevWeight))
                                {
                                    if (prevWeight != weight.Weight)
                                    {
                                        _weightsData[weight] = weight.Weight;
                                        isWeightChanged = true;
                                    }
                                }
                                else
                                {
                                    prevWeight = 0;
                                    _weightsData.Add(weight, weight.Weight);
                                    isWeightChanged = true;
                                }

                                if (isWeightChanged && isOnlyChangedWeights)
                                {
                                    double fraction = Math.Abs((prevWeight - weight.Weight) / prevWeight);
                                    if (fraction <= 0.001)
                                    {
                                        isWeightChanged = false;
                                    }
                                }

                                if (isWeightChanged && isHighlightChangedWeights)
                                {
                                    penChange = Tools.Draw.GetPen(Colors.Lime);
                                }

                                if (isOnlyWeights || isOnlyChangedWeights)
                                {
                                    if ((isWeightChanged && isOnlyChangedWeights) || !isOnlyChangedWeights)
                                    {
                                        pen = Tools.Draw.GetPen(weight.Weight, 1);
                                    }
                                }
                                else
                                {
                                    pen = Tools.Draw.GetPen(neuron1.AxW(neuron2), 1);
                                }

                                if (!isOnlyChangedWeights && isHighlightChangedWeights && isWeightChanged)
                                {
                                    pen = penChange;
                                    penChange = null;
                                }

                                if (pen != null && penChange != null)
                                {
                                    throw new Exception("Render error.");
                                }

                                if (pen != null)
                                {
                                    if (!_coordinator.ContainsKey(neuron2))
                                    {
                                        _coordinator.Add(neuron2, Points.Get(LayerX(networkModel, layerModel2), TOP_OFFSET + VerticalShift(networkModel, layerModel2) + neuron2.Id * VerticalDistance(layerModel2.Height)));
                                    }

                                    CtlPresenter.DrawLine(pen, _coordinator[neuron1], _coordinator[neuron2]);
                                    prevNeuron = neuron1;
                                }

                                if (penChange != null)
                                {
                                    CtlPresenter.DrawLine(penChange, _coordinator[neuron1], _coordinator[neuron2]);
                                    prevNeuron = neuron1;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawLayerNeurons(bool fullState, NetworkDataModel model, LayerDataModel layer)
        {
            double threshold = model.Layers[0] == layer ? model.InputThreshold : 0;

            var biasColor = Tools.Draw.GetPen(Colors.Orange);

            NeuronDataModel prevNeuron = null;
            foreach (var neuron in layer.Neurons)
            {
                if (fullState || neuron.IsBias || neuron.Activation > threshold || layer.Id > 0)
                {                   
                    if (!_coordinator.ContainsKey(neuron))
                    {
                        _coordinator.Add(neuron,
                                         Points.Get(LayerX(model, layer),
                                         TOP_OFFSET + VerticalShift(model, layer) + neuron.Id * VerticalDistance(layer.Height)));
                    }

                    // Skip intersected neurons on first layer to improove performance.
                    if (!fullState && !neuron.IsBias && model.Layers[0] == layer && prevNeuron != null)
                    {
                        if (_coordinator[neuron].Y - _coordinator[prevNeuron].Y < NEURON_SIZE)
                        {
                            continue;
                        }
                    }
                    prevNeuron = neuron;

                    var pen = Tools.Draw.GetPen(neuron.Activation);
                    var brush = pen.Brush;

                    if (neuron.IsBias)
                    {
                        CtlPresenter.DrawEllipse(Brushes.Orange, biasColor, _coordinator[neuron], BIAS_RADIUS, BIAS_RADIUS);
                    }

                    CtlPresenter.DrawEllipse(brush, pen, _coordinator[neuron], NEURON_RADIUS, NEURON_RADIUS);  
                }
            }
        }

        private void Draw(bool fullState, NetworkDataModel networkModel, bool isOnlyWeights, bool isOnlyChangedWeights, bool isHighlightChangedWeights)
        {
            if (networkModel == null)
            {
                CtlPresenter.Clear();
                return;
            }

            CtlPresenter.Clear();

            //lock (Main.ApplyChangesLocker)
            {
                if (networkModel.Layers.Count > 0)
                {
                    foreach (var layer in networkModel.Layers)
                    {
                        if (layer == networkModel.Layers.Last())
                        {
                            break;
                        }

                        DrawLayersLinks(fullState, networkModel, layer, layer.Next, isOnlyWeights, isOnlyChangedWeights, isHighlightChangedWeights);
                    }

                    foreach (var layer in networkModel.Layers)
                    {
                        DrawLayerNeurons(fullState, networkModel, layer);
                    }
                }
            }

            CtlPresenter.Update();
        }

        public void RenderStanding(NetworkDataModel model)
        {
            Draw(true, model, false, false, false);
        }

        public void RenderRunning(NetworkDataModel model, bool isOnlyWeights, bool isOnlyChangedWeights, bool isHighlightChangedWeights)
        {
            Draw(false, model, isOnlyWeights, isOnlyChangedWeights, isHighlightChangedWeights);
        }
    }
}

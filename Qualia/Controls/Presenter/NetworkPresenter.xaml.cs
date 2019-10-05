using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public partial class NetworkPresenter : UserControl
    {
        const int NEURON_MAX_DIST = 40;
        const int HORIZONTAL_OFFSET = 10;
        const int TOP_OFFSET = 10;
        const int BOTTOM_OFFSET = 25;
        const int NEURON_SIZE = 8;
        const double NEURON_RADIUS = NEURON_SIZE / 2;
        const int BIAS_SIZE = 14;
        const double BIAS_RADIUS = BIAS_SIZE / 2;

        public long ResizeTicks;

        readonly Dictionary<NeuronDataModel, Point> Coordinator = new Dictionary<NeuronDataModel, Point>();
        readonly Dictionary<WeightDataModel, double> WeightsData = new Dictionary<WeightDataModel, double>();

        public NetworkPresenter()
        {
            InitializeComponent();
            SizeChanged += NetworkPresenter_SizeChanged;
        }

        private void NetworkPresenter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Coordinator.Clear();
        }

        public int LayerDistance(NetworkDataModel model)
        {
            return (int)(ActualWidth - 2 * HORIZONTAL_OFFSET) / (model.Layers.Count - 1);
        }

        private float VerticalDistance(int count)
        {
            return Math.Min(((float)ActualHeight - TOP_OFFSET - BOTTOM_OFFSET) / count, NEURON_MAX_DIST);
        }

        private int LayerX(NetworkDataModel model, LayerDataModel layer)
        {
            return HORIZONTAL_OFFSET + LayerDistance(model) * layer.Id;
        }

        private new float MaxHeight(NetworkDataModel model)
        {
            return model.Layers.Max(layer => layer.Height * VerticalDistance(layer.Height));
        }

        private float VerticalShift(NetworkDataModel model, LayerDataModel layer)
        {
            return (MaxHeight(model) - layer.Height * VerticalDistance(layer.Height)) / 2;
        }

        private void DrawLayersLinks(bool fullState, NetworkDataModel model, LayerDataModel layer1, LayerDataModel layer2, bool isOnlyWeights, bool isOnlyChangedWeights, bool isHighlightChangedWeights)
        {
            double threshold = model.Layers[0] == layer1 ? model.InputThreshold : 0;

            NeuronDataModel prevNeuron = null;
            foreach (var neuron1 in layer1.Neurons)
            {
                if (!Coordinator.ContainsKey(neuron1))
                {
                    Coordinator.Add(neuron1, Points.Get(LayerX(model, layer1), TOP_OFFSET + VerticalShift(model, layer1) + neuron1.Id * VerticalDistance(layer1.Height)));
                }

                // Skip intersected neurons on first layer to improove performance.
                if (!fullState && !neuron1.IsBias && model.Layers[0] == layer1 && prevNeuron != null)
                {
                    if (Coordinator[neuron1].Y - Coordinator[prevNeuron].Y < NEURON_SIZE)
                    {
                        continue;
                    }
                }

                if (fullState || neuron1.IsBias || neuron1.Activation > threshold)
                {
                    foreach (var neuron2 in layer2.Neurons)
                    {
                        if (!neuron2.IsBias || (neuron2.IsBiasConnected && neuron1.IsBias))
                        {
                            if (fullState || ((neuron1.IsBias || neuron1.Activation > threshold) && neuron1.AxW(neuron2) != 0))
                            {
                                Pen pen = null;
                                Pen penChange = null;
                                bool isWeightChanged = false;
                                var weight = neuron1.WeightTo(neuron2);

                                if (WeightsData.TryGetValue(weight, out double prevWeight))
                                {
                                    if (prevWeight != weight.Weight)
                                    {
                                        isWeightChanged = true;
                                        WeightsData[weight] = weight.Weight;
                                    }
                                }
                                else
                                {
                                    prevWeight = 0;
                                    isWeightChanged = true;
                                    WeightsData.Add(weight, weight.Weight);
                                }

                                double fraction = Math.Min(1, Math.Abs((prevWeight - weight.Weight) / prevWeight));
                                if (fraction <= 0.001)
                                {
                                    isWeightChanged = false;
                                }

                                if (isWeightChanged && isHighlightChangedWeights)
                                {
                                    penChange = Tools.Draw.GetPen(Colors.Lime);
                                }

                                if (isOnlyWeights)
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

                                if (pen != null)
                                {
                                    if (!Coordinator.ContainsKey(neuron2))
                                    {
                                        Coordinator.Add(neuron2, Points.Get(LayerX(model, layer2), TOP_OFFSET + VerticalShift(model, layer2) + neuron2.Id * VerticalDistance(layer2.Height)));
                                    }

                                    CtlPresenter.DrawLine(pen, Coordinator[neuron1], Coordinator[neuron2]);
                                    prevNeuron = neuron1;
                                }

                                if (penChange != null)
                                {
                                    CtlPresenter.DrawLine(penChange, Coordinator[neuron1], Coordinator[neuron2]);
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
                    if (!Coordinator.ContainsKey(neuron))
                    {
                        Coordinator.Add(neuron, Points.Get(LayerX(model, layer), TOP_OFFSET + VerticalShift(model, layer) + neuron.Id * VerticalDistance(layer.Height)));
                    }

                    // Skip intersected neurons on first layer to improove performance.
                    if (!fullState && !neuron.IsBias && model.Layers[0] == layer && prevNeuron != null)
                    {
                        if (Coordinator[neuron].Y - Coordinator[prevNeuron].Y < NEURON_SIZE)
                        {
                            continue;
                        }
                    }
                    prevNeuron = neuron;

                    var pen = Tools.Draw.GetPen(neuron.Activation);
                    var brush = pen.Brush;

                    if (neuron.IsBias)
                    {
                        CtlPresenter.DrawEllipse(Brushes.Orange, biasColor, Coordinator[neuron], BIAS_RADIUS, BIAS_RADIUS);
                    }

                    CtlPresenter.DrawEllipse(brush, pen, Coordinator[neuron], NEURON_RADIUS, NEURON_RADIUS);  
                }
            }
        }

        private void Draw(bool fullState, NetworkDataModel model, bool isOnlyWeights, bool isOnlyChangedWeights, bool isHighlightChangedWeights)
        {
            if (model == null)
            {
                CtlPresenter.Clear();
                return;
            }

            CtlPresenter.Clear();

            //lock (Main.ApplyChangesLocker)
            {
                if (model.Layers.Count > 0)
                {
                    foreach (var layer in model.Layers)
                    {
                        if (layer == model.Layers.Last())
                        {
                            break;
                        }

                        DrawLayersLinks(fullState, model, layer, layer.Next, isOnlyWeights, isOnlyChangedWeights, isHighlightChangedWeights);
                    }

                    foreach (var layer in model.Layers)
                    {
                        DrawLayerNeurons(fullState, model, layer);
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

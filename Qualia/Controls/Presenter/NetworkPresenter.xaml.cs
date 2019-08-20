using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tools;

namespace Qualia.Controls
{
    public partial class NetworkPresenter : UserControl
    {
        const int NEURON_MAX_DIST = 40;
        const int HORIZONTAL_OFFSET = 10;
        const int VERTICAL_OFFSET = 10;
        const int NEURON_SIZE = 8;
        const double NEURON_RADIUS = NEURON_SIZE / 2;
        const int BIAS_SIZE = 14;
        const double BIAS_RADIUS = BIAS_SIZE / 2;

        Dictionary<NeuronDataModel, Point> Coordinator = new Dictionary<NeuronDataModel, Point>();

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
            return Math.Min(((float)ActualHeight - VERTICAL_OFFSET * 2) / count, NEURON_MAX_DIST);
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

        private void DrawLayersLinks(bool fullState, NetworkDataModel model, LayerDataModel layer1, LayerDataModel layer2)
        {
            double threshold = model.Layers.First() == layer1 ? model.InputThreshold : 0;

            foreach (var neuron1 in layer1.Neurons)
            {
                foreach (var neuron2 in layer2.Neurons)
                {
                    if (!neuron2.IsBias || (neuron2.IsBiasConnected && neuron1.IsBias))
                    {
                        if (fullState || ((neuron1.IsBias || neuron1.Activation > threshold) && neuron1.AxW(neuron2) != 0))
                        {
                            var pen = Tools.Draw.GetPen(neuron1.AxW(neuron2), 1);

                            if (!Coordinator.ContainsKey(neuron1))
                            {
                                Coordinator.Add(neuron1, new Point(LayerX(model, layer1), VERTICAL_OFFSET + VerticalShift(model, layer1) + neuron1.Id * VerticalDistance(layer1.Height)));
                            }

                            if (!Coordinator.ContainsKey(neuron2))
                            {
                                Coordinator.Add(neuron2, new Point(LayerX(model, layer2), VERTICAL_OFFSET + VerticalShift(model, layer2) + neuron2.Id * VerticalDistance(layer2.Height)));
                            }

                            CtlPresenter.DrawLine(pen, Coordinator[neuron1], Coordinator[neuron2]);
                        }
                    }
                }
            }
        }

        private void DrawLayerNeurons(bool fullState, NetworkDataModel model, LayerDataModel layer)
        {
            double threshold = model.Layers.First() == layer ? model.InputThreshold : 0;

            var biasColor = Tools.Draw.GetPen(Colors.Orange);

            foreach (var neuron in layer.Neurons)
            {
                if (fullState || (neuron.IsBias || neuron.Activation > threshold))
                {                   
                    var pen = Tools.Draw.GetPen(neuron.Activation);
                    var brush = pen.Brush; // Tools.Draw.GetBrush(neuron.Activation);

                    if (neuron.IsBias)
                    {
                        if (!Coordinator.ContainsKey(neuron))
                        {
                            Coordinator.Add(neuron, new Point(LayerX(model, layer), VERTICAL_OFFSET + VerticalShift(model, layer) + neuron.Id * VerticalDistance(layer.Height)));
                        }

                        CtlPresenter.DrawEllipse(Brushes.Orange, biasColor,
                                                 Coordinator[neuron],
                                                 BIAS_RADIUS, BIAS_RADIUS);
                    }

                    if (!Coordinator.ContainsKey(neuron))
                    {
                        Coordinator.Add(neuron, new Point(LayerX(model, layer), VERTICAL_OFFSET + VerticalShift(model, layer) + neuron.Id * VerticalDistance(layer.Height)));
                    }

                    CtlPresenter.DrawEllipse(brush, pen,
                                             Coordinator[neuron],
                                             NEURON_RADIUS, NEURON_RADIUS);  
                }
            }
        }

        private void Draw(bool fullState, NetworkDataModel model)
        {
            CtlPresenter.Clear();

            if (model == null)
            {               
                return;
            }

            lock (Main.ApplyChangesLocker)
            {
                if (model.Layers.Count > 0)
                {
                    foreach (var layer in model.Layers)
                    {
                        if (layer == model.Layers.Last())
                        {
                            break;
                        }

                        DrawLayersLinks(fullState, model, layer, layer.Next);
                    }
                }

                foreach (var layer in model.Layers)
                {
                    DrawLayerNeurons(fullState, model, layer);
                }
            }

            CtlPresenter.Update();
        }

        public void RenderStanding(NetworkDataModel model)
        {
            Draw(true, model);
        }

        public void RenderRunning(NetworkDataModel model)
        {
            Draw(false, model);
        }
    }
}

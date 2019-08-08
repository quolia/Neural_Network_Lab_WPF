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

        public NetworkPresenter()
        {
            InitializeComponent();

           
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

            Range.ForEach(layer1.Neurons, layer2.Neurons, (neuron1, neuron2) =>
            {
                if (!neuron2.IsBias || (neuron1.IsBias && neuron2.IsBiasConnected))
                {
                    if (fullState || ((neuron1.IsBias || neuron1.Activation > threshold) && neuron1.AxW(neuron2) != 0))
                    {
                        var pen = Tools.Draw.GetPen(neuron1.AxW(neuron2), 1);
                        
                        CtlPresenter.DrawLine(pen,
                                              new Point(LayerX(model, layer1), VERTICAL_OFFSET + VerticalShift(model, layer1) + neuron1.Id * VerticalDistance(layer1.Height)),
                                              new Point(LayerX(model, layer2), VERTICAL_OFFSET + VerticalShift(model, layer2) + neuron2.Id * VerticalDistance(layer2.Height)));
                    }
                }
            });
        }

        private void DrawLayerNeurons(bool fullState, NetworkDataModel model, LayerDataModel layer)
        {
            double threshold = model.Layers.First() == layer ? model.InputThreshold : 0;

            var biasColor = Tools.Draw.GetPen(Colors.Orange);

            Range.ForEach(layer.Neurons, neuron =>
            {
                if (fullState || (neuron.IsBias || neuron.Activation > threshold))
                {
                    var brush = Tools.Draw.GetBrush(neuron.Activation);
                    var pen = Tools.Draw.GetPen(neuron.Activation);


                    if (neuron.IsBias)
                    {
                        CtlPresenter.DrawEllipse(Brushes.Orange, biasColor,
                                                 new Point(LayerX(model, layer),
                                                 VERTICAL_OFFSET + VerticalShift(model, layer) + neuron.Id * VerticalDistance(layer.Height)),
                                                 BIAS_RADIUS, BIAS_RADIUS);
                    }

                    CtlPresenter.DrawEllipse(brush, pen,
                                             new Point(LayerX(model, layer),
                                             VERTICAL_OFFSET + VerticalShift(model, layer) + neuron.Id * VerticalDistance(layer.Height)),
                                             NEURON_RADIUS, NEURON_RADIUS);
                    
                }
            });
        }

        private void Draw(bool fullState, NetworkDataModel model)
        {
            //StartRender();
            CtlPresenter.Clear();

            if (model == null)
            {
                
                return;
            }

            lock (Main.ApplyChangesLocker)
            {
                if (model.Layers.Count > 0)
                {
                    Range.ForEachTrimEnd(model.Layers, -1, layer => DrawLayersLinks(fullState, model, layer, layer.Next));
                }

                Range.ForEach(model.Layers, layer => DrawLayerNeurons(fullState, model, layer));
            }

            //CtlBox.Invalidate();

            //CtlPresenter.InvalidateVisual();

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

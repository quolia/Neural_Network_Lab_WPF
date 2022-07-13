using Qualia.Model;
using Qualia.Tools;
using System;
using System.Drawing;
using System.Windows;

namespace Qualia.Controls
{
    unsafe sealed public partial class RandomizerViewer : WindowResizeControl
    {
        private readonly RandomizeFunction _randomizeMode;
        private readonly double _param;

        private readonly Font _font = new("Tahoma",
                                          6.5f,
                                          System.Drawing.FontStyle.Bold);

        private readonly NetworkDataModel _model = new(Constants.UnknownId, new[] { 100, 100, 100, 100, 100, 100 });

        public RandomizerViewer()
        {
            InitializeComponent();
        }

        public RandomizerViewer(RandomizeFunction randomizeMode, double param)
        {
            InitializeComponent();

            _randomizeMode = randomizeMode ?? throw new ArgumentNullException(nameof(randomizeMode));
            _param = param;

            CtlName.Text = _randomizeMode.GetType().ToString();
            _randomizeMode.Do(_model, _param);

            CtlPresenter.Width = SystemParameters.PrimaryScreenWidth;
            CtlPresenter.Height = SystemParameters.PrimaryScreenHeight;

            RandomizerViewer_OnSizeChanged(null, null);

            SizeChanged += RandomizerViewer_OnSizeChanged;
            Loaded += RandomizerViewer_OnLoaded;

            WindowState = WindowState.Maximized;
        }

        private void RandomizerViewer_OnLoaded(object sender, RoutedEventArgs e)
        {
            Render();
        }

        private void RandomizerViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {   
            CtlPresenter.SetValue(System.Windows.Controls.Canvas.LeftProperty, (CtlCanvas.ActualWidth - CtlPresenter.ActualWidth) / 2);
            CtlPresenter.SetValue(System.Windows.Controls.Canvas.TopProperty, (CtlCanvas.ActualHeight - CtlPresenter.ActualHeight) / 2);
        }

        private int GetModelWeightsMaxValue()
        {
            double max = 0;

            var layer = _model.Layers.First;
            while (layer != _model.Layers.Last)
            {
                var neuron = layer.Neurons.First;
                while (neuron != null)
                {
                    var weight = neuron.Weights.First;
                    while (weight != null)
                    {
                        if (MathX.Abs(weight.Weight) > max)
                        {
                            max = MathX.Abs(weight.Weight);
                        }

                        weight = weight.Next;
                    }

                    neuron = neuron.Next;
                }

                layer = layer.Next;
            }

            return (int)MathX.Ceiling(MathX.Max(1, max));
        }

        private void Render()
        {
            CtlPresenter.CtlPresenter.StartRender();

            const int layersDistance = 200;

            int left = CtlPresenter.CtlPresenter.Width / 2 - (_model.Layers.Count - 1) * (layersDistance - 100 / 2) / 2;
            int top = CtlPresenter.CtlPresenter.Height / 2 - _model.Layers.First.Neurons.Count / 2;

            byte alpha = 100;
            int heightOf1 = (CtlPresenter.CtlPresenter.Height - 250) / 2 / GetModelWeightsMaxValue();

            using Pen zeroColor = new(Color.FromArgb(alpha, Color.Gray)); 

            for (int layer = 0; layer < _model.Layers.Count - 1; ++layer)
            {
                var neuronsCount = _model.Layers[layer].Neurons.Count;
                for (int neuron = 0; neuron < neuronsCount; ++neuron)
                {
                    CtlPresenter.CtlPresenter.G.DrawLine(zeroColor, left - neuron + layer * layersDistance, top + neuron, 100 + left - neuron + layer * layersDistance, top + neuron);

                    for (int weight = 0; weight < _model.Layers[layer].Neurons[neuron].Weights.Count; ++weight)
                    {
                        var value = _model.Layers[layer].Neurons[neuron].Weights[weight].Weight;
                        var hover = value == 0 ? 0 : 30 * MathX.Sign(value);
                        var p = Draw.GetPen(value, 0, alpha);

                        using Pen pen = new(Draw.MediaColorToSystemColor(p.Brush.GetColor()));
                        CtlPresenter.CtlPresenter.G.DrawLine(pen,
                                                             left - neuron + layer * layersDistance + weight,
                                                             top + neuron - hover,
                                                             left - neuron + layer * layersDistance + weight,
                                                             top + neuron - hover - (float)(heightOf1 * value));

                        CtlPresenter.CtlPresenter.G.FillEllipse(Brushes.Orange,
                                                                left - neuron + layer * layersDistance + weight - 1,
                                                                top + neuron - hover - (float)(heightOf1 * value),
                                                                2,
                                                                2);
                    }
                }

                // 1 line
                CtlPresenter.CtlPresenter.G.DrawLine(Pens.Black,
                                                     left - 105,
                                                     top + 100 - 30,
                                                     left - 105,
                                                     top + 100 - 30 - heightOf1);

                // 1 text
                CtlPresenter.CtlPresenter.G.DrawString("1",
                                                       _font,
                                                       Brushes.Black,
                                                       left - 115,
                                                       top + 100 - _font.Height - 30);

                CtlPresenter.CtlPresenter.Invalidate();
            }

            zeroColor.Dispose();
        }
    }
}
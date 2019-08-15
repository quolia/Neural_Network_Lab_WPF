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
using System.Windows.Shapes;
using Tools;

namespace Qualia
{
    public partial class RandomizerViewer : Window
    {
        string Randomizer;
        double? A;
        Image Image;

        NetworkDataModel Model = new NetworkDataModel(Const.UnknownId, new int[] { 100, 100, 100, 100, 100, 100 });
        //NetworkDataModel Model = new NetworkDataModel(Const.UnknownId, new int[] { 20, 20, 20, 20, 20, 20 });

        public RandomizerViewer()
        {
            InitializeComponent();
        }

        private double Scale(double x)
        {
            return x * Tools.Render.PixelSize;
        }

        public RandomizerViewer(string randomizer, double? a)
        {
            InitializeComponent();
            CtlPresenter.Scale = Scale;
            Title = "Randomizer Viewer";// | " + randomizer;

            Randomizer = randomizer;
            A = a;

            
            

            RandomizeMode.Helper.Invoke(Randomizer, Model, A);

            CtlPresenter.Width = SystemParameters.PrimaryScreenWidth;
            CtlPresenter.Height = SystemParameters.PrimaryScreenHeight;
  

            Render();

            SizeChanged += RandomizerViewer_SizeChanged;
        }

        private void RandomizerViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {


            if (Image != null)
            {
                Image.SetValue(Canvas.LeftProperty, (Width - SystemParameters.PrimaryScreenWidth) / 2);
                Image.SetValue(Canvas.TopProperty, (Height - SystemParameters.PrimaryScreenHeight) / 2);
            }
            

            CtlPresenter.SetValue(Canvas.LeftProperty, Tools.Render.PixelSize * (Width - SystemParameters.PrimaryScreenWidth) / 2);
            CtlPresenter.SetValue(Canvas.TopProperty, Tools.Render.PixelSize * (Height - SystemParameters.PrimaryScreenHeight) / 2);
        }

        private void Render()
        {
            double left = 3 + (SystemParameters.PrimaryScreenWidth / 2) - ((Model.Layers.Count - 1) * 125) / 2;
            left /= Tools.Render.PixelSize;

            double top = (SystemParameters.PrimaryScreenHeight / 2) - Model.Layers[0].Height / 2 - 30;
            top /= Tools.Render.PixelSize;

            byte alpha = 100;
            double mult = Height / 12;

            var zeroColor = Draw.GetPen(Draw.GetColor(alpha, Colors.Gray));

            for (int layer = 0; layer < Model.Layers.Count - 1; ++layer)
            {
                var neuronsCount = Model.Layers[layer].Neurons.Count;
                for (int neuron = 0; neuron < neuronsCount; ++neuron)
                {
                    CtlPresenter.DrawLine(zeroColor, new Point(left - neuron + layer * 150, top + neuron), new Point(100 + left - neuron + layer * 150, top + neuron));

                    for (int weight = 0; weight < Model.Layers[layer].Neurons[neuron].Weights.Count; ++weight)
                    {
                        var value = Model.Layers[layer].Neurons[neuron].Weights[weight].Weight;
                        var hover = value == 0 ? 0 : 30 * Math.Sign(value);
                        var pen = Draw.GetPen(value, 0, alpha);
                        
                        CtlPresenter.DrawLine(pen,
                                              new Point(left - neuron + layer * 150 + weight,
                                              top + neuron - hover),
                                              new Point(left - neuron + layer * 150 + weight,
                                              top + neuron - hover - (float)(mult * value)));

                        CtlPresenter.DrawEllipse(Brushes.Orange, Draw.GetPen(Colors.Orange),
                                                 new Point(left - neuron + layer * 150 + weight - 1,
                                                 top + neuron - hover - (float)(mult * value)),
                                                 0.5,
                                                 0.5);
                        
                    }
                }

                CtlPresenter.DrawLine(Draw.GetPen(Colors.Black),
                                      new Point(left - 105,
                                      top + 100 - 30),
                                      new Point(left - 105,
                                      top + 100 - 30 - mult));

                var font = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
                var text = new FormattedText("1", Culture.Current, FlowDirection.LeftToRight, font, 7, Brushes.Black, Tools.Render.PixelsPerDip);
                CtlPresenter.DrawText(text, new Point(left - 115, top + 100 - text.Height - 30));

             
                
            }
            
    
                Image = CtlPresenter.GetImage();
                //CtlPresenter.Clear();


                //CtlCenter.Children.Clear();
                CtlCenter.Children.Add(Image);
                
            
        }
    }
}
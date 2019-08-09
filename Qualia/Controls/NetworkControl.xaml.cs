using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tools;

namespace Qualia.Controls
{
    public partial class NetworkControl : System.Windows.Controls.UserControl
    {
        public readonly long Id;
        public Config Config;
        Action<Notification.ParameterChanged, object> OnNetworkUIChanged;

        public InputLayerControl InputLayer
        {
            get;
            private set;
        }

        OutputLayerControl OutputLayer;

        public NetworkControl(long id, Config config, Action<Notification.ParameterChanged, object> onNetworkUIChanged)
        {
            InitializeComponent();
            OnNetworkUIChanged = onNetworkUIChanged;

            Id = UniqId.GetId(id);
            Config = config.Extend(Id);

            LoadConfig();

            CtlTabsLayers.SelectionChanged += CtlTabsLayers_SelectionChanged;
            CtlRandomizerParamA.Changed += OnChanged;
            CtlRandomizer.SelectedIndexChanged += CtlRandomizer_SelectedValueChanged;
            CtlLearningRate.Changed += OnChanged;
        }

        private void CtlTabsLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //CtlMenuDeleteLayer.Enabled = CtlTabsLayers.SelectedIndex > 0 && CtlTabsLayers.SelectedIndex < CtlTabsLayers.TabCount - 1;
        }

        /*
protected override void OnResize(EventArgs e)
{
   CtlTabsLayers.SuspendLayout();
   base.OnResize(e);
   CtlTabsLayers.ResumeLayout();// .Visible = true;
}
*/
        /*
        public void ResizeBegin()
        {
            CtlTabsLayers.SuspendLayout();
        }

        public void ResizeEnd()
        {
            CtlTabsLayers.ResumeLayout();
        }
        */
        private void OnChanged()
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure, null);
        }

        private void CtlRandomizer_SelectedValueChanged(int index)
        {
            //CtlLearningRateLabel.Focus();
            OnNetworkUIChanged(Notification.ParameterChanged.Structure, null);
        }

        private void CtlTabsLayers_SelectedIndexChanged(object sender, EventArgs e)
        {
           // CtlMenuDeleteLayer.Enabled = CtlTabsLayers.SelectedIndex > 0 && CtlTabsLayers.SelectedIndex < CtlTabsLayers.TabCount - 1;
        }

        public void AddLayer()
        {
            AddLayer(Const.UnknownId);
        }

        private void AddLayer(long id)
        {
            var layer = new HiddenLayerControl(id, Config, OnNetworkUIChanged);
            var sv = new ScrollViewer() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            sv.Content = layer;
            sv.ScrollChanged += layer.OnScrollChanged;

            var tab = new TabItem();
            tab.Content = sv;
            CtlTabsLayers.Items.Insert(CtlTabsLayers.Items.Count - 1, tab);
            CtlTabsLayers.SelectedItem = tab;
            ResetLayersTabsNames();
            if (id == Const.UnknownId)
            {
                OnNetworkUIChanged(Notification.ParameterChanged.Structure, null);
            }
        }

        public void ResetLayersTabsNames()
        {
            var layers = GetLayersControls();
            for (int i = 0; i < layers.Count; ++i)
            {
                if (layers[i].IsInput)
                {
                    CtlTabsLayers.Tab(i).Header = $"Input ({layers[i].NeuronsCount})";
                }
                else if (layers[i].IsOutput)
                {
                    CtlTabsLayers.Tab(i).Header = $"Output ({layers[i].NeuronsCount})";
                }
                else
                {
                    CtlTabsLayers.Tab(i).Header = $"L{i} ({layers[i].NeuronsCount})";
                }
            }

            //Refresh();
        }

        private void CtlMenuAddLayer_Click(object sender, EventArgs e)
        {
            AddLayer();
        }

        public bool IsValid()
        {
            return CtlRandomizerParamA.IsValid() &&
                   CtlLearningRate.IsValid() &&
                   GetLayersControls().All(c => c.IsValid());
        }

        public void SaveConfig()
        {
            Config.Set(Const.Param.SelectedLayerIndex, CtlTabsLayers.SelectedIndex);
            Config.Set(Const.Param.RandomizeMode, Randomizer);
            Config.Set(Const.Param.Color, $"{CtlColor.Foreground.GetColor().A},{CtlColor.Foreground.GetColor().R},{CtlColor.Foreground.GetColor().G},{CtlColor.Foreground.GetColor().B}");

            CtlRandomizerParamA.Save(Config);
            CtlLearningRate.Save(Config);

            var layers = GetLayersControls();
            Range.ForEach(layers, l => l.SaveConfig());
            Config.Set(Const.Param.Layers, layers.Select(l => l.Id));

            //

            ResetLayersTabsNames();
        }

        public void VanishConfig()
        {
            Config.Remove(Const.Param.SelectedLayerIndex);
            Config.Remove(Const.Param.RandomizeMode);
            Config.Remove(Const.Param.Color);

            CtlRandomizerParamA.Vanish(Config);
            CtlLearningRate.Vanish(Config);

            var layers = GetLayersControls();
            Range.ForEach(layers, l => l.VanishConfig());
            Config.Remove(Const.Param.Layers);
        }

        public List<LayerBase> GetLayersControls()
        {
            var result = new List<LayerBase>();
            for (int i = 0; i < CtlTabsLayers.Items.Count; ++i)
            {
                //if (CtlTabsLayers.Tab(i).Content is LayerBase layer)
                {
                    result.Add(CtlTabsLayers.Tab(i).FindVisualChildren<LayerBase>().First());
                }
            }
            return result;
        }

        private void LoadConfig()
        {
            RandomizeMode.Helper.FillComboBox(CtlRandomizer, Config, Const.Param.RandomizeMode, nameof(RandomizeMode.Random));
            CtlRandomizerParamA.Load(Config);
            CtlLearningRate.Load(Config);
            var color = Config.GetArray(Const.Param.Color, "255,100,100,100");
            CtlColor.Foreground = Tools.Draw.GetBrush(Color.FromArgb((byte)color[0], (byte)color[1], (byte)color[2], (byte)color[3]));

            //

            //CtlTabsLayers.SuspendLayout();
            var layers = Config.GetArray(Const.Param.Layers);

            var inputLayerId = layers.Length > 0 ? layers[0] : Const.UnknownId;
            var outputLayerId = layers.Length > 0 ? layers[layers.Length - 1] : Const.UnknownId;

            InputLayer = new InputLayerControl(inputLayerId, Config, OnNetworkUIChanged);
            var sv = new ScrollViewer() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            sv.Content = InputLayer;
            CtlTabInput.Content = sv;
            sv.ScrollChanged += InputLayer.OnScrollChanged;

            OutputLayer = new OutputLayerControl(outputLayerId, Config, OnNetworkUIChanged);
            sv = new ScrollViewer() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            sv.Content = OutputLayer;
            CtlTabOutput.Content = sv;
            sv.ScrollChanged += OutputLayer.OnScrollChanged;

            

            Range.ForEach(layers, l =>
            {
                if (l != layers.First() && l != layers.Last())
                    AddLayer(l);
            });

            CtlTabsLayers.SelectedIndex = Config.GetInt(Const.Param.SelectedLayerIndex, 0).Value;
            //CtlTabsLayers.ResumeLayout();
        }



        public int[] GetLayersSize()
        {
            return GetLayersControls().Select(l => l.NeuronsCount).ToArray();
        }

        public int InputNeuronsCount => InputLayer.GetNeuronsControls().Where(c => !c.IsBias).Count();

        private string Randomizer => CtlRandomizer.SelectedItem.ToString();
        private double? RandomizerParamA => CtlRandomizerParamA.ValueOrNull;
        private double LearningRate => CtlLearningRate.Value;

        public LayerBase SelectedLayer => CtlTabsLayers.SelectedTab().FindVisualChildren<LayerBase>().First();
        public Type SelectedLayerType => CtlTabsLayers.SelectedTab().Content.GetType();
        public bool IsSelectedLayerHidden => SelectedLayerType == typeof(HiddenLayerControl);

        private void CtlMenuDeleteLayer_Click(object sender, EventArgs e)
        {
            DeleteLayer();
        }

        public void DeleteLayer()
        {
            if (System.Windows.MessageBox.Show($"Would you really like to delete layer L{CtlTabsLayers.SelectedIndex + 1}?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                var layer = CtlTabsLayers.SelectedTab().Content as HiddenLayerControl;
                layer.VanishConfig();

                CtlTabsLayers.Items.Remove(CtlTabsLayers.SelectedTab());
                ResetLayersTabsNames();
                OnNetworkUIChanged(Notification.ParameterChanged.Structure, null);
            }
        }

        public NetworkDataModel CreateNetworkDataModel()
        {
            var model = new NetworkDataModel(Id, GetLayersSize())
            {
                Color = CtlColor.Foreground.GetColor(),
                //Statistic = new Statistic(true),
                //DynamicStatistic = new DynamicStatistic(),
                //ErrorMatrix = new ErrorMatrix(),
                RandomizeMode = Randomizer,
                RandomizerParamA = RandomizerParamA,
                LearningRate = LearningRate,
                InputInitial0 = ActivationFunction.Helper.GetInstance(InputLayer.ActivationFunc).Do(InputLayer.Initial0, null),
                InputInitial1 = ActivationFunction.Helper.GetInstance(InputLayer.ActivationFunc).Do(InputLayer.Initial1, null)
            };

            model.Activate();

            var layers = GetLayersControls();
            for (int ln = 0; ln < layers.Count; ++ln)
            {
                model.Layers[ln].VisualId = layers[ln].Id;

                var neurons = layers[ln].GetNeuronsControls();
                for (int nn = 0; nn < neurons.Count; ++nn)
                {
                    var neuronModel = model.Layers[ln].Neurons[nn];
                    neuronModel.VisualId = neurons[nn].Id;
                    neuronModel.IsBias = neurons[nn].IsBias;
                    neuronModel.IsBiasConnected = neurons[nn].IsBiasConnected;

                    neuronModel.ActivationFunction = ActivationFunction.Helper.GetInstance(neurons[nn].ActivationFunc);
                    neuronModel.ActivationDerivative = ActivationDerivative.Helper.GetInstance(neurons[nn].ActivationFunc);
                    neuronModel.ActivationFuncParamA = neurons[nn].ActivationFuncParamA;

                    neuronModel.WeightsInitializer = neurons[nn].WeightsInitializer;
                    neuronModel.WeightsInitializerParamA = neurons[nn].WeightsInitializerParamA;
                    double initValue = InitializeMode.Helper.Invoke(neurons[nn].WeightsInitializer, neurons[nn].WeightsInitializerParamA);
                    if (!InitializeMode.Helper.IsSkipValue(initValue))
                    {
                        foreach (var weight in neuronModel.Weights)
                        {
                            weight.Weight = InitializeMode.Helper.Invoke(neurons[nn].WeightsInitializer, neurons[nn].WeightsInitializerParamA);
                        }
                    }

                    if (neuronModel.IsBias)
                    {
                        neuronModel.ActivationInitializer = neurons[nn].ActivationInitializer;
                        neuronModel.ActivationInitializerParamA = neurons[nn].ActivationInitializerParamA;
                        initValue = InitializeMode.Helper.Invoke(neurons[nn].ActivationInitializer, neurons[nn].ActivationInitializerParamA);
                        if (!InitializeMode.Helper.IsSkipValue(initValue))
                        {
                            neuronModel.Activation = initValue;
                        }
                    }
                }
            }

            model.Layers.Last().VisualId = Const.OutputLayerId;
            {
                var neurons = OutputLayer.GetNeuronsControls();
                for (int i = 0; i < neurons.Count; ++i)
                {
                    model.Layers.Last().Neurons[i].VisualId = neurons[i].Id;
                }
            }

            return model;
        }

        private void CtlRandomViewerButton_Click(object sender, EventArgs e)
        {
// RandomViewer(Randomizer, RandomizerParamA);
          //  viewer.Show();
        }

        private void CtlColor_Click(object sender, MouseButtonEventArgs e)
        {
            using (var colorDialog = new ColorDialog())
            {
                var wpfColor = CtlColor.Foreground.GetColor();
                colorDialog.Color = System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B); ;
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    CtlColor.Foreground = Draw.GetBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                    OnNetworkUIChanged(Notification.ParameterChanged.Structure, null);
                }
            }
        }
        /*
private void CtlContextMenu_Opening(object sender, CancelEventArgs e)
{
   CtlMenuDeleteLayer.Enabled = IsSelectedLayerHidden;
}
*/
    }
}

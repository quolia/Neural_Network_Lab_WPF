using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public partial class NetworkControl : System.Windows.Controls.UserControl
    {
        public readonly long Id;
        public Config Config;
        Action<Notification.ParameterChanged> OnNetworkUIChanged;

        List<IConfigValue> ConfigParams;

        public InputLayerControl InputLayer
        {
            get;
            private set;
        }

        OutputLayerControl OutputLayer;
        public bool IsNetworkEnabled => CtlIsNetworkEnabled.IsOn;

        public NetworkControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
        {
            InitializeComponent();
            OnNetworkUIChanged = onNetworkUIChanged;

            Id = UniqId.GetId(id);
            Config = config.Extend(Id);

            ConfigParams = new List<IConfigValue>()
            {
                CtlRandomizeModeParamA,
                CtlRandomizeMode,
                CtlLearningRate,
                CtlIsNetworkEnabled,
                CtlCostFunction
            };

            ConfigParams.ForEach(p => p.SetConfig(Config));
            LoadConfig();
            ConfigParams.ForEach(p => p.SetChangeEvent(OnChanged));
        }

        private void OnChanged()
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
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
                OnNetworkUIChanged(Notification.ParameterChanged.Structure);
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
        }

        public bool IsValid()
        {
            return ConfigParams.All(p => p.IsValid()) && GetLayersControls().All(c => c.IsValid());
        }

        public void SaveConfig()
        {
            Config.Set(Const.Param.SelectedLayerIndex, CtlTabsLayers.SelectedIndex);
            Config.Set(Const.Param.Color, $"{CtlColor.Foreground.GetColor().A},{CtlColor.Foreground.GetColor().R},{CtlColor.Foreground.GetColor().G},{CtlColor.Foreground.GetColor().B}");
            ConfigParams.ForEach(p => p.SaveConfig());

            var layers = GetLayersControls();
            layers.ForEach(l => l.SaveConfig());
            Config.Set(Const.Param.Layers, layers.Select(l => l.Id));

            //

            ResetLayersTabsNames();
        }

        public void OnTaskChanged(INetworkTask task)
        {
            InputLayer.OnTaskChanged(task);
            OutputLayer.OnTaskChanged(task);
        }

        public void VanishConfig()
        {
            Config.Remove(Const.Param.SelectedLayerIndex);
            Config.Remove(Const.Param.Color);

            ConfigParams.ForEach(p => p.VanishConfig());

            GetLayersControls().ForEach(l => l.VanishConfig());
            Config.Remove(Const.Param.Layers);
        }

        public ListX<LayerBase> GetLayersControls()
        {
            var result = new ListX<LayerBase>(CtlTabsLayers.Items.Count);
            for (int i = 0; i < CtlTabsLayers.Items.Count; ++i)
            {
                result.Add(CtlTabsLayers.Tab(i).FindVisualChildren<LayerBase>().First());
            }
            return result;
        }

        private void LoadConfig()
        {
            Tools.RandomizeMode.Helper.FillComboBox(CtlRandomizeMode, Config, nameof(Tools.RandomizeMode.FlatRandom));
            Tools.CostFunction.Helper.FillComboBox(CtlCostFunction, Config, nameof(Tools.CostFunction.MSE));

            ConfigParams.ForEach(p => p.LoadConfig());

            var color = Config.GetArray(Const.Param.Color, "255,100,100,100");
            CtlColor.Foreground = Tools.Draw.GetBrush(Color.FromArgb((byte)color[0], (byte)color[1], (byte)color[2], (byte)color[3]));

            //

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

            foreach (var layer in layers)
            {
                if (layer != layers[0] && layer != layers.Last())
                    AddLayer(layer);
            }

            CtlTabsLayers.SelectedIndex = (int)Config.GetInt(Const.Param.SelectedLayerIndex, 0).Value;
        }

        public int[] GetLayersSize()
        {
            return GetLayersControls().Select(l => l.NeuronsCount).ToArray();
        }

        public int InputNeuronsCount => InputLayer.GetNeuronsControls().Count(c => !c.IsBias);
        private string RandomizeMode => CtlRandomizeMode.SelectedItem.ToString();
        private double? RandomizerParamA => CtlRandomizeModeParamA.ValueOrNull;
        private double LearningRate => CtlLearningRate.Value;
        public LayerBase SelectedLayer => CtlTabsLayers.SelectedTab().FindVisualChildren<LayerBase>().First();
        public Type SelectedLayerType => CtlTabsLayers.SelectedTab().FindVisualChildren<LayerBase>().First().GetType();
        public bool IsSelectedLayerHidden => SelectedLayerType == typeof(HiddenLayerControl);

        public void DeleteLayer()
        {
            if (System.Windows.MessageBox.Show($"Would you really like to delete layer L{CtlTabsLayers.SelectedIndex + 1}?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                SelectedLayer.VanishConfig();
                CtlTabsLayers.Items.Remove(CtlTabsLayers.SelectedTab());
                ResetLayersTabsNames();
                OnNetworkUIChanged(Notification.ParameterChanged.Structure);
            }
        }

        public NetworkDataModel CreateNetworkDataModel(INetworkTask task, bool isCopy)
        {
            ErrorMatrix em1 = null;
            if (task != null)
            {
                em1 = new ErrorMatrix(task.GetClasses());
                var em2 = new ErrorMatrix(task.GetClasses());
                em1.Next = em2;
                em2.Next = em1;
            }

            var model = new NetworkDataModel(Id, GetLayersSize())
            {
                ErrorMatrix = em1,
                Classes = task?.GetClasses(),
                IsEnabled = CtlIsNetworkEnabled.IsOn,
                Color = CtlColor.Foreground.GetColor(),
                RandomizeMode = RandomizeMode,
                RandomizerParamA = RandomizerParamA,
                LearningRate = LearningRate,
                InputInitial0 = ActivationFunction.Helper.GetInstance(InputLayer.ActivationFunc).Do(InputLayer.Initial0, InputLayer.ActivationFuncParamA),
                InputInitial1 = ActivationFunction.Helper.GetInstance(InputLayer.ActivationFunc).Do(InputLayer.Initial1, InputLayer.ActivationFuncParamA),
                CostFunction = CostFunction.Helper.GetInstance(CtlCostFunction.SelectedValue.ToString()),
                IsAdjustFirstLayerWeights = InputLayer.IsAdjustFirstLayerWeights
            };

            model.ActivateNetwork();

            LayerDataModel prevLayer = null;

            var layers = GetLayersControls();
            for (int ln = 0; ln < layers.Count; ++ln)
            {
                if (ln > 0)
                {
                    prevLayer = model.Layers[ln - 1];
                }

                model.Layers[ln].VisualId = layers[ln].Id;

                var neurons = layers[ln].GetNeuronsControls();

                for (int nn = 0; nn < neurons.Count; ++nn)
                {
                    var neuronModel = model.Layers[ln].Neurons[nn];
                    neuronModel.VisualId = neurons[nn].Id;
                    neuronModel.IsBias = neurons[nn].IsBias;
                    neuronModel.IsBiasConnected = neurons[nn].IsBiasConnected;

                    neuronModel.ActivationFunction = ActivationFunction.Helper.GetInstance(neurons[nn].ActivationFunc);
                    neuronModel.ActivationFuncParamA = neurons[nn].ActivationFuncParamA;


                    if (ln == 0 && !neuronModel.IsBias)
                    {
                        neuronModel.WeightsInitializer = InputLayer.WeightsInitializer;
                        neuronModel.WeightsInitializerParamA = InputLayer.WeightsInitializerParamA;
                        double initValue = InitializeMode.Helper.Invoke(neuronModel.WeightsInitializer, neuronModel.WeightsInitializerParamA);
                        if (!InitializeMode.Helper.IsSkipValue(initValue))
                        {
                            neuronModel.Weights.ForEach(w => w.Weight = InitializeMode.Helper.Invoke(neuronModel.WeightsInitializer, neuronModel.WeightsInitializerParamA));
                        }
                    }
                    else
                    {
                        neuronModel.WeightsInitializer = neurons[nn].WeightsInitializer;
                        neuronModel.WeightsInitializerParamA = neurons[nn].WeightsInitializerParamA;
                        double initValue = InitializeMode.Helper.Invoke(neuronModel.WeightsInitializer, neuronModel.WeightsInitializerParamA);
                        if (!InitializeMode.Helper.IsSkipValue(initValue))
                        {
                            neuronModel.Weights.ForEach(w => w.Weight = InitializeMode.Helper.Invoke(neuronModel.WeightsInitializer, neuronModel.WeightsInitializerParamA));
                        }
                    }

                    if (neuronModel.IsBias)
                    {
                        neuronModel.ActivationInitializer = neurons[nn].ActivationInitializer;
                        neuronModel.ActivationInitializerParamA = neurons[nn].ActivationInitializerParamA;
                        double initValue = InitializeMode.Helper.Invoke(neurons[nn].ActivationInitializer, neurons[nn].ActivationInitializerParamA);
                        if (!InitializeMode.Helper.IsSkipValue(initValue))
                        {
                            neuronModel.Activation = initValue;
                        }
                    }
                    
                    if (!isCopy && prevLayer != null && prevLayer.Height > 0)
                    {
                        neuronModel.ForwardHelper = new ListX<ForwardNeuron>(prevLayer.Height);

                        var prevNeuron = prevLayer.Neurons[0];
                        while (prevNeuron != null)
                        {
                            if (!neuronModel.IsBias || (neuronModel.IsBiasConnected && prevNeuron.IsBias))
                            {
                                neuronModel.ForwardHelper.Add(new ForwardNeuron(prevNeuron, prevNeuron.WeightTo(neuronModel)));
                            }

                            prevNeuron = prevNeuron.Next;
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

            if (!isCopy)
            {
                model.Copy = CreateNetworkDataModel(task, true);
            }

            return model;
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
                    OnNetworkUIChanged(Notification.ParameterChanged.Structure);
                }
            }
        }

        private void CtlLayerContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            CtlMenuDeleteLayer.IsEnabled = IsSelectedLayerHidden;
        }

        private void CtlMenuAddLayer_Click(object sender, RoutedEventArgs e)
        {
            AddLayer();
        }

        private void CtlMenuDeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            DeleteLayer();
        }

        private void CtlRandomizerButton_Click(object sender, RoutedEventArgs e)
        {
            var viewer = new RandomizerViewer(RandomizeMode, RandomizerParamA);
            viewer.Show();
        }
    }
}

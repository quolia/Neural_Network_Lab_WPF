using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using Tools;

namespace Qualia.Controls
{
    public partial class NetworkControl : System.Windows.Controls.UserControl
    {
        public readonly long Id;
        public Config Config;
        
        private readonly Action<Notification.ParameterChanged> OnNetworkUIChanged;
        private readonly List<IConfigParam> _configParams;
        private OutputLayerControl _outputLayer;

        public InputLayerControl InputLayer { get; private set; }

        public bool IsNetworkEnabled => CtlIsNetworkEnabled.IsOn;

        public NetworkControl(long existingId, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
        {
            InitializeComponent();
            OnNetworkUIChanged = onNetworkUIChanged;

            Id = UniqId.GetNextId(existingId);
            Config = config.Extend(Id);

            _configParams = new List<IConfigParam>()
            {
                CtlRandomizeModeParamA,
                CtlRandomizeMode,
                CtlLearningRate,
                CtlIsNetworkEnabled,
                CtlCostFunction
            };

            _configParams.ForEach(param => param.SetConfig(Config));
            LoadConfig();

            _configParams.ForEach(param => param.SetChangeEvent(OnChanged));
        }

        private void OnChanged()
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        public void AddLayer()
        {
            AddLayer(Const.UnknownId);
        }

        private void AddLayer(long layerId)
        {
            var ctlLayer = new HiddenLayerControl(layerId, Config, OnNetworkUIChanged);
            var ctlScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = ctlLayer
            };
            ctlScroll.ScrollChanged += ctlLayer.OnScrollChanged;

            var tabItem = new TabItem
            {
                Content = ctlScroll
            };

            CtlTabsLayers.Items.Insert(CtlTabsLayers.Items.Count - 1, tabItem);
            CtlTabsLayers.SelectedItem = tabItem;
            ResetLayersTabsNames();

            if (layerId == Const.UnknownId)
            {
                OnNetworkUIChanged(Notification.ParameterChanged.Structure);
            }
        }

        public void ResetLayersTabsNames()
        {
            var ctlLayers = GetLayersControls();
            for (int ind = 0; ind < ctlLayers.Count; ++ind)
            {
                if (ctlLayers[ind].IsInput)
                {
                    CtlTabsLayers.Tab(ind).Header = $"Input ({ctlLayers[ind].NeuronsCount})";
                }
                else if (ctlLayers[ind].IsOutput)
                {
                    CtlTabsLayers.Tab(ind).Header = $"Output ({ctlLayers[ind].NeuronsCount})";
                }
                else
                {
                    CtlTabsLayers.Tab(ind).Header = $"L{ind} ({ctlLayers[ind].NeuronsCount})";
                }
            }

            // The code below is needed to refresh Tabcontrol.
            // Without it newly added neuron control is not visible for hit test (some WPF issue).

            int selectedIndex = CtlTabsLayers.SelectedIndex;
            CtlTabsLayers.SelectedIndex = 0;
            CtlTabsLayers.SelectedIndex = selectedIndex;
        }

        public bool IsValid()
        {
            return _configParams.All(param => param.IsValid()) && GetLayersControls().All(control => control.IsValid());
        }

        public void SaveConfig()
        {
            Config.Set(Const.Param.SelectedLayerIndex, CtlTabsLayers.SelectedIndex);
            Config.Set(Const.Param.Color, $"{CtlColor.Foreground.GetColor().A},{CtlColor.Foreground.GetColor().R},{CtlColor.Foreground.GetColor().G},{CtlColor.Foreground.GetColor().B}");
            _configParams.ForEach(param => param.SaveConfig());

            var ctlLayers = GetLayersControls();
            ctlLayers.ForEach(ctlLayer => ctlLayer.SaveConfig());
            Config.Set(Const.Param.Layers, ctlLayers.Select(ctlLayer => ctlLayer.Id));

            //

            ResetLayersTabsNames();
        }

        public void OnTaskChanged(INetworkTask networkTask)
        {
            InputLayer.OnTaskChanged(networkTask);
            _outputLayer.OnTaskChanged(networkTask);
        }

        public void VanishConfig()
        {
            Config.Remove(Const.Param.SelectedLayerIndex);
            Config.Remove(Const.Param.Color);

            _configParams.ForEach(param => param.VanishConfig());

            GetLayersControls().ForEach(ctlLayer => ctlLayer.VanishConfig());
            Config.Remove(Const.Param.Layers);
        }

        public ListX<LayerBase> GetLayersControls()
        {
            var ctlLayers = new ListX<LayerBase>(CtlTabsLayers.Items.Count);
            for (int ind = 0; ind < CtlTabsLayers.Items.Count; ++ind)
            {
                ctlLayers.Add(CtlTabsLayers.Tab(ind).FindVisualChildren<LayerBase>().First());
            }

            return ctlLayers;
        }

        private void LoadConfig()
        {
            Tools.RandomizeMode.Helper.FillComboBox(CtlRandomizeMode, Config, nameof(Tools.RandomizeMode.FlatRandom));
            Tools.CostFunction.Helper.FillComboBox(CtlCostFunction, Config, nameof(Tools.CostFunction.MSE));

            _configParams.ForEach(param => param.LoadConfig());

            var color = Config.GetArray(Const.Param.Color, "255,100,100,100");
            CtlColor.Foreground = Tools.Draw.GetBrush(Color.FromArgb((byte)color[0], (byte)color[1], (byte)color[2], (byte)color[3]));

            //

            var layerIds = Config.GetArray(Const.Param.Layers);
            var inputLayerId = layerIds.Length > 0 ? layerIds[0] : Const.UnknownId;
            var outputLayerId = layerIds.Length > 0 ? layerIds[layerIds.Length - 1] : Const.UnknownId;

            InputLayer = new InputLayerControl(inputLayerId, Config, OnNetworkUIChanged);
            var ctlScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = InputLayer
            };

            CtlTabInput.Content = ctlScroll;
            ctlScroll.ScrollChanged += InputLayer.OnScrollChanged;

            _outputLayer = new OutputLayerControl(outputLayerId, Config, OnNetworkUIChanged);
            ctlScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _outputLayer
            };

            CtlTabOutput.Content = ctlScroll;
            ctlScroll.ScrollChanged += _outputLayer.OnScrollChanged;

            var lastLayerId = layerIds.Last();

            foreach (var layerId in layerIds)
            {
                if (layerId != layerIds[0] && layerId != lastLayerId)
                {
                    AddLayer(layerId);
                }
            }

            CtlTabsLayers.SelectedIndex = (int)Config.GetInt(Const.Param.SelectedLayerIndex, 0).Value;
        }

        public int[] GetLayersSizes()
        {
            return GetLayersControls().Select(ctlLayer => ctlLayer.NeuronsCount).ToArray();
        }

        public int InputNeuronsCount => InputLayer.GetNeuronsControls().Count(ctlNeuron => !ctlNeuron.IsBias);

        public LayerBase SelectedLayer => CtlTabsLayers.SelectedTab().FindVisualChildren<LayerBase>().First();

        public Type SelectedLayerType => CtlTabsLayers.SelectedTab().FindVisualChildren<LayerBase>().First().GetType();

        public bool IsSelectedLayerHidden => SelectedLayerType == typeof(HiddenLayerControl);

        private string RandomizeMode => CtlRandomizeMode.SelectedItem.ToString();
        private double? RandomizerParamA => CtlRandomizeModeParamA.ValueOrNull;
        private double LearningRate => CtlLearningRate.Value;

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

        public NetworkDataModel CreateNetworkDataModel(INetworkTask networkTask, bool isCopy)
        {
            ErrorMatrix matrix = null;
            if (networkTask != null)
            {
                matrix = new ErrorMatrix(networkTask.GetClasses());
                var nextMatrix = new ErrorMatrix(networkTask.GetClasses());
                matrix.Next = nextMatrix;
                nextMatrix.Next = matrix;
            }

            var networkModel = new NetworkDataModel(Id, GetLayersSizes())
            {
                ErrorMatrix = matrix,
                Classes = networkTask?.GetClasses(),
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

            networkModel.ActivateNetwork();

            LayerDataModel prevLayerModel = null;

            var ctlLayers = GetLayersControls();
            for (int layerInd = 0; layerInd < ctlLayers.Count; ++layerInd)
            {
                if (layerInd > 0)
                {
                    prevLayerModel = networkModel.Layers[layerInd - 1];
                }

                networkModel.Layers[layerInd].VisualId = ctlLayers[layerInd].Id;

                var ctlNeurons = ctlLayers[layerInd].GetNeuronsControls();

                for (int neuronInd = 0; neuronInd < ctlNeurons.Count; ++neuronInd)
                {
                    var neuronModel = networkModel.Layers[layerInd].Neurons[neuronInd];
                    neuronModel.VisualId = ctlNeurons[neuronInd].Id;
                    neuronModel.IsBias = ctlNeurons[neuronInd].IsBias;
                    neuronModel.IsBiasConnected = ctlNeurons[neuronInd].IsBiasConnected;

                    neuronModel.ActivationFunction = ActivationFunction.Helper.GetInstance(ctlNeurons[neuronInd].ActivationFunc);
                    neuronModel.ActivationFuncParamA = ctlNeurons[neuronInd].ActivationFuncParamA;


                    if (layerInd == 0 && !neuronModel.IsBias)
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
                        neuronModel.WeightsInitializer = ctlNeurons[neuronInd].WeightsInitializer;
                        neuronModel.WeightsInitializerParamA = ctlNeurons[neuronInd].WeightsInitializerParamA;

                        double initValue = InitializeMode.Helper.Invoke(neuronModel.WeightsInitializer, neuronModel.WeightsInitializerParamA);
                        if (!InitializeMode.Helper.IsSkipValue(initValue))
                        {
                            neuronModel.Weights.ForEach(w => w.Weight = InitializeMode.Helper.Invoke(neuronModel.WeightsInitializer, neuronModel.WeightsInitializerParamA));
                        }
                    }

                    if (neuronModel.IsBias)
                    {
                        neuronModel.ActivationInitializer = ctlNeurons[neuronInd].ActivationInitializer;
                        neuronModel.ActivationInitializerParamA = ctlNeurons[neuronInd].ActivationInitializerParamA;
                        double initValue = InitializeMode.Helper.Invoke(ctlNeurons[neuronInd].ActivationInitializer, ctlNeurons[neuronInd].ActivationInitializerParamA);

                        if (!InitializeMode.Helper.IsSkipValue(initValue))
                        {
                            neuronModel.Activation = initValue;
                        }
                    }
                    
                    if (!isCopy && prevLayerModel != null && prevLayerModel.Height > 0)
                    {
                        neuronModel.ForwardHelper = new ListX<ForwardNeuron>(prevLayerModel.Height);

                        var prevNeuronModel = prevLayerModel.Neurons[0];
                        while (prevNeuronModel != null)
                        {
                            if (!neuronModel.IsBias || (neuronModel.IsBiasConnected && prevNeuronModel.IsBias))
                            {
                                neuronModel.ForwardHelper.Add(new ForwardNeuron(prevNeuronModel, prevNeuronModel.WeightTo(neuronModel)));
                            }

                            prevNeuronModel = prevNeuronModel.Next;
                        }
                    }
                }
            }

            networkModel.Layers.Last().VisualId = Const.OutputLayerId;
            {
                var ctlNeurons = _outputLayer.GetNeuronsControls();
                for (int ind = 0; ind < ctlNeurons.Count; ++ind)
                {
                    networkModel.Layers.Last().Neurons[ind].VisualId = ctlNeurons[ind].Id;
                }
            }

            if (!isCopy)
            {
                networkModel.Copy = CreateNetworkDataModel(networkTask, true);
            }

            return networkModel;
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

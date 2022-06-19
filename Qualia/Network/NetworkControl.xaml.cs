using Qualia.Model;
using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public partial class NetworkControl : System.Windows.Controls.UserControl
    {
        public readonly long Id;
        public Config Config;
        
        private readonly Action<Notification.ParameterChanged> NetworkUI_OnChanged;
        private readonly List<IConfigParam> _configParams;
        private OutputLayerControl _outputLayer;

        public InputLayerControl InputLayer { get; private set; }

        public bool IsNetworkEnabled => CtlIsNetworkEnabled.IsOn;

        public NetworkControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
        {
            InitializeComponent();
            NetworkUI_OnChanged = onNetworkUIChanged;

            Id = UniqId.GetNextId(id);
            Config = config.Extend(Id);

            _configParams = new()
            {
                CtlRandomizeFunction.Initialize(nameof(RandomizeFunction.Centered)),
                CtlRandomizeFunctionParam.Initialize(defaultValue: 1),
                CtlLearningRate.Initialize(defaultValue: 0.03),
                CtlIsNetworkEnabled.Initialize(true),
                CtlCostFunction.Initialize(nameof(CostFunction.MeanSquaredError)),
                CtlBackPropagationStrategy.Initialize(nameof(BackPropagationStrategy.Always))
            };

            _configParams.ForEach(param => param.SetConfig(Config));
            LoadConfig();

            _configParams.ForEach(param => param.SetChangeEvent(OnChanged));
        }

        private void OnChanged()
        {
            NetworkUI_OnChanged(Notification.ParameterChanged.Structure);

            //var description = BackPropagationStrategy.GetDescription(CtlBackPropagationStrategy.SelectedItem);
            //CtlBackPropagationStrategyDescription.Text = description;
        }

        public void AddLayer()
        {
            AddLayer(Constants.UnknownId);
        }

        private void AddLayer(long layerId)
        {
            HiddenLayerControl ctlLayer = new(layerId, Config, NetworkUI_OnChanged);

            ScrollViewer ctlScroll = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = ctlLayer
            };
            ctlScroll.ScrollChanged += ctlLayer.Scroll_OnChanged;

            TabItem tabItem = new()
            {
                Content = ctlScroll
            };

            CtlTabsLayers.Items.Insert(CtlTabsLayers.Items.Count - 1, tabItem);
            CtlTabsLayers.SelectedItem = tabItem;
            ResetLayersTabsNames();

            if (layerId == Constants.UnknownId)
            {
                NetworkUI_OnChanged(Notification.ParameterChanged.Structure);
            }
        }

        public void ResetLayersTabsNames()
        {
            var ctlLayers = GetLayersControls();
            for (int i = 0; i < ctlLayers.Count; ++i)
            {
                if (ctlLayers[i].IsInput)
                {
                    CtlTabsLayers.Tab(i).Header = $"Input ({ctlLayers[i].NeuronsCount})";
                }
                else if (ctlLayers[i].IsOutput)
                {
                    CtlTabsLayers.Tab(i).Header = $"Output ({ctlLayers[i].NeuronsCount})";
                }
                else
                {
                    CtlTabsLayers.Tab(i).Header = $"L{i} ({ctlLayers[i].NeuronsCount})";
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
            Config.Set(Constants.Param.SelectedLayerIndex,
                       CtlTabsLayers.SelectedIndex);

            Config.Set(Constants.Param.Color,
                       $"{CtlColor.Foreground.GetColor().A}," +
                       $"{CtlColor.Foreground.GetColor().R}," +
                       $"{CtlColor.Foreground.GetColor().G}," +
                       $"{CtlColor.Foreground.GetColor().B}");

            _configParams.ForEach(param => param.SaveConfig());

            var ctlLayers = GetLayersControls();
            ctlLayers.ForEach(ctlLayer => ctlLayer.SaveConfig());
            Config.Set(Constants.Param.Layers, ctlLayers.Select(ctlLayer => ctlLayer.Id));

            //

            ResetLayersTabsNames();
        }

        public void NetworkTask_OnChanged(TaskFunction taskFunction)
        {
            InputLayer.NetworkTask_OnChanged(taskFunction);
            _outputLayer.NetworkTask_OnChanged(taskFunction);
        }

        public void RemoveFromConfig()
        {
            Config.Remove(Constants.Param.SelectedLayerIndex);
            Config.Remove(Constants.Param.Color);

            _configParams.ForEach(param => param.RemoveFromConfig());

            GetLayersControls().ForEach(ctlLayer => ctlLayer.RemoveFromConfig());
            Config.Remove(Constants.Param.Layers);
        }

        private List<LayerBaseControl> GetLayersControls()
        {
            List<LayerBaseControl> ctlLayers = new(CtlTabsLayers.Items.Count);
            for (int i = 0; i < CtlTabsLayers.Items.Count; ++i)
            {
                ctlLayers.Add(CtlTabsLayers.Tab(i).FindVisualChildren<LayerBaseControl>().First());
            }

            return ctlLayers;
        }

        private void LoadConfig()
        {
            CtlRandomizeFunction.Fill<RandomizeFunction>(Config);
            CtlCostFunction.Fill<CostFunction>(Config);
            CtlBackPropagationStrategy.Fill<BackPropagationStrategy>(Config);
            var description = BackPropagationStrategy.GetDescription(CtlBackPropagationStrategy);
            CtlBackPropagationStrategyDescription.Text = description;

            _configParams.ForEach(param => param.LoadConfig());

            var color = Config.Get(Constants.Param.Color, new long[] { 255,100,100,100 });
            CtlColor.Foreground = Draw.GetBrush(Color.FromArgb((byte)color[0],
                                                               (byte)color[1],
                                                               (byte)color[2],
                                                               (byte)color[3]));

            //

            var layerIds = Config.Get(Constants.Param.Layers, Array.Empty<long>());
            var inputLayerId = layerIds.Length > 0 ? layerIds[0] : Constants.UnknownId;
            var outputLayerId = layerIds.Length > 0 ? layerIds[layerIds.Length - 1] : Constants.UnknownId;

            InputLayer = new(inputLayerId, Config, NetworkUI_OnChanged);
            ScrollViewer ctlScroll = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = InputLayer
            };

            CtlTabInput.Content = ctlScroll;
            ctlScroll.ScrollChanged += InputLayer.Scroll_OnChanged;

            _outputLayer = new(outputLayerId, Config, NetworkUI_OnChanged);
            ctlScroll = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _outputLayer
            };

            CtlTabOutput.Content = ctlScroll;
            ctlScroll.ScrollChanged += _outputLayer.Scroll_OnChanged;

            var lastLayerId = layerIds.Length > 0 ? layerIds.Last() : Constants.UnknownId;

            foreach (var layerId in layerIds)
            {
                if (layerId != layerIds[0] && layerId != lastLayerId)
                {
                    AddLayer(layerId);
                }
            }

            CtlTabsLayers.SelectedIndex = (int)Config.Get(Constants.Param.SelectedLayerIndex, 0);
        }

        public int[] GetLayersSizes()
        {
            return GetLayersControls().Select(ctlLayer => ctlLayer.NeuronsCount).ToArray();
        }

        public LayerBaseControl SelectedLayer => CtlTabsLayers.SelectedTab().FindVisualChildren<LayerBaseControl>().First();

        public Type SelectedLayerType => CtlTabsLayers.SelectedTab().FindVisualChildren<LayerBaseControl>().First().GetType();

        public bool IsSelectedLayerHidden => SelectedLayerType == typeof(HiddenLayerControl);

        private RandomizeFunction RandomizeMode => RandomizeFunction.GetInstance(CtlRandomizeFunction);
        private double RandomizerParam => CtlRandomizeFunctionParam.Value;
        private double LearningRate => CtlLearningRate.Value;

        public void DeleteLayer()
        {
            if (MessageBoxResult.OK == System.Windows.MessageBox.Show($"Would you really like to delete layer L{CtlTabsLayers.SelectedIndex + 1}?",
                                                                      "Confirm",
                                                                      MessageBoxButton.OKCancel))
            {
                SelectedLayer.RemoveFromConfig();
                CtlTabsLayers.Items.Remove(CtlTabsLayers.SelectedTab());
                ResetLayersTabsNames();
                NetworkUI_OnChanged(Notification.ParameterChanged.Structure);
            }
        }

        unsafe public NetworkDataModel CreateNetworkDataModel(TaskFunction taskFunction, bool isCopy)
        {
            ErrorMatrix matrix = null;
            if (taskFunction != null)
            {
                matrix = new(taskFunction.VisualControl.GetClasses());
                ErrorMatrix nextMatrix = new(taskFunction.VisualControl.GetClasses());
                matrix.Next = nextMatrix;
                nextMatrix.Next = matrix;
            }

            NetworkDataModel networkModel = new(Id, GetLayersSizes())
            {
                ErrorMatrix = matrix,
                Classes = taskFunction?.VisualControl.GetClasses(),
                IsEnabled = CtlIsNetworkEnabled.IsOn,
                Color = CtlColor.Foreground.GetColor(),
                RandomizeMode = RandomizeMode,
                RandomizerParam = RandomizerParam,
                LearningRate = LearningRate,
                InputInitial0 = InputLayer.ActivationFunction.Do(InputLayer.Initial0,
                                                                 InputLayer.ActivationFunctionParam),

                InputInitial1 = InputLayer.ActivationFunction.Do(InputLayer.Initial1,
                                                                 InputLayer.ActivationFunctionParam),

                CostFunction = CostFunction.GetInstance(CtlCostFunction),
                BackPropagationStrategy = BackPropagationStrategy.GetInstance(CtlBackPropagationStrategy),
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

                var ctlNeurons = ctlLayers[layerInd].GetNeuronsControls().ToArray();

                for (int neuronInd = 0; neuronInd < ctlNeurons.Length; ++neuronInd)
                {
                    var neuronModel = networkModel.Layers[layerInd].Neurons[neuronInd];
                    neuronModel.VisualId = ctlNeurons[neuronInd].Id;
                    neuronModel.IsBias = ctlNeurons[neuronInd].IsBias;
                    neuronModel.IsBiasConnected = ctlNeurons[neuronInd].IsBiasConnected;

                    neuronModel.ActivationFunction = ctlNeurons[neuronInd].ActivationFunction;
                    neuronModel.ActivationFunctionParam = ctlNeurons[neuronInd].ActivationFunctionParam;


                    if (layerInd == 0 && !neuronModel.IsBias)
                    {
                        neuronModel.WeightsInitializer = InputLayer.WeightsInitializeFunction;
                        neuronModel.WeightsInitializerParam = InputLayer.WeightsInitializeFunctionParam;

                        double initValue = neuronModel.WeightsInitializer.Do(neuronModel.WeightsInitializerParam);
                        if (!InitializeFunction.IsSkipValue(initValue))
                        {
                            neuronModel.Weights.ForEach(w => w.Weight = neuronModel.WeightsInitializer.Do(neuronModel.WeightsInitializerParam));
                        }
                    }
                    else
                    {
                        neuronModel.WeightsInitializer = ctlNeurons[neuronInd].WeightsInitializeFunction;
                        neuronModel.WeightsInitializerParam = ctlNeurons[neuronInd].WeightsInitializeFunctionParam;

                        double initValue = neuronModel.WeightsInitializer.Do(neuronModel.WeightsInitializerParam);
                        if (!InitializeFunction.IsSkipValue(initValue))
                        {
                            neuronModel.Weights.ForEach(w => w.Weight = neuronModel.WeightsInitializer.Do(neuronModel.WeightsInitializerParam));
                        }
                    }

                    if (neuronModel.IsBias)
                    {
                        neuronModel.ActivationInitializer = ctlNeurons[neuronInd].ActivationInitializeFunction;
                        neuronModel.ActivationInitializerParam = ctlNeurons[neuronInd].ActivationInitializeFunctionParam;
                        double initValue = ctlNeurons[neuronInd].ActivationInitializeFunction.Do(ctlNeurons[neuronInd].ActivationInitializeFunctionParam);

                        if (!InitializeFunction.IsSkipValue(initValue))
                        {
                            neuronModel.Activation = initValue;
                        }
                    }
                    
                    if (!isCopy && prevLayerModel != null && prevLayerModel.Neurons.Count > 0)
                    {
                        neuronModel.WeightsToPreviousLayer = new(prevLayerModel.Neurons.Count);

                        var prevNeuronModel = prevLayerModel.Neurons.First;
                        while (prevNeuronModel != null)
                        {
                            if (!neuronModel.IsBias || (neuronModel.IsBiasConnected && prevNeuronModel.IsBias))
                            {
                                neuronModel.WeightsToPreviousLayer.Add(new(prevNeuronModel, prevNeuronModel.WeightTo(neuronModel)));
                            }

                            prevNeuronModel = prevNeuronModel.Next;
                        }
                    }
                }
            }

            var lastLayer = networkModel.Layers.Last;
            lastLayer.VisualId = Constants.OutputLayerId;
            {
                var ctlNeurons = _outputLayer.GetNeuronsControls().ToArray();
                for (int i = 0; i < ctlNeurons.Length; ++i)
                {
                    lastLayer.Neurons[i].VisualId = ctlNeurons[i].Id;
                }
            }

            if (!isCopy)
            {
                var copy = CreateNetworkDataModel(taskFunction, true);
                networkModel.SetCopy(copy);
            }

            return networkModel;
        }

        private void Color_OnClick(object sender, MouseButtonEventArgs e)
        {
            using ColorDialog colorDialog = new();

            var wpfColor = CtlColor.Foreground.GetColor();
            colorDialog.Color = System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B); ;
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                CtlColor.Foreground = Draw.GetBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                NetworkUI_OnChanged(Notification.ParameterChanged.Structure);
            }
        }

        private void LayerContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            CtlMenuDeleteLayer.IsEnabled = IsSelectedLayerHidden;
        }

        private void MenuAddLayer_OnClick(object sender, RoutedEventArgs e)
        {
            AddLayer();
        }

        private void MenuDeleteLayer_OnClick(object sender, RoutedEventArgs e)
        {
            DeleteLayer();
        }

        private void RandomizerButton_OnClick(object sender, RoutedEventArgs e)
        {
            RandomizerViewer viewer = new(RandomizeMode, RandomizerParam);
            viewer.Show();
        }
    }
}

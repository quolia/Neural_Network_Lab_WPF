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
    sealed public partial class NetworkControl : BaseUserControl
    {
        public readonly long Id;
        
        private readonly Action<Notification.ParameterChanged> NetworkUI_OnChanged;
        private OutputLayerControl _outputLayer;

        public InputLayerControl InputLayer { get; private set; }

        public bool IsNetworkEnabled => CtlIsNetworkEnabled.Value;

        public NetworkControl(long id, Config config, Action<Notification.ParameterChanged> onChanged)
        {
            InitializeComponent();
            NetworkUI_OnChanged = onChanged;

            Id = UniqId.GetNextId(id);
            _config = config.ExtendWithId(Id);

            _configParams = new()
            {
                CtlRandomizeFunction
                    .Initialize(nameof(RandomizeFunction.Centered)),

                CtlRandomizeFunctionParam
                    .Initialize(defaultValue: 1),

                CtlLearningRate
                    .Initialize(defaultValue: 0.03),

                CtlIsNetworkEnabled
                    .Initialize(true),

                CtlCostFunction
                    .Initialize(nameof(CostFunction.MeanSquaredError)),

                CtlBackPropagationStrategy
                    .Initialize(nameof(BackPropagationStrategy.Always))
            };

            _configParams.ForEach(p => p.SetConfig(_config));
            LoadConfig();

            _configParams.ForEach(p => p.SetOnChangeEvent(OnChanged));
        }

        private new void OnChanged(Notification.ParameterChanged _)
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
            HiddenLayerControl hiddenLayer = new(layerId, _config, NetworkUI_OnChanged);

            ScrollViewer scroll = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = hiddenLayer
            };
            scroll.ScrollChanged += hiddenLayer.Scroll_OnChanged;

            TabItem tabItem = new()
            {
                Content = scroll
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
            var layers = GetLayersControls();
            for (int i = 0; i < layers.Count; ++i)
            {
                if (layers[i].IsInput)
                {
                    CtlTabsLayers.Tab(i).Header = $"Input ({layers[i].Neurons.Count})";
                }
                else if (layers[i].IsOutput)
                {
                    CtlTabsLayers.Tab(i).Header = $"Output ({layers[i].Neurons.Count})";
                }
                else
                {
                    CtlTabsLayers.Tab(i).Header = $"L{i} ({layers[i].Neurons.Count})";
                }
            }
        }

        public override bool IsValid()
        {
            return _configParams.All(p => p.IsValid()) && GetLayersControls().All(c => c.IsValid());
        }

        public override void SaveConfig()
        {
            _config.Set(Constants.Param.SelectedLayerIndex, CtlTabsLayers.SelectedIndex);

            _config.Set(Constants.Param.Color,
                        $"{CtlColor.Foreground.GetColor().A}," +
                        $"{CtlColor.Foreground.GetColor().R}," +
                        $"{CtlColor.Foreground.GetColor().G}," +
                        $"{CtlColor.Foreground.GetColor().B}");

            _configParams.ForEach(p => p.SaveConfig());

            var layers = GetLayersControls();
            layers.ForEach(l => l.SaveConfig());
            _config.Set(Constants.Param.Layers, layers.Select(l => l.Id));

            //

            ResetLayersTabsNames();
        }

        public void NetworkTask_OnChanged(TaskFunction taskFunction)
        {
            InputLayer.NetworkTask_OnChanged(taskFunction);
            _outputLayer.NetworkTask_OnChanged(taskFunction);
        }

        public override void RemoveFromConfig()
        {
            _config.Remove(Constants.Param.SelectedLayerIndex);
            _config.Remove(Constants.Param.Color);

            _configParams.ForEach(p => p.RemoveFromConfig());

            GetLayersControls().ForEach(l => l.RemoveFromConfig());
            _config.Remove(Constants.Param.Layers);
        }

        private List<LayerBaseControl> GetLayersControls()
        {
            List<LayerBaseControl> layers = new(CtlTabsLayers.Items.Count);
            for (int i = 0; i < CtlTabsLayers.Items.Count; ++i)
            {
                layers.Add(CtlTabsLayers.Tab(i).FindVisualChildren<LayerBaseControl>().First());
            }

            return layers;
        }

        public override void LoadConfig()
        {
            CtlRandomizeFunction
                .Fill<RandomizeFunction>(_config);

            CtlCostFunction
                .Fill<CostFunction>(_config);

            CtlBackPropagationStrategy
                .Fill<BackPropagationStrategy>(_config);

            var description = BackPropagationStrategy.GetDescription(CtlBackPropagationStrategy);
            CtlBackPropagationStrategyDescription.Text = description;

            _configParams.ForEach(param => param.LoadConfig());

            var color = _config.Get(Constants.Param.Color, new long[] { 255, 100, 100, 100 });
            CtlColor.Foreground = Draw.GetBrush(Color.FromArgb((byte)color[0],
                                                               (byte)color[1],
                                                               (byte)color[2],
                                                               (byte)color[3]));
            //

            var layerIds = _config.Get(Constants.Param.Layers, Array.Empty<long>());
            var inputLayerId = layerIds.Length > 0 ? layerIds[0] : Constants.UnknownId;
            var outputLayerId = layerIds.Length > 0 ? layerIds[layerIds.Length - 1] : Constants.UnknownId;

            InputLayer = new(inputLayerId, _config, NetworkUI_OnChanged);
            ScrollViewer scroll = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = InputLayer
            };

            CtlTabInput.Content = scroll;
            scroll.ScrollChanged += InputLayer.Scroll_OnChanged;

            _outputLayer = new(outputLayerId, _config, NetworkUI_OnChanged);
            scroll = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _outputLayer
            };

            CtlTabOutput.Content = scroll;
            scroll.ScrollChanged += _outputLayer.Scroll_OnChanged;

            var lastLayerId = layerIds.Length > 0 ? layerIds.Last() : Constants.UnknownId;

            foreach (var layerId in layerIds)
            {
                if (layerId != layerIds[0] && layerId != lastLayerId)
                {
                    AddLayer(layerId);
                }
            }

            CtlTabsLayers.SelectedIndex = _config.Get(Constants.Param.SelectedLayerIndex, 0);
        }

        public int[] GetLayersSizes()
        {
            return GetLayersControls().Select(l => l.Neurons.Count).ToArray();
        }

        public LayerBaseControl SelectedLayer => CtlTabsLayers.SelectedTab().FindVisualChildren<LayerBaseControl>().First();

        public Type SelectedLayerType => CtlTabsLayers.SelectedTab().FindVisualChildren<LayerBaseControl>().First().GetType();

        public bool IsSelectedLayerHidden => SelectedLayerType == typeof(HiddenLayerControl);

        private RandomizeFunction _randomizeMode => RandomizeFunction.GetInstance(CtlRandomizeFunction);
        private double _randomizerParam => CtlRandomizeFunctionParam.Value;
        private double _learningRate => CtlLearningRate.Value;

        public void RemoveLayer()
        {
            if (MessageBoxResult.OK == System.Windows.MessageBox.Show($"Would you really like to remove layer L{CtlTabsLayers.SelectedIndex + 1}?",
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
                matrix = new(taskFunction.VisualControl.GetOutputClasses());
                ErrorMatrix nextMatrix = new(taskFunction.VisualControl.GetOutputClasses());
                matrix.Next = nextMatrix;
                nextMatrix.Next = matrix;
            }

            NetworkDataModel network = new(Id, GetLayersSizes())
            {
                ErrorMatrix = matrix,
                OutputClasses = taskFunction?.VisualControl.GetOutputClasses(),
                IsEnabled = CtlIsNetworkEnabled.Value,
                Color = CtlColor.Foreground.GetColor(),
                RandomizeMode = _randomizeMode,
                RandomizerParam = _randomizerParam,
                LearningRate = _learningRate,
                InputInitial0 = InputLayer.ActivationFunction.Do(InputLayer.Initial0,
                                                                 InputLayer.ActivationFunctionParam),

                InputInitial1 = InputLayer.ActivationFunction.Do(InputLayer.Initial1,
                                                                 InputLayer.ActivationFunctionParam),

                CostFunction = CostFunction.GetInstance(CtlCostFunction),
                BackPropagationStrategy = BackPropagationStrategy.GetInstance(CtlBackPropagationStrategy),
                IsAdjustFirstLayerWeights = InputLayer.IsAdjustFirstLayerWeights
            };

            network.ActivateNetwork();

            LayerDataModel prevLayer = null;

            var ctlLayers = GetLayersControls();
            for (int layerInd = 0; layerInd < ctlLayers.Count; ++layerInd)
            {
                if (layerInd > 0)
                {
                    prevLayer = network.Layers[layerInd - 1];
                }

                var layer = network.Layers[layerInd];

                layer.VisualId = ctlLayers[layerInd].Id;

                var ctlNeurons = ctlLayers[layerInd].Neurons.ToArray();

                for (int neuronInd = 0; neuronInd < ctlNeurons.Length; ++neuronInd)
                {
                    var neuron = network.Layers[layerInd].Neurons[neuronInd];
                    neuron.VisualId = ctlNeurons[neuronInd].Id;

                    neuron.ActivationFunction = ctlNeurons[neuronInd].ActivationFunction;
                    neuron.ActivationFunctionParam = ctlNeurons[neuronInd].ActivationFunctionParam;

                    if (layer.Next == null) // Output layer.
                    {
                        neuron.Label = ctlNeurons[neuronInd].Label;

                        neuron.PositiveTargetValue = ctlNeurons[neuronInd].PositiveTargetValue;
                        neuron.NegativeTargetValue = ctlNeurons[neuronInd].NegativeTargetValue;
                    }

                    if (layerInd == 0)
                    {
                        neuron.WeightsInitializer = InputLayer.WeightsInitializeFunction;
                        neuron.WeightsInitializerParam = InputLayer.WeightsInitializeFunctionParam;

                        double initValue = neuron.WeightsInitializer.Do(neuron.WeightsInitializerParam);
                        if (!InitializeFunction.IsSkipValue(initValue))
                        {
                            neuron.Weights.ForEach(w => w.Weight = neuron.WeightsInitializer.Do(neuron.WeightsInitializerParam));
                        }
                    }
                    else
                    {
                        neuron.WeightsInitializer = ctlNeurons[neuronInd].WeightsInitializeFunction;
                        neuron.WeightsInitializerParam = ctlNeurons[neuronInd].WeightsInitializeFunctionParam;

                        double initValue = neuron.WeightsInitializer.Do(neuron.WeightsInitializerParam);
                        if (!InitializeFunction.IsSkipValue(initValue))
                        {
                            neuron.Weights.ForEach(w => w.Weight = neuron.WeightsInitializer.Do(neuron.WeightsInitializerParam));
                        }
                    }

                   
                    if (!isCopy && prevLayer != null && prevLayer.Neurons.Count > 0)
                    {
                        neuron.WeightsToPreviousLayer = new(prevLayer.Neurons.Count);

                        var prevNeuronModel = prevLayer.Neurons.First;
                        while (prevNeuronModel != null)
                        {
                            neuron.WeightsToPreviousLayer.Add(new(prevNeuronModel, prevNeuronModel.WeightTo(neuron)));
                            prevNeuronModel = prevNeuronModel.Next;
                        }
                    }
                }
            }

            var lastLayer = network.Layers.Last;
            lastLayer.VisualId = Constants.OutputLayerId;
            {
                var ctlNeurons = _outputLayer.Neurons.ToArray();
                for (int i = 0; i < ctlNeurons.Length; ++i)
                {
                    lastLayer.Neurons[i].VisualId = ctlNeurons[i].Id;
                }
            }

            if (!isCopy)
            {
                var copy = CreateNetworkDataModel(taskFunction, true);
                network.SetCopy(copy);
            }

            return network;
        }

        private void Color_OnClick(object sender, MouseButtonEventArgs e)
        {
            using ColorDialog colorDialog = new();

            var wpfColor = CtlColor.Foreground.GetColor();
            colorDialog.Color = System.Drawing.Color.FromArgb(wpfColor.A,
                                                              wpfColor.R,
                                                              wpfColor.G,
                                                              wpfColor.B);
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                CtlColor.Foreground = Draw.GetBrush(Color.FromArgb(colorDialog.Color.A,
                                                                   colorDialog.Color.R,
                                                                   colorDialog.Color.G,
                                                                   colorDialog.Color.B));

                NetworkUI_OnChanged(Notification.ParameterChanged.Structure);
            }
        }

        private void LayerContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            CtlMenuRemoveLayer.IsEnabled = IsSelectedLayerHidden;
        }

        private void MenuAddLayer_OnClick(object sender, RoutedEventArgs e)
        {
            AddLayer();
        }

        private void MenuRemoveLayer_OnClick(object sender, RoutedEventArgs e)
        {
            RemoveLayer();
        }

        private void RandomizerButton_OnClick(object sender, RoutedEventArgs e)
        {
            RandomizerViewer viewer = new(_randomizeMode, _randomizerParam);
            viewer.Show();
        }
    }
}

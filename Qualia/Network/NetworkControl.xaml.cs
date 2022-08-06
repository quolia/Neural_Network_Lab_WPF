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
        
        private OutputLayerControl _outputLayer;

        public InputLayerControl InputLayer { get; private set; }

        public bool IsNetworkEnabled => CtlIsNetworkEnabled.Value;

        public NetworkControl(long id, Config config, Action<Notification.ParameterChanged> onChanged)
        {
            InitializeComponent();
            this.SetUIHandler(onChanged);

            Id = UniqId.GetNextId(id);
            this.PutConfig(config.ExtendWithId(Id));

            this.SetConfigParams(new()
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
            });

            this.GetConfigParams().ForEach(p => p.SetConfig(this.GetConfig()));
            LoadConfig();

            this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(OnChanged));
        }

        private new void OnChanged(Notification.ParameterChanged _)
        {
            this.InvokeUIHandler(Notification.ParameterChanged.Structure);

            //var description = BackPropagationStrategy.GetDescription(CtlBackPropagationStrategy.SelectedItem);
            //CtlBackPropagationStrategyDescription.Text = description;
        }

        public HiddenLayerControl AddLayer()
        {
            return AddLayer(Constants.UnknownId);
        }

        private HiddenLayerControl AddLayer(long layerId)
        {
            HiddenLayerControl hiddenLayer = new(layerId, this.GetConfig(), this.GetUIHandler());

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

            var index = CtlTabsLayers.Items.Count - 1;
            if (layerId == Constants.UnknownId)
            {
                index = MathX.Min(CtlTabsLayers.SelectedIndex + 1, index);
            }

            CtlTabsLayers.Items.Insert(index, tabItem);
            CtlTabsLayers.SelectedItem = tabItem;
            ResetLayersTabsNames();

            if (layerId == Constants.UnknownId)
            {
                this.InvokeUIHandler(Notification.ParameterChanged.Structure);
            }

            return hiddenLayer;
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
            return this.GetConfigParams().All(p => p.IsValid()) && GetLayersControls().All(c => c.IsValid());
        }

        public override void SaveConfig()
        {
            this.GetConfig().Set(Constants.Param.SelectedLayerIndex, CtlTabsLayers.SelectedIndex);

            this.GetConfig().Set(Constants.Param.Color,
                             $"{CtlColor.Foreground.GetColor().A}," +
                             $"{CtlColor.Foreground.GetColor().R}," +
                             $"{CtlColor.Foreground.GetColor().G}," +
                             $"{CtlColor.Foreground.GetColor().B}");

            this.GetConfigParams().ForEach(p => p.SaveConfig());

            var layers = GetLayersControls();
            layers.ForEach(l => l.SaveConfig());
            this.GetConfig().Set(Constants.Param.Layers, layers.Select(l => l.Id));

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
            this.GetConfig().Remove(Constants.Param.SelectedLayerIndex);
            this.GetConfig().Remove(Constants.Param.Color);

            this.GetConfigParams().ForEach(p => p.RemoveFromConfig());

            GetLayersControls().ForEach(l => l.RemoveFromConfig());
            this.GetConfig().Remove(Constants.Param.Layers);
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
                .Fill<RandomizeFunction>(this.GetConfig());

            CtlCostFunction
                .Fill<CostFunction>(this.GetConfig());

            CtlBackPropagationStrategy
                .Fill<BackPropagationStrategy>(this.GetConfig());

            var description = BackPropagationStrategy.GetDescription(CtlBackPropagationStrategy);
            CtlBackPropagationStrategyDescription.Text = description;

            this.GetConfigParams().ForEach(param => param.LoadConfig());

            var color = this.GetConfig().Get(Constants.Param.Color, new long[] { 255, 100, 100, 100 });
            CtlColor.Foreground = Draw.GetBrush(Color.FromArgb((byte)color[0],
                                                               (byte)color[1],
                                                               (byte)color[2],
                                                               (byte)color[3]));
            //

            var layerIds = this.GetConfig().Get(Constants.Param.Layers, Array.Empty<long>());
            var inputLayerId = layerIds.Length > 0 ? layerIds[0] : Constants.UnknownId;
            var outputLayerId = layerIds.Length > 0 ? layerIds[layerIds.Length - 1] : Constants.UnknownId;

            InputLayer = new(inputLayerId, this.GetConfig(), this.GetUIHandler());
            ScrollViewer scroll = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = InputLayer
            };

            CtlTabInput.Content = scroll;
            scroll.ScrollChanged += InputLayer.Scroll_OnChanged;

            _outputLayer = new(outputLayerId, this.GetConfig(), this.GetUIHandler());
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

            CtlTabsLayers.SelectedIndex = this.GetConfig().Get(Constants.Param.SelectedLayerIndex, 0);
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
            if (MessageBoxResult.OK == System.Windows.MessageBox.Show($"Would you really like to remove layer L{CtlTabsLayers.SelectedIndex}?",
                                                                      "Confirm",
                                                                      MessageBoxButton.OKCancel))
            {
                SelectedLayer.RemoveFromConfig();
                CtlTabsLayers.Items.Remove(CtlTabsLayers.SelectedTab());
                ResetLayersTabsNames();

                this.InvokeUIHandler(Notification.ParameterChanged.Structure);
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

                this.InvokeUIHandler(Notification.ParameterChanged.Structure);
            }
        }

        private void LayerContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            CtlMenuCloneLayer.IsEnabled = IsSelectedLayerHidden;
            CtlMenuRemoveLayer.IsEnabled = IsSelectedLayerHidden;
        }

        private void MenuAddLayer_OnClick(object sender, RoutedEventArgs e)
        {
            AddLayer();
        }

        private void MenuCloneLayer_OnClick(object sender, RoutedEventArgs e)
        {
            SelectedLayer.CopyTo(AddLayer());
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

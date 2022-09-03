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

        public InputLayerControl InputLayer { get; private set; }

        public bool IsNetworkEnabled => CtlIsNetworkEnabled.Value;

        private OutputLayerControl _outputLayer;

        public NetworkControl(long id, Config config, ActionManager.ApplyActionDelegate onChanged)
        {
            InitializeComponent();
            this.SetUIHandler(onChanged);

            Id = UniqId.GetNextId(id);
            this.PutConfig(config.ExtendWithId(Id));

            this.SetConfigParams(new()
            {
                CtlIsNetworkEnabled
                    .Initialize(true)
                    .SetUIParam(Notification.ParameterChanged.IsNetworkEnabled),

                CtlRandomizeFunction
                    .Initialize(nameof(RandomizeFunction.Centered))
                    .SetUIParam(Notification.ParameterChanged.NetworkRandomizerFunction),

                CtlRandomizeFunctionParam
                    .Initialize(defaultValue: 1)
                    .SetUIParam(Notification.ParameterChanged.NetworkRandomizerFunctionParam),

                CtlLearningRate
                    .Initialize(defaultValue: 0.03)
                    .SetUIParam(Notification.ParameterChanged.NetworkLearningRate),

                CtlCostFunction
                    .Initialize(nameof(CostFunction.MeanSquaredError)),

                CtlBackPropagationStrategy
                    .Initialize(nameof(BackPropagationStrategy.Always))
                    .SetUIParam(Notification.ParameterChanged.BackPropagationStrategy)
            });

            this.GetConfigParams().ForEach(p => p.SetConfig(this.GetConfig()));
            LoadConfig();

            this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(OnChanged));
        }

        private new void OnChanged(Notification.ParameterChanged param, ApplyAction action)
        {
            this.InvokeUIHandler(param == Notification.ParameterChanged.Unknown ? Notification.ParameterChanged.Structure : param, action);

            //var description = BackPropagationStrategy.GetDescription(CtlBackPropagationStrategy.SelectedItem);
            //CtlBackPropagationStrategyDescription.Text = description;
        }

        public HiddenLayerControl AddLayer()
        {
            return AddLayer(Constants.UnknownId);
        }

        private HiddenLayerControl AddLayer(long layerId)
        {
            ActionManager.Instance.Lock();

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
            var selectedItem = CtlTabsLayers.SelectedItem;
            CtlTabsLayers.SelectedItem = tabItem;
            ResetLayersTabsNames();

            ActionManager.Instance.Unlock();

            if (layerId == Constants.UnknownId)
            {
                ApplyAction action = new(this)
                {
                    Cancel = (isRunning) =>
                    {
                        hiddenLayer.RemoveFromConfig();
                        CtlTabsLayers.Items.Remove(tabItem);
                        CtlTabsLayers.SelectedItem = selectedItem;
                        ResetLayersTabsNames();

                        this.InvokeUIHandler(Notification.ParameterChanged.Structure, new(this));
                    }
                };

                this.InvokeUIHandler(Notification.ParameterChanged.NeuronsAdded, action);
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
            InputLayer.SetTaskFunction(taskFunction);
            _outputLayer.SetTaskFunction(taskFunction);

            ResetLayersTabsNames();
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

            ResetLayersTabsNames();
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
                var selectedLayer = SelectedLayer;
                var selectedItem = CtlTabsLayers.SelectedItem;
                var index = CtlTabsLayers.SelectedIndex;
                CtlTabsLayers.Items.Remove(selectedItem);
                ResetLayersTabsNames();

                ApplyAction action = new(this)
                {
                    Apply = (isRunning) => selectedLayer.RemoveFromConfig(),
                    Cancel = (isRunning) =>
                    {
                        CtlTabsLayers.Items.Insert(index, selectedItem);
                        CtlTabsLayers.SelectedItem = selectedItem;
                        ResetLayersTabsNames();
                    }
                };

                this.InvokeUIHandler(Notification.ParameterChanged.NeuronsRemoved, action);
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
                    
                    if (layer.Previous == null) // Input layer.
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
            if (colorDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            CtlColor.Foreground = Draw.GetBrush(Color.FromArgb(colorDialog.Color.A,
                                                                colorDialog.Color.R,
                                                                colorDialog.Color.G,
                                                                colorDialog.Color.B));

            ApplyAction action = new(this)
            {
                Cancel = (isRunning) =>
                {
                    CtlColor.Foreground = Draw.GetBrush(in wpfColor);
                    this.InvokeUIHandler(Notification.ParameterChanged.NetworkColor, new(this));
                }
            };

            this.InvokeUIHandler(Notification.ParameterChanged.NetworkColor, action);
        }

        private void LayerContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            CtlMenuCloneLayer.IsEnabled = IsSelectedLayerHidden;
            CtlMenuRemoveLayer.IsEnabled = IsSelectedLayerHidden;
        }

        private void MenuAddLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ActionManager.Instance.IsValid)
            {
                Messages.ShowError("Cannot add layer. Editor has invalid value.");
                return;
            }

            AddLayer();
        }

        private void MenuCloneLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ActionManager.Instance.IsValid)
            {
                Messages.ShowError("Cannot clone layer. Editor has invalid value.");
                return;
            }

            SelectedLayer.CopyTo(AddLayer());
        }

        private void MenuRemoveLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ActionManager.Instance.IsValid)
            {
                Messages.ShowError("Cannot remove layer. Editor has invalid value.");
                return;
            }

            RemoveLayer();
        }

        private void RandomizerButton_OnClick(object sender, RoutedEventArgs e)
        {
            RandomizerViewer viewer = new(_randomizeMode, _randomizerParam);
            viewer.Show();
        }

        public void CopyTo(NetworkControl newNetwork)
        {
            var layers = GetLayersControls();
            var newLayers = newNetwork.GetLayersControls();

            layers.First().CopyTo(newLayers.First());
            layers.Last().CopyTo(newLayers.Last());

            for (int i = 1; i < layers.Count - 1; ++i)
            {
                var layer = layers[i];
                var newLayer = i < newLayers.Count - 1 ? newLayers[i] : newNetwork.AddLayer();
                layer.CopyTo(newLayer);
            }
        }
    }
}

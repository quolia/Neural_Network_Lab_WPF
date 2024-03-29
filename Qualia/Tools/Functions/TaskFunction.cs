﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;
using Qualia.Controls.Task;
using Qualia.Models;
using Qualia.Tools.Managers;

namespace Qualia.Tools.Functions;

public interface ITaskControl : IConfigParam
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Control GetVisualControl();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int GetInputCount();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    List<string> GetOutputClasses();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ApplyChanges();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IsGridSnapAdjustmentAllowed();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int GetPointsRearrangeSnap();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SetIsPreventRepetition(bool isPreventDataRepetition);
}

public unsafe class TaskFunction : BaseFunction<TaskFunction>
{
    public readonly delegate*<NetworkDataModel, DistributionFunction, double, void> Do;

    public readonly ITaskControl VisualControl;

    public DistributionFunction DistributionFunction;
    public double DistributionFunctionParam;

    private readonly TaskSolutions _solutions;

    public TaskFunction(delegate*<NetworkDataModel, DistributionFunction, double, void> doFunc, ITaskControl taskControl)
        : base(nameof(DotsCount))
    {
        Do = doFunc;
        VisualControl = taskControl;

        _solutions = new(VisualControl.GetType());
    }

    public TaskFunction SetInputDataFunction(DistributionFunction distributionFunction)
    {
        DistributionFunction = distributionFunction;     
        return this;
    }

    public SolutionsData GetSolutionsData()
    {
        return _solutions.GetSolutionsData(_solutions.Solutions);
    }

    public sealed class DotsCount : ITaskControl
    {
        public static readonly string Description = "Network counts red dots amount.";

        public static readonly TaskFunction Instance = new(&Do, new DotsCount());

        private static readonly DotsCountControl s_control = new();

        private static int _minDotsAmountToCount;
        private static int _maxDotsAmountToCount;

        private static bool _isPreventRepetition;
        private static int _prevTargetOutputNeuronId = -100;

        private static NeuronDataModel[] _activeNeurons;

        public Control GetVisualControl() => s_control;

        public int GetPointsRearrangeSnap() => 10;

        public bool IsGridSnapAdjustmentAllowed() => true;

        public void ApplyChanges()
        {
            _minDotsAmountToCount = s_control.MinDotsAmountToCount;
            _maxDotsAmountToCount = s_control.MaxDotsAmountToCount;

            if (_activeNeurons != null)
            {
                foreach (var neuron in _activeNeurons)
                {
                    if (neuron != null)
                    {
                        neuron.Target = neuron.NegativeTargetValue;
                    }
                }    
            }
            
            _activeNeurons = new NeuronDataModel[_maxDotsAmountToCount];
        }

        public void SetIsPreventRepetition(bool isPreventDataRepetition)
        {
            _isPreventRepetition = isPreventDataRepetition;
        }

        public void SetConfig(Config config)
        {
            s_control.SetConfig(config);
        }

        public void LoadConfig()
        {
            s_control.LoadConfig();
            ApplyChanges();
        }

        public int GetInputCount() => s_control.CommonDotsAmount;

        public void SaveConfig() => s_control.SaveConfig();

        public List<string> GetOutputClasses()
        {
            List<string> classes = new();
            for (var number = s_control.MinDotsAmountToCount; number <= s_control.MaxDotsAmountToCount; ++number)
            {
                classes.Add(Converter.IntToText(number));
            }

            return classes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Do(NetworkDataModel network, DistributionFunction distributionFunction, double distributionFunctionParam)
        {
            int intNumber;
            int targetOutputNeuronId;
            var range = 1 + _maxDotsAmountToCount - _minDotsAmountToCount;

            do
            {
                var randNumber = distributionFunction.Do(distributionFunctionParam);
                randNumber = range * randNumber + _minDotsAmountToCount;

                intNumber = (int)randNumber;
                    
                if (intNumber < _minDotsAmountToCount)
                {
                    intNumber = _minDotsAmountToCount;
                }
                else if (intNumber > _maxDotsAmountToCount)
                {
                    intNumber = _maxDotsAmountToCount;
                }

                targetOutputNeuronId = intNumber - _minDotsAmountToCount;

                if (!_isPreventRepetition
                    || targetOutputNeuronId != _prevTargetOutputNeuronId
                    || _maxDotsAmountToCount == _minDotsAmountToCount)
                {
                    break;
                }
            }
            while (true);

            var neurons = network.Layers.Last.Neurons;
            if (_prevTargetOutputNeuronId > -1)
            {
                neurons[_prevTargetOutputNeuronId].Target = neurons[_prevTargetOutputNeuronId].NegativeTargetValue;
            }


            _prevTargetOutputNeuronId =
                network.TargetOutputNeuronId = targetOutputNeuronId;

            neurons = network.Layers.First.Neurons;
            var neuronsCount = neurons.Count;

            for (var i = 0; i < _activeNeurons.Length; ++i)
            {
                var activeNeuron = _activeNeurons[i];
                if (activeNeuron == null)
                {
                    continue;
                }

                activeNeuron.X =
                    activeNeuron.Activation = network.InputInitial0;
                _activeNeurons[i] = null;
            }

            NeuronDataModel neuron;

            while (intNumber > 0)
            {
                neuron = neurons[Rand.RandomFlat.Next(neuronsCount)];

                while (neuron.X == network.InputInitial1)
                {
                    neuron = neuron.Next ?? neurons.First;
                }

                neuron.X = 
                    neuron.Activation = network.InputInitial1;
                _activeNeurons[intNumber - 1] = neuron;
                --intNumber;
            }
                
            neuron = network.Layers.Last.Neurons[targetOutputNeuronId];
            neuron.Target = neuron.PositiveTargetValue;
        }

        public void RemoveFromConfig() => s_control.RemoveFromConfig();

        public bool IsValid() => s_control.IsValid();

        public void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChanged) => s_control.SetOnChangeEvent(onChanged);

        public void InvalidateValue() => throw new InvalidOperationException("Task function cannot be invalidated.");
    }

    public sealed class CrossCount : ITaskControl
    {
        private static readonly int DIMENSION = Constants.SquareRootMNIST;

        public static readonly string Description = "Network counts a simple crosses amount on the field of points.";

        public static readonly TaskFunction Instance = new(&Do, new CrossCount());

        private static readonly CrossCountControl s_control = new();

        private static int _minCrossesAmountToCount;
        private static int _maxCrossesAmountToCount;
        private static int _noisePointsAmount;

        private static bool _isPreventRepetition;
        private static int _prevCrossesAmount = -1; // todo: is inited?

        private static byte[] s_array;
        private static byte[][] s_array2;

        public Control GetVisualControl() => s_control;

        public int GetPointsRearrangeSnap() => DIMENSION;

        public bool IsGridSnapAdjustmentAllowed() => false;

        public void ApplyChanges()
        {
            _minCrossesAmountToCount = s_control.MinCrossesAmountToCount;
            _maxCrossesAmountToCount = s_control.MaxCrossesAmountToCount;
            _noisePointsAmount = s_control.NoisePointsAmount;

            Instance._solutions.Clear();
            Instance._solutions.Add(nameof(S1));
            Instance._solutions.Add(nameof(S2));
            Instance._solutions.Add(nameof(S3));
            Instance._solutions.Add(nameof(S4));
            Instance._solutions.Add(nameof(P1));
        }

        public CrossCount()
        {
            s_array = new byte[DIMENSION * DIMENSION];
            s_array2 = new byte[DIMENSION][];

            for (var i = 0; i < DIMENSION; ++i)
            {
                s_array2[i] = new byte[DIMENSION];
            }
        }

        public void SetIsPreventRepetition(bool isPreventDataRepetition)
        {
            _isPreventRepetition = isPreventDataRepetition;
        }

        public void SetConfig(Config config)
        {
            s_control.SetConfig(config);
        }

        public void LoadConfig()
        {
            s_control.LoadConfig();
            ApplyChanges();
        }

        public int GetInputCount() => DIMENSION * DIMENSION;

        public void SaveConfig() => s_control.SaveConfig();

        public List<string> GetOutputClasses() //todo: add output classes '<' and '>'
        {
            List<string> classes = new();
            for (var number = s_control.MinCrossesAmountToCount; number <= s_control.MaxCrossesAmountToCount; ++number)
            {
                classes.Add(Converter.IntToText(number));
            }

            return classes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Do(NetworkDataModel network, DistributionFunction distributionFunction, double distributionFunctionParam)
        {
            while (true)
            {
                var randNumber = distributionFunction.Do(distributionFunctionParam);

                randNumber = (1 + _maxCrossesAmountToCount - _minCrossesAmountToCount) * randNumber + _minCrossesAmountToCount;

                var intNumber = (int)randNumber;

                intNumber = (int)randNumber;

                if (intNumber < _minCrossesAmountToCount)
                {
                    intNumber = _minCrossesAmountToCount;
                }
                else if (intNumber > _maxCrossesAmountToCount)
                {
                    intNumber = _maxCrossesAmountToCount;
                }

                var neurons = network.Layers.First.Neurons;
                var neuron = neurons.First;

                while (neuron != null)
                {
                    neuron.X = 
                        neuron.Activation = network.InputInitial0;
                    
                    neuron = neuron.Next;
                }

                var number = intNumber;

                int ind;

                while (number > 0)
                {
                    var x = 1 + Rand.RandomFlat.Next(DIMENSION - 2);
                    var y = Rand.RandomFlat.Next(DIMENSION - 2);

                    ind = Rand.RandomFlat.Next(neurons.Count - DIMENSION * 2); // Exclude the last two lines.
                    var activeNeuron = neurons[ind];

                    while (activeNeuron.Activation == network.InputInitial1)
                    {
                        activeNeuron = activeNeuron.Next;
                        if (activeNeuron == null)
                        {
                            activeNeuron = neurons.First;
                        }
                    }

                    activeNeuron.X = 
                        activeNeuron.Activation = network.InputInitial1;

                    var center = neurons[ind + DIMENSION];
                    center.X = 
                        center.Activation = network.InputInitial1;

                    var left = neurons[ind + DIMENSION - 1];
                    left.X = 
                        left.Activation = network.InputInitial1;

                    var right = neurons[ind + DIMENSION + 1];
                    right.X = 
                        right.Activation = network.InputInitial1;

                    var bottom = neurons[ind + DIMENSION * 2];
                    bottom.X = 
                        bottom.Activation = network.InputInitial1;

                    --number;
                }

                for (ind = 0; ind < _noisePointsAmount; ++ind)
                {
                    var activeNeuron = neurons[Rand.RandomFlat.Next(neurons.Count)];
                    activeNeuron.X =
                        activeNeuron.Activation = network.InputInitial1;

                    var inactiveNeuron = neurons[Rand.RandomFlat.Next(neurons.Count)];
                    inactiveNeuron.X = 
                        inactiveNeuron.Activation = network.InputInitial0;
                }

                ind = 0;
                network.Layers.First.Neurons.ForEach(n =>
                {
                    s_array[ind] = (byte)(n.Activation == network.InputInitial1 ? 1 : 0);
                    ++ind;
                });

                for (var i = 0; i < DIMENSION; ++i)
                {
                    Array.Copy(s_array, i * DIMENSION, s_array2[i], 0, DIMENSION);
                }

                var crossesAmount = Instance._solutions.GetResult(new object[] { s_array, s_array2 });

                if (!_isPreventRepetition || crossesAmount != _prevCrossesAmount)
                {
                    _prevCrossesAmount = crossesAmount;

                    network.TargetOutputNeuronId = crossesAmount - _minCrossesAmountToCount;

                    neuron = network.Layers.Last.Neurons.First;
                    while (neuron != null)
                    {
                        neuron.Target = (neuron.Id == network.TargetOutputNeuronId) ? neuron.PositiveTargetValue : neuron.NegativeTargetValue;
                        neuron = neuron.Next;
                    }

                    break;
                }
            }
        }

        private static string GetMatrixFromArray(ref byte[] array)
        {
            var s = string.Join("", array);

            StringBuilder builder = new();

            for (var y = 0; y < Constants.SquareRootMNIST; ++y)
            {
                builder.AppendLine(s.Substring(y * Constants.SquareRootMNIST, Constants.SquareRootMNIST));
            }

            return builder.ToString();
        }

        private static unsafe int S1(byte[] array, byte[][] array2)
        {
            const int y_limit = Constants.SquareRootMNIST - 2;
            const int x_limit = Constants.SquareRootMNIST - 1;

            var count = 0;

            for (var y = 0; y < y_limit; ++y)
            {
                for (var x = 1; x < x_limit;)
                {
                    var top = x + y * Constants.SquareRootMNIST;
                    if (array[top] == 0) // top
                    {
                        x += 1;
                        continue;
                    }

                    if (y > 0 && array[top - Constants.SquareRootMNIST] == 1) // top-top
                    {
                        x += 2;
                        continue;
                    }

                    if (array[top + 1] == 1) // right-top
                    {
                        x += 3;
                        continue;
                    }

                    var right = (x + 1) + (y + 1) * Constants.SquareRootMNIST;
                    if (x < Constants.SquareRootMNIST - 2 && array[right + 1] == 1) // right-right
                    {
                        x += 2;
                        continue;
                    }

                    if (array[right] == 0) // right
                    {
                        x += 2;
                        continue;
                    }

                    var center = x + (y + 1) * Constants.SquareRootMNIST; // center
                    if (array[center] == 0)
                    {
                        x += 4;
                        continue;
                    }

                    var left = (x - 1) + (y + 1) * Constants.SquareRootMNIST; // left
                    if (array[left] == 0)
                    {
                        x += 4;
                        continue;
                    }

                    var bottom = x + (y + 2) * Constants.SquareRootMNIST; // bottom
                    if (array[bottom] == 0)
                    {
                        x += 4;
                        continue;
                    }

                    if (array[top - 1] == 1 // left-top
                        || array[bottom - 1] == 1 // left-bottom
                        || array[bottom + 1] == 1) // right-bottom
                    {
                        x += 4;
                        continue;
                    }

                    if (x > 1 && array[left - 1] != 0) // left-left
                    {
                        x += 4;
                        continue;
                    }

                    if (y < Constants.SquareRootMNIST - 3 && array[bottom + Constants.SquareRootMNIST] != 0) // bottom-bottom
                    {
                        x += 4;
                        continue;
                    }

                    x += 4;

                    ++count;
                }
            }

            if (count > 3)
            {
                var matrix = GetMatrixFromArray(ref array);
            }

            return count;
        }

        private static unsafe int S2(byte[] array, byte[][] array2)
        {
            const int y_limit = Constants.SquareRootMNIST - 2;
            const int x_limit = Constants.SquareRootMNIST - 1;

            var count = 0;

            for (var y = 0; y < y_limit; ++y)
            {
                for (var x = 1; x < x_limit;)
                {
                    var top = x + y * Constants.SquareRootMNIST;
                    if (array[top] == 0) // top
                    {
                        x += 1;
                        continue;
                    }

                    var right = top + Constants.SquareRootMNIST + 1;
                    if (array[right] == 0) // right
                    {
                        x += 3;
                        continue;
                    }

                    if (x < Constants.SquareRootMNIST - 2 && array[right + 1] == 1) // right-right
                    {
                        x += 2;
                        continue;
                    }

                    var center = top + Constants.SquareRootMNIST;
                    if (array[center] == 0) // center
                    {
                        x += 4;
                        continue;
                    }

                    var left = center - 1;
                    if (array[left] == 0) // left
                    {
                        x += 4;
                        continue;
                    }

                    var bottom = center + Constants.SquareRootMNIST;
                    if (array[bottom] == 0) // bottom
                    {
                        x += 4;
                        continue;
                    }

                    if (array[top + 1] == 1) // right-top
                    {
                        x += 4;
                        continue;
                    }

                    if (y > 0 && array[top - Constants.SquareRootMNIST] == 1) // top-top
                    {
                        x += 4;
                        continue;
                    }

                    if (array[top - 1] == 1 // left-top
                        || array[bottom - 1] == 1 // left-bottom
                        || array[bottom + 1] == 1) // right-bottom
                    {
                        x += 4;
                        continue;
                    }

                    if (x > 1 && array[left - 1] == 1) // left-left
                    {
                        x += 4;
                        continue;
                    }

                    if (y < 28 - 3 && array[bottom + Constants.SquareRootMNIST] == 1) // bottom-bottom
                    {
                        x += 4;
                        continue;
                    }

                    x += 4;

                    ++count;
                }
            }

            return count;
        }

        private static unsafe int S3(byte[] array, byte[][] array2)
        {
            const int y_limit = Constants.SquareRootMNIST - 2;
            const int x_limit = Constants.SquareRootMNIST - 1;

            var count = 0;

            for (var y = 0; y < y_limit; ++y)
            {
                for (var x = 1; x < x_limit;)
                {
                    if (array2[y][x] == 0) // top
                    {
                        x += 1;
                        continue;
                    }

                    if (array2[y + 1][x + 1] == 0) // right
                    {
                        x += 3;
                        continue;
                    }

                    if (array2[y + 1][x] == 0) // center
                    {
                        x += 2;
                        continue;
                    }

                    if (array2[y + 1][x - 1] == 0) // left
                    {
                        x += 4;
                        continue;
                    }

                    if (array2[y + 2][x] == 0) // bottom
                    {
                        x += 4;
                        continue;
                    }

                    if (x < 28 - 2 && array2[y + 1][x + 2] == 1) // right-right
                    {
                        x += 5;
                        continue;
                    }

                    if (array2[y][x + 1] == 1) // right-top
                    {
                        x += 4;
                        continue;
                    }

                    if (y > 0 && array2[y - 1][x] == 1) // top-top
                    {
                        x += 4;
                        continue;
                    }

                    if (array2[y][x - 1] == 1 // left-top
                        || array2[y + 2][x - 1] == 1 // left-bottom
                        || array2[y + 2][x + 1] == 1) // right-bottom
                    {
                        x += 4;
                        continue;
                    }

                    if (x > 1 && array2[y + 1][x - 2] == 1) // left-left
                    {
                        x += 4;
                        continue;
                    }

                    if (y < 28 - 3 && array2[y + 3][x] == 1) // bottom-bottom
                    {
                        x += 4;
                        continue;
                    }

                    x += 4;

                    ++count;
                }
            }

            return count;
        }

        private static unsafe int S4(byte[] array, byte[][] array2)
        {
            const int y_limit = Constants.SquareRootMNIST - 2;
            const int x_limit = Constants.SquareRootMNIST - 1;

            var count = 0;

            for (var y = 0; y < y_limit; ++y)
            {
                for (var x = 1; x < x_limit;)
                {
                    var top = x + y * Constants.SquareRootMNIST;
                    if (array[top] == 0) // top
                    {
                        x += 1;
                        continue;
                    }

                    if (array[top] > 1)
                    {
                        x += array[top]; //4;
                        array[top] = 1;

                        continue;
                    }

                    var right = top + Constants.SquareRootMNIST + 1;
                    if (array[right] == 0) // right
                    {
                        x += 3;
                        continue;
                    }

                    if (x < Constants.SquareRootMNIST - 2 && array[right + 1] == 1) // right-right
                    {
                        x += 2;
                        continue;
                    }

                    var center = top + Constants.SquareRootMNIST;
                    if (array[center] == 0) // center
                    {
                        x += 4;
                        continue;
                    }

                    var left = center - 1;
                    if (array[left] == 0) // left
                    {
                        x += 4;
                        continue;
                    }

                    var bottom = center + Constants.SquareRootMNIST;
                    if (array[bottom] == 0) // bottom
                    {
                        x += 4;
                        continue;
                    }

                    if (array[top + 1] == 1) // right-top
                    {
                        x += 4;
                        continue;
                    }

                    if (y > 0 && array[top - Constants.SquareRootMNIST] == 1) // top-top
                    {
                        x += 4;
                        continue;
                    }

                    if (array[top - 1] == 1 // left-top
                        || array[bottom - 1] == 1 // left-bottom
                        || array[bottom + 1] == 1) // right-bottom
                    {
                        x += 4;
                        continue;
                    }

                    if (x > 1 && array[left - 1] == 1) // left-left
                    {
                        x += 4;
                        continue;
                    }

                    if (y < 28 - 3 && array[bottom + Constants.SquareRootMNIST] == 1) // bottom-bottom
                    {
                        x += 4;
                        continue;
                    }

                    if (y < y_limit - 5 && x > 1 && x < x_limit - 2)
                    {
                        array[left] = 4;
                    }

                    x += 4;

                    ++count;
                }
            }

            return count;
        }

        private static int[] _mask = new int[Constants.SquareRootMNIST];
        static Func<int, int> BIT = n => 1 << n;

        private static unsafe int P1(byte[] array, byte[][] array2)
        {
            var result = 0;

            var mask = _mask;

            // Подготовка матрицы - столбеца mask[i].

            for (var y = 0; y < Constants.SquareRootMNIST; ++y)
            {
                mask[y] = 0;

                for (var x = 0; x < Constants.SquareRootMNIST; ++x)
                {
                    mask[y] <<= 1;
                    if (array[y * Constants.SquareRootMNIST + x] != 0)
                    {
                        mask[y] |= 1;
                    };
                }
            }

            var BIT_SIZE_1 = 1 << Constants.SquareRootMNIST - 1;
            var BIT_SIZE_2 = 1 << Constants.SquareRootMNIST - 2;

            int three;

            //Обработка первых трех строк матрицы (s == 0).

            three = mask[0] & mask[1] & mask[2];

            while (three != 0) // В очередных трех строках имеется хотя бы одна вертикальная тройка true.
            {
                // Выделение крайнего справа установленного бита.
                var bit_low = three & (0 - three);

                // На "краях" матрицы креста не может быть.
                if (bit_low > 1 && bit_low < BIT_SIZE_1)
                {
                    var three_bit_low = (bit_low << 1) | (bit_low >> 1);

                    // Верхние углы креста нулевые.
                    if ((mask[0] & three_bit_low) == 0)

                        // Нижние углы креста нулевые.
                        if ((mask[2] & three_bit_low) == 0)

                            // Снизу нет касания.
                            if ((mask[3] & bit_low) == 0)

                                // Справа нет касания.
                                if (bit_low == 2
                                    || (bit_low > 2
                                        && (mask[1] & (bit_low >> 2)) == 0))

                                    // Слева нет касания.
                                    if (bit_low == BIT_SIZE_2
                                        || (bit_low < BIT_SIZE_2
                                            && (mask[1] & (bit_low << 2)) == 0))

                                        // Есть горизонтальная часть креста из трех бит (mask[s+1] & bit_low заведомо == true).
                                        if ((mask[1] & three_bit_low) == three_bit_low)
                                        {
                                            result++;
                                        }
                }

                // Обнуление крайнего справа единичного бита.
                three &= three - 1;
            };

            // Обработка последних трех строк матрицы (s == SIZE-3).

            three = mask[Constants.SquareRootMNIST - 3] & mask[Constants.SquareRootMNIST - 2] & mask[Constants.SquareRootMNIST - 1];

            while (three != 0) // В очередных трех строках имеется хотя бы одна вертикальная тройка true.
            {
                // Выделение крайнего справа установленного бита.
                var bit_low = three & (0 - three);

                // На "краях" матрицы креста не может быть.
                if (bit_low > 1 && bit_low < BIT_SIZE_1)
                {
                    var three_bit_low = (bit_low << 1) | (bit_low >> 1);

                    // Верхние углы креста нулевые.
                    if ((mask[Constants.SquareRootMNIST - 3] & three_bit_low) == 0)

                        // Нижние углы креста нулевые.
                        if ((mask[Constants.SquareRootMNIST - 1] & three_bit_low) == 0)

                            // Сверху нет касания.
                            if ((mask[Constants.SquareRootMNIST - 4] & bit_low) == 0)

                                // Справа нет касания.
                                if (bit_low == 2
                                    || (bit_low > 2
                                        && (mask[Constants.SquareRootMNIST - 2] & (bit_low >> 2)) == 0))

                                    // Слева нет касания.
                                    if (bit_low == BIT_SIZE_2
                                        || (bit_low < BIT_SIZE_2
                                            && ((mask[Constants.SquareRootMNIST - 2] & (bit_low << 2)) == 0)))

                                        // Есть горизонтальная часть креста из трех бит (mask[s+1] & bit_low заведомо == true).
                                        if ((mask[Constants.SquareRootMNIST - 2] & three_bit_low) == three_bit_low)
                                        {
                                            result++;
                                        }
                }

                // Обнуление крайнего справа единичного бита.
                three &= three - 1;
            };

            //Обработка остальных строк матрицы.

            for (var s = 1; s < Constants.SquareRootMNIST - 3; ++s)
            {
                three = mask[s + 0] & mask[s + 1] & mask[s + 2];

                while (three != 0) // В очередных трех строках имеется хотя бы одна вертикальная тройка true.
                {
                    // Выделение крайнего справа установленного бита.
                    var bit_low = three & (0 - three);

                    // На "краях" матрицы креста не может быть.
                    if (bit_low > 1 && bit_low < BIT_SIZE_1)
                    {
                        var three_bit_low = (bit_low << 1) | (bit_low >> 1);

                        // Верхние углы креста нулевые.
                        if ((mask[s + 0] & three_bit_low) == 0)

                            // Нижние углы креста нулевые.
                            if ((mask[s + 2] & three_bit_low) == 0)

                                // Сверху нет касания.
                                if ((mask[s - 1] & bit_low) == 0)

                                    // Снизу нет касания.
                                    if ((mask[s + 3] & bit_low) == 0)

                                        //Справа нет касания.
                                        if (bit_low == 2
                                            || (bit_low > 2
                                                && ((mask[s + 1] & (bit_low >> 2)) == 0)))

                                            //Слева нет касания.
                                            if (bit_low == BIT_SIZE_2
                                                || (bit_low < BIT_SIZE_2
                                                    && ((mask[s + 1] & (bit_low << 2)) == 0)))

                                                // Есть горизонтальная часть креста из трех бит (mask[s+1] & bit_low заведомо == true).
                                                if ((mask[s + 1] & three_bit_low) == three_bit_low)
                                                {
                                                    result++;
                                                }
                    }

                    three &= three - 1; // Обнуление крайнего справа единичного бита.
                };
            }

            return result;
        }

        public void RemoveFromConfig() => s_control.RemoveFromConfig();

        public bool IsValid() => s_control.IsValid();

        public void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChanged) => s_control.SetOnChangeEvent(onChanged);

        public void InvalidateValue() => throw new InvalidOperationException("Task function cannot be invalidated.");
    }


    public sealed class MNISTNumbers : ITaskControl
    {
        public static readonly string Description = "Network recognizes hand-written numbers.";

        public static readonly TaskFunction Instance = new(&Do, new MNISTNumbers());

        private static readonly MNISTControl s_control = new();

        private static int _minNumber;
        private static int _maxNumber;

        private static bool _isPreventRepetition;
        private static int _prevTargetOutputNeuronId = -1;

        public Control GetVisualControl() => s_control;

        public int GetPointsRearrangeSnap() => 28;

        public bool IsGridSnapAdjustmentAllowed() => false;

        public void ApplyChanges()
        {
            _minNumber = s_control.MinNumber;
            _maxNumber = s_control.MaxNumber;
        }

        public void SetIsPreventRepetition(bool isPreventDataRepetition)
        {
            _isPreventRepetition = isPreventDataRepetition;
        }

        public void SetConfig(Config config) => s_control.SetConfig(config);

        public void LoadConfig()
        {
            s_control.LoadConfig();
            ApplyChanges();
        }

        public void SaveConfig() => s_control.SaveConfig();

        public int GetInputCount() => 28 * 28;

        public List<string> GetOutputClasses()
        {
            List<string> classes = new();
            for (var number = s_control.MinNumber; number <= s_control.MaxNumber; ++number)
            {
                classes.Add(Converter.IntToText(number));
            }

            return classes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Do(NetworkDataModel network, DistributionFunction distributionFunction, double distributionFunctionParam)
        {
            int intNumber;

            while (true)
            { 
                var randNumber = distributionFunction.Do(distributionFunctionParam);

                randNumber = (1 + _maxNumber - _minNumber) * randNumber + _minNumber;

                intNumber = (int)randNumber;

                if (intNumber < _minNumber)
                {
                    intNumber = _minNumber;
                }
                else if (intNumber > _maxNumber)
                {
                    intNumber = _maxNumber;
                }

                if (!_isPreventRepetition
                    || intNumber != _prevTargetOutputNeuronId
                    || _maxNumber == _minNumber)
                {
                    _prevTargetOutputNeuronId = network.TargetOutputNeuronId;
                    break;
                }
            }

            var rand = Rand.RandomFlat.NextDouble();

            var id = (int)(s_control.Images.Count * rand);
            var image = s_control.Images[id];
            while (image.Label != intNumber)
            {
                ++id;
                if (id == s_control.Images.Count)
                {
                    id = 0;
                }

                image = s_control.Images[id];
            }

            var count = network.Layers.First.Neurons.Count;

            for (var i = 0; i < count; ++i)
            {
                var n = network.Layers.First.Neurons[i];
                n.Activation =
                    n.X = n.ActivationFunction.Do(network.InputInitial1 * image.Image[i] / 255, n.ActivationFunctionParam);
            }

            network.TargetOutputNeuronId = image.Label - _minNumber;

            var neuron = network.Layers.Last.Neurons.First;
            while (neuron != null)
            {
                neuron.Target = (neuron.Id == network.TargetOutputNeuronId) ? neuron.PositiveTargetValue : neuron.NegativeTargetValue;
                neuron = neuron.Next;
            }
        }

        public void RemoveFromConfig() => s_control.RemoveFromConfig();

        public bool IsValid() => s_control.IsValid();

        public void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChanged) => s_control.SetOnChangeEvent(onChanged);

        public void InvalidateValue() => throw new InvalidOperationException("Task function cannot be invalidated.");
    }
}
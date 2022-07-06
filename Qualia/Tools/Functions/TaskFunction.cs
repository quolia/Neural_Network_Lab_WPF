using Qualia.Controls;
using Qualia.Model;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Controls;
using System.Linq;
using System.Text;

namespace Qualia.Tools
{
    public interface ITaskControl : IConfigParam
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Control GetVisualControl();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetInputCount();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        List<string> GetClasses();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ApplyChanges();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsGridSnapAdjustmentAllowed();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetPointsRearrangeSnap();
    }

    unsafe public class TaskFunction : BaseFunction<TaskFunction>
    {
        public readonly delegate*<NetworkDataModel, InputDataFunction, double, void> Do;

        public ITaskControl ITaskControl;

        public InputDataFunction InputDataFunction;
        public double InputDataFunctionParam;

        private readonly TaskSolutions _solutions;

        public TaskFunction(delegate*<NetworkDataModel, InputDataFunction, double, void> doFunc, ITaskControl taskControl)
            : base(nameof(DotsCount))
        {
            Do = doFunc;
            ITaskControl = taskControl;

            _solutions = new(ITaskControl.GetType());
    }

        public TaskFunction SetInputDataFunction(InputDataFunction function)
        {
            InputDataFunction = function;     
            return this;
        }
        public SolutionsData GetSolutionsData()
        {
            return _solutions.GetSolutionsData(_solutions.Solutions);
        }

        sealed public class DotsCount : ITaskControl
        {
            public static readonly string Description = "Network counts red dots amount.";

            public static readonly TaskFunction Instance = new(&Do, new DotsCount());

            private static readonly DotsCountControl s_control = new();

            private static int _minNumber;
            private static int _maxNumber;

            public Control GetVisualControl() => s_control;

            public int GetPointsRearrangeSnap() => 10;

            public bool IsGridSnapAdjustmentAllowed() => true;

            public void ApplyChanges()
            {
                _minNumber = s_control.MinNumber;
                _maxNumber = s_control.MaxNumber;
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

            public int GetInputCount() => s_control.InputCount;

            public void SaveConfig() => s_control.SaveConfig();

            public List<string> GetClasses()
            {
                List<string> classes = new();
                for (int number = s_control.MinNumber; number <= s_control.MaxNumber; ++number)
                {
                    classes.Add(Converter.IntToText(number));
                }

                return classes;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, InputDataFunction inputDataFunction, double inputDataFunctionParam)
            {
                double randNumber = inputDataFunction.Do(inputDataFunctionParam);

                randNumber = (1 + _maxNumber - _minNumber) * randNumber + _minNumber;

                var intNumber = (int)randNumber;

                networkModel.TargetOutput = intNumber;

                var neurons = networkModel.Layers.First.Neurons;
                var neuron = neurons.First;

                while (neuron != null)
                {
                    neuron.X = networkModel.InputInitial0; // ?
                    neuron.Activation = networkModel.InputInitial0;
                    neuron = neuron.Next;
                }

                while (intNumber > 0)
                {
                    var active = neurons[Rand.RandomFlat.Next(neurons.Count)];

                    while (active.Activation == networkModel.InputInitial1)
                    {
                        active = active.Next;
                        if (active == null)
                        {
                            active = neurons.First;
                        }
                    }

                    active.X = networkModel.InputInitial1; // ?
                    active.Activation = networkModel.InputInitial1;
                    --intNumber;
                }

                neuron = networkModel.Layers.Last.Neurons.First;
                while (neuron != null)
                {
                    neuron.Target = (neuron.Id == networkModel.TargetOutput) ? 1 : 0;
                    neuron = neuron.Next;
                }
            }

            public void RemoveFromConfig() => s_control.RemoveFromConfig();

            public bool IsValid() => s_control.IsValid();

            public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged) => s_control.SetOnChangeEvent(onChanged);

            public void InvalidateValue() => throw new InvalidOperationException();
        }

        sealed public class MNISTNumbers : ITaskControl
        {
            public static readonly string Description = "Network recognizes hand-written numbers.";

            public static readonly TaskFunction Instance = new(&Do, new MNISTNumbers());

            private static readonly MNISTControl s_control = new();

            public Control GetVisualControl() => s_control;

            public int GetPointsRearrangeSnap() => 28;

            public bool IsGridSnapAdjustmentAllowed() => false;

            public void ApplyChanges()
            {
                //
            }

            public void SetConfig(Config config) => s_control.SetConfig(config);

            public void LoadConfig()
            {
                s_control.LoadConfig();
                ApplyChanges();
            }

            public void SaveConfig() => s_control.SaveConfig();

            public int GetInputCount() => 28 * 28;

            public List<string> GetClasses()
            {
                List<string> classes = new();
                for (int number = s_control.MinNumber; number <= s_control.MaxNumber; ++number)
                {
                    classes.Add(Converter.IntToText(number));
                }

                return classes;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, InputDataFunction inputDataFunction, double inputDataFunctionParam)
            {
                var image = s_control.Images[(int)(s_control.Images.Count * inputDataFunction.Do(inputDataFunctionParam))];
                var count = networkModel.Layers.First.Neurons.Count;

                for (int i = 0; i < count; ++i)
                {
                    networkModel.Layers.First.Neurons[i].X = networkModel.InputInitial1 * image.Image[i]; // ?
                    networkModel.Layers.First.Neurons[i].Activation = networkModel.InputInitial1 * image.Image[i];
                }

                var neuron = networkModel.Layers.Last.Neurons.First;
                while (neuron != null)
                {
                    neuron.Target = (neuron.Id == image.Label) ? 1 : 0;
                    neuron = neuron.Next;
                }

                networkModel.TargetOutput = image.Label;
            }

            public void RemoveFromConfig() => s_control.RemoveFromConfig();

            public bool IsValid() => s_control.IsValid();

            public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged) => s_control.SetOnChangeEvent(onChanged);

            public void InvalidateValue() => throw new InvalidOperationException();
        }

        sealed public class CrossCount : ITaskControl
        {
            private const int SIZE = 28;

            public static readonly string Description = "Network counts a simple croosses amount on the field of points.";

            public static readonly TaskFunction Instance = new(&Do, new CrossCount());

            private static readonly CrossCountControl s_control = new();

            private static int _maxPointsCount;

            public Control GetVisualControl() => s_control;

            public int GetPointsRearrangeSnap() => SIZE;

            public bool IsGridSnapAdjustmentAllowed() => false;

            private static byte[] s_array;
            private static byte[][] s_array2;

            public CrossCount()
            {
                s_array = new byte[SIZE * SIZE];
                s_array2 = new byte[SIZE][];

                for (int i = 0; i < SIZE; ++i)
                {
                    s_array2[i] = new byte[SIZE];
                }
            }

            public void ApplyChanges()
            {
                _maxPointsCount = s_control.MaxPointsCount;

                Instance._solutions.Clear();
                Instance._solutions.Add(nameof(S1));
                Instance._solutions.Add(nameof(S2));
                Instance._solutions.Add(nameof(M1));
                Instance._solutions.Add(nameof(A1));
                Instance._solutions.Add(nameof(P1));
            }

            public void SetConfig(Config config) => s_control.SetConfig(config);

            public void LoadConfig()
            {
                s_control.LoadConfig();
                ApplyChanges();
            }

            public void SaveConfig() => s_control.SaveConfig();

            public int GetInputCount() => 28 * 28;

            public List<string> GetClasses()
            {
                List<string> classes = new();
                for (int number = 0; number < 2; ++number) // outputs: no, yes
                {
                    classes.Add(Converter.IntToText(number));
                }

                return classes;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Do(NetworkDataModel networkModel, InputDataFunction inputDataFunction, double inputDataFunctionParam)
            {
                int maxPointsCount = _maxPointsCount; // 100

                double randNumber = maxPointsCount;// inputDataFunction.Do(inputDataFunctionParam);

                var intNumber = (int)randNumber;

                var neurons = networkModel.Layers.First.Neurons;
                var neuron = neurons.First;

                while (neuron != null)
                {
                    neuron.X = networkModel.InputInitial0; // ?
                    neuron.Activation = networkModel.InputInitial0;
                    neuron = neuron.Next;
                }

                while (intNumber > 0)
                {
                    var active = neurons[Rand.RandomFlat.Next(neurons.Count)];

                    while (active.Activation == networkModel.InputInitial1)
                    {
                        active = active.Next;
                        if (active == null)
                        {
                            active = neurons.First;
                        }
                    }

                    active.X = networkModel.InputInitial1; // ?
                    active.Activation = networkModel.InputInitial1;
                    --intNumber;
                }

                int ind = 0;
                networkModel.Layers.First.Neurons.ForEach(n =>
                {
                    s_array[ind] = (byte)(n.Activation == networkModel.InputInitial1 ? 1 : 0);
                    ++ind;
                });

                for (int i = 0; i < SIZE; ++i)
                {
                    Array.Copy(s_array, i * 28, s_array2[i], 0, SIZE);
                }

                int targetOutput = Instance._solutions.GetTargetOutput(new object[] { s_array, s_array2 });

                neuron = networkModel.Layers.Last.Neurons.First;
                neuron.Target = targetOutput == 0 ? 1 : 0; // no
                neuron.Next.Target = targetOutput == 1 ? 1 : 0; // yes

                networkModel.TargetOutput = targetOutput;
            }

            private static string GetMatrixFromArray(ref byte[] array)
            {
                var s = string.Join("", array);

                StringBuilder builder = new();

                for (int y = 0; y < 28; ++y)
                {
                    builder.AppendLine(s.Substring(y * 28, 28));
                }

                return builder.ToString();
            }

            [TaskSolution]
            private static int S1(byte[] array, byte[][] array2)
            {
                int len = array.Length;
                int y_limit = len / 28 - 2;
                int x_limit = 28 - 1;

                int count = 0;

                for (int y = 0; y < y_limit; ++y)
                {
                    for (int x = 1; x < x_limit;)
                    {
                        var top = x + y * 28;
                        if (array[top] == 0) // top
                        {
                            x += 1;
                            continue;
                        }

                        if (y > 0 && array[top - 28] == 1) // top-top
                        {
                            x += 2;
                            continue;
                        }

                        if (array[top + 1] != 0) // right-top
                        {
                            x += 3;
                            continue;
                        }

                        var right = (x + 1) + (y + 1) * 28;
                        if (x < 28 - 2 && array[right + 1] != 0) // right-right
                        {
                            x += 5;
                            continue;
                        }

                        if (array[right] == 0) // right
                        {
                            x += 3;
                            continue;
                        }

                        var center = x + (y + 1) * 28; // center
                        if (array[center] == 0)
                        {
                            x += 4;
                            continue;
                        }

                        var left = (x - 1) + (y + 1) * 28; // left
                        if (array[left] == 0)
                        {
                            x += 4;
                            continue;
                        }

                        var bottom = x + (y + 2) * 28; // bottom
                        if (array[bottom] == 0)
                        {
                            x += 4;
                            continue;
                        }

                        if (array[top - 1] != 0 // left-top
                            || array[bottom - 1] != 0 // left-bottom
                            || array[bottom + 1] != 0) // right-bottom
                        {
                            x += 4;
                            continue;
                        }

                        if (x > 1 && array[left - 1] != 0) // left-left
                        {
                            x += 4;
                            continue;
                        }

                        if (y < 28 - 3 && array[bottom + 28] != 0) // bottom-bottom
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
                    //var matrix = GetMatrixFromArray(ref array);
                }

                return count > 0 ? 1 : 0;
            }

            [TaskSolution]
            private static int S2(byte[] array, byte[][] array2)
            {
                int len = array.Length;
                int y_limit = len / 28 - 2;
                int x_limit = 28 - 1;

                int count = 0;

                for (int y = 0; y < y_limit; ++y)
                {
                    for (int x = 1; x < x_limit;)
                    {
                        var top = x + y * 28;
                        if (array[top] == 0) // top
                        {
                            x += 1;
                            continue;
                        }

                        var right = (x + 1) + (y + 1) * 28;
                        if (array[right] == 0) // right
                        {
                            x += 2;
                            continue;
                        }

                        var center = x + (y + 1) * 28; // center
                        if (array[center] == 0)
                        {
                            x += 3;
                            continue;
                        }

                        var left = (x - 1) + (y + 1) * 28; // left
                        if (array[left] == 0)
                        {
                            x += 3;
                            continue;
                        }

                        var bottom = x + (y + 2) * 28; // bottom
                        if (array[bottom] == 0)
                        {
                            x += 3;
                            continue;
                        }

                        if (x < 28 - 2 && array[right + 1] == 1) // right-right
                        {
                            x += 5;
                            continue;
                        }

                        if (array[top + 1] == 1) // right-top
                        {
                            x += 4;
                            continue;
                        }

                        if (y > 0 && array[top - 28] == 1) // top-top
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

                        if (y < 28 - 3 && array[bottom + 28] == 1) // bottom-bottom
                        {
                            x += 4;
                            continue;
                        }

                        x += 4;

                        ++count;
                    }
                }

                return count > 0 ? 1 : 0;
            }

            [TaskSolution]
            private static int M1(byte[] array, byte[][] array2)
            {
                //Thread.Sleep(2);

                int b = 0;
                for (int i = 0; i < 1000 + Rand.RandomFlat.Next() % 2000; ++i)
                {
                    b = b * i;
                    int a = b * b;
                    a = a - i;
                    b = a;
                }

                return Rand.RandomFlat.Next() % 2;
            }

            [TaskSolution()]
            private static int A1(byte[] array, byte[][] array2)
            {
                return M1(array, array2);
            }

            [TaskSolution()]
            private static int P1(byte[] array, byte[][] array2)
            {
                return M1(array, array2);
            }

            public void RemoveFromConfig() => s_control.RemoveFromConfig();

            public bool IsValid() => s_control.IsValid();

            public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged) => s_control.SetOnChangeEvent(onChanged);

            public void InvalidateValue() => throw new InvalidOperationException();
        }
    }
}

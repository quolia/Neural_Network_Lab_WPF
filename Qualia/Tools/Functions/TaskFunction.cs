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
        public readonly delegate*<NetworkDataModel, DistributionFunction, double, void> Do;

        public ITaskControl ITaskControl;

        public DistributionFunction DistributionFunction;
        public double DistributionFunctionParam;

        private readonly TaskSolutions _solutions;

        public TaskFunction(delegate*<NetworkDataModel, DistributionFunction, double, void> doFunc, ITaskControl taskControl)
            : base(nameof(DotsCount))
        {
            Do = doFunc;
            ITaskControl = taskControl;

            _solutions = new(ITaskControl.GetType());
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
            public static void Do(NetworkDataModel networkModel, DistributionFunction distributionFunction, double distributionFunctionParam)
            {
                double randNumber = distributionFunction.Do(distributionFunctionParam);

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
            public static void Do(NetworkDataModel networkModel, DistributionFunction distributionFunction, double distributionFunctionParam)
            {
                var image = s_control.Images[(int)(s_control.Images.Count * distributionFunction.Do(distributionFunctionParam))];
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
                Instance._solutions.Add(nameof(S3));
                //Instance._solutions.Add(nameof(M1));
                //Instance._solutions.Add(nameof(A1));
                Instance._solutions.Add(nameof(P1));
            }

            public void SetConfig(Config config) => s_control.SetConfig(config);

            public void LoadConfig()
            {
                s_control.LoadConfig();
                ApplyChanges();
            }

            public void SaveConfig() => s_control.SaveConfig();

            public int GetInputCount() => SIZE * SIZE;

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
            public static void Do(NetworkDataModel networkModel, DistributionFunction distributionFunction, double distributionFunctionParam)
            {
                int maxPointsCount = _maxPointsCount; // 100

                double randNumber = maxPointsCount;// distributionFunction.Do(distributionFunctionParam);

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
                    Array.Copy(s_array, i * SIZE, s_array2[i], 0, SIZE);
                }

                int targetOutput = Instance._solutions.GetTargetOutput(new object[] { s_array, s_array2 });

                targetOutput = targetOutput > 1 ? 1 : targetOutput;

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
                //var s = "0000000000100000000000000000001000000010000000000001000000010010000001000000000000000110000000000000000000100001000000000000000100000100001000000100000000000000110000000100000000000010001000000011000000100000000000010000001100000001100011000000000000000000000000000010000000001001000000100010000000000000010000000000000000001000000000100000000001001110000000000010000000000000010100001010000000000000000010011000000010000000000000000010000000000000001010100100000001100100000001000000111000010010000000000000000101000100010000000000000000000000010000000011000010000100000010000000001000000000000010000000100000000000100000100100010000000010010001000000000000110000000000000000000000001000001001001000000000000000000000000000000000001100000000000010001000000000010000011001100000000000";
                //array = s.Select(c => byte.Parse(c.ToString())).ToArray();

                //array = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 1 };



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

                        if (array[top + 1] == 1) // right-top
                        {
                            x += 3;
                            continue;
                        }

                        var right = (x + 1) + (y + 1) * 28;
                        if (x < 28 - 2 && array[right + 1] == 1) // right-right
                        {
                            x += 2;
                            continue;
                        }

                        if (array[right] == 0) // right
                        {
                            x += 2;
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
                    var matrix = GetMatrixFromArray(ref array);
                }

                return count;
            }

            [TaskSolution]
            private static int S2(byte[] array, byte[][] array2)
            {
                const int y_limit = SIZE - 2;
                const int x_limit = SIZE - 1;

                int count = 0;

                for (int y = 0; y < y_limit; ++y)
                {
                    for (int x = 1; x < x_limit;)
                    {
                        int top = x + y * 28;
                        if (array[top] == 0) // top
                        {
                            x += 1;
                            continue;
                        }

                        int right = top + 29;
                        if (array[right] == 0) // right
                        {
                            x += 3;
                            continue;
                        }

                        if (x < 28 - 2 && array[right + 1] == 1) // right-right
                        {
                            x += 2;
                            continue;
                        }

                        int center = top + 28; 
                        if (array[center] == 0) // center
                        {
                            x += 4;
                            continue;
                        }

                        int left = center - 1; 
                        if (array[left] == 0) // left
                        {
                            x += 4;
                            continue;
                        }

                        int bottom = center + 28;
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

                return count;
            }


            [TaskSolution]
            private static int S3(byte[] array, byte[][] array2)
            {
                int len = array.Length;
                int y_limit = len / 28 - 2;
                int x_limit = 28 - 1;

                int count = 0;

                for (int y = 0; y < y_limit; ++y)
                {
                    for (int x = 1; x < x_limit;)
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

            private static uint[] mask = new uint[SIZE];
            static Func<int, uint> BIT = n => ((uint)((1 << n)));

            static bool first = true;

            [TaskSolution()]
            unsafe private static int P1(byte[] array, byte[][] array2)
            {


                int result = 0;
                //uint[] mask = new uint[SIZE]; //Матрица - столбец из long, где в каждом mask[i] установленный бит соответствует true из array[28][28]
                //for (int i = 0; i < SIZE; ++i)
                {
                    //  mask[i] = 0;
                }

                //if (first)
                {
                    first = false;
                    for (int s = 0; s < SIZE; s++)           //Подготовка матрицы - столбеца mask[i]
                    {
                        mask[s] = 0;

                        for (int c = 0; c < SIZE; c++)
                        {
                            mask[s] <<= 1;
                            if (array[s * SIZE + c] != 0)
                            {
                                mask[s] |= 1;
                            };
                        }
                    }
                }

                uint three;

                //Обработка первых трех строк матрицы (s == 0)
                three = mask[0] & mask[1] & mask[2];                          //if(three) - в очередных трех строках имеется хотя бы одна вертикальная тройка true

                while (three != 0)
                {
                    uint bit_low = three & (0-three);                           //Выделение крайнего справа установленного бита

                    if (((bit_low & 1) == 0) && ((bit_low & BIT(SIZE - 1)) == 0)) //На "краях" матрицы креста не может быть
                    {
                        uint three_bit_low = (bit_low << 1) | (bit_low >> 1);

                        if ((mask[1] & three_bit_low) == three_bit_low)                               //Есть горизонтальная часть креста из трех бит (mask[s+1] & bit_low заведомо == true)
                            if ((mask[0] & three_bit_low) == 0)                        //Верхние углы креста нулевые
                                if ((mask[2] & three_bit_low) == 0)                      //Нижние углы креста нулевые
                                    if ((mask[3] & bit_low) == 0)                          //Снизу нет касания

                                        if ((bit_low == 2) || ((bit_low > 2) && ((mask[1] & (bit_low >> 2)) == 0)))             //Справа нет касания
                                            if ((bit_low == BIT(SIZE - 2)) || ((bit_low < BIT(SIZE - 2)) && ((mask[1] & (bit_low << 2)) == 0))) //Слева нет касания
                                                result++;
                    }

                    three &= (three - 1);                                    //Обнуление крайнего справа единичного бита
                };

                //Обработка последних трех строк матрицы (s == SIZE-3)
                three = mask[SIZE - 3] & mask[SIZE - 2] & mask[SIZE - 1];        //if(three)- в очередных трех строках имеется хотя бы одна вертикальная тройка true

                while (three != 0)
                {
                    uint bit_low = three & (0-three);                            //Выделение крайнего справа установленного бита

                    if (((bit_low & 1) == 0) && ((bit_low & BIT(SIZE - 1)) == 0))  //На "краях" матрицы креста не может быть
                    {
                        uint three_bit_low = (bit_low << 1) | (bit_low >> 1);

                        if ((mask[SIZE - 2] & three_bit_low) == three_bit_low)                       //Есть горизонтальная часть креста из трех бит (mask[s+1] & bit_low заведомо == true)
                            if ((mask[SIZE - 3] & three_bit_low) == 0)                //Верхние углы креста нулевые
                                if ((mask[SIZE - 1] & three_bit_low) == 0)              //Нижние углы креста нулевые
                                    if ((mask[SIZE - 4] & bit_low) == 0)                  //Сверху нет касания

                                        if ((bit_low == 2) || ((bit_low > 2) && ((mask[SIZE - 2] & (bit_low >> 2)) == 0)))             //Справа нет касания
                                            if ((bit_low == BIT(SIZE - 2)) || ((bit_low < BIT(SIZE - 2)) && ((mask[SIZE - 2] & (bit_low << 2)) == 0))) //Слева нет касания
                                                result++;
                    }

                    three &= (three - 1);                                     //Обнуление крайнего справа единичного бита
                };

                //Обработка остальных строк матрицы
                for (int s = 1; s < SIZE - 3; s++)
                {
                    three = mask[s + 0] & mask[s + 1] & mask[s + 2];                //if(three) - в очередных трех строках имеется хотя бы одна вертикальная тройка true

                    while (three != 0)
                    {
                        uint bit_low = three & (0-three);                           //Выделение крайнего справа установленного бита

                        if (((bit_low & 1) == 0) && ((bit_low & BIT(SIZE - 1)) == 0)) //На "краях" матрицы креста не может быть
                        {
                            uint three_bit_low = (bit_low << 1) | (bit_low >> 1);

                            if ((mask[s + 1] & three_bit_low) == three_bit_low)                         //Есть горизонтальная часть креста из трех бит (mask[s+1] & bit_low заведомо == true)
                                if ((mask[s + 0] & three_bit_low) == 0)                  //Верхние углы креста нулевые
                                    if ((mask[s + 2] & three_bit_low) == 0)                //Нижние углы креста нулевые
                                        if ((mask[s - 1] & bit_low) == 0)                    //Сверху нет касания
                                            if ((mask[s + 3] & bit_low) == 0)                  //Снизу нет касания

                                                if ((bit_low == 2) || ((bit_low > 2) && ((mask[s + 1] & (bit_low >> 2)) == 0)))             //Справа нет касания
                                                    if ((bit_low == BIT(SIZE - 2)) || ((bit_low < BIT(SIZE - 2)) && ((mask[s + 1] & (bit_low << 2)) == 0))) //Слева нет касания
                                                        result++;
                        }

                        three &= (three - 1);                                   //Обнуление крайнего справа единичного бита
                    };
                }

                return result;
            }

            public void RemoveFromConfig() => s_control.RemoveFromConfig();

            public bool IsValid() => s_control.IsValid();

            public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged) => s_control.SetOnChangeEvent(onChanged);

            public void InvalidateValue() => throw new InvalidOperationException();
        }
    }
}

using Qualia;
using Qualia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Tools
{
    public interface INetworkTask : IConfigValue
    {
        void Do(NetworkDataModel model);
        Control GetVisualControl();
        void RebuildNetwork(NetworkControl network);
        int GetInputCount();
    }

    public interface INetworkTaskChanged
    {
        void TaskChanged();
        void TaskParameterChanged();
    }

    public static class NetworkTask
    {
        static Dictionary<int, double[]> _arrays = new Dictionary<int, double[]>();

        public class CountDotsSymmetric : INetworkTask
        {
            public static INetworkTask Instance = new CountDotsSymmetric();
            static CountDotsControl Control = new CountDotsControl();

            public CountDotsSymmetric()
            {

            }

            public void LoadConfig(Config config)
            {

            }

            public void SaveConfig(Config config)
            {

            }

            public Control GetVisualControl()
            {
                return Control;
            }

            public void RebuildNetwork(NetworkControl network)
            {

            }

            public int GetInputCount()
            {
                return 0;
            }

            public void Do(NetworkDataModel model)
            {
                int bound = 10;

                int number = Rand.Flat.Next(bound + 1);
                if (number == 0)
                {
                    return;
                }

                if (!_arrays.ContainsKey(bound))
                {
                    _arrays.Add(bound, new double[bound]);
                }

                for (int i = 0; i < _arrays[bound].Length; ++i)
                {
                    _arrays[bound][i] = i < number ? model.InputInitial1 : model.InputInitial0;
                }

                var shaffle = _arrays[bound].OrderBy(a => Rand.Flat.Next()).ToArray();

                int k = 0;
                Range.ForEach(model.Layers.First().Neurons.Where(n => !n.IsBias), n => n.Activation = shaffle[k++]);
            }

            public void Load(Config config)
            {

            }

            public void Save(Config config)
            {

            }

            public void Vanish(Config config)
            {

            }

            public bool IsValid()
            {
                return false;
            }

            public void SetChangeEvent(Action action)
            {

            }
        }

        public class CountDotsAsymmetric : INetworkTask
        {
            public static INetworkTask Instance = new CountDotsAsymmetric();
            static CountDotsControl Control = new CountDotsControl();

            public Control GetVisualControl()
            {
                return Control;
            }

            public void Load(Config config)
            {
                Control.Load(config);
            }

            public void Save(Config config)
            {
                Control.Save(config);
            }

            public void RebuildNetwork(NetworkControl network)
            {
                network.InputLayer.OnInputDataChanged(Control.InputCount);
            }

            public int GetInputCount()
            {
                return Control.InputCount;
            }

            public void Do(NetworkDataModel model)
            {
                Range.For(Rand.Flat.Next(11), i => model.Layers.First().Neurons.RandomElementTrimEnd(model.Layers.First().BiasCount).Activation = model.InputInitial1);
            }

            public void Vanish(Config config)
            {
                Control.Vanish(config);
            }

            public bool IsValid()
            {
                return Control.IsValid();
            }

            public void SetChangeEvent(Action action)
            {
                Control.SetChangeEvent(action);
            }
        }

        public static class Helper
        {
            public static string[] GetItems()
            {
                return typeof(NetworkTask).GetNestedTypes().Where(c => typeof(INetworkTask).IsAssignableFrom(c)).Select(c => c.Name).ToArray();
            }

            public static INetworkTask GetInstance(string name)
            {
                return (INetworkTask)typeof(NetworkTask).GetNestedTypes().Where(c => c.Name == name).First().GetField("Instance").GetValue(null);
            }

            public static void FillComboBox(ComboBox cb, Config config, Const.Param param, string defaultValue)
            {
                Initializer.FillComboBox(typeof(NetworkTask.Helper), cb, config, param, defaultValue);
            }
        }
    }

    public static class NetworkTaskResult
    {
        public static class Helper
        {
            public static double Invoke(string name, double a)
            {
                var method = typeof(NetworkTaskResult).GetMethod(name);
                return (double)method.Invoke(null, new object[] { a });
            }
        }
    }
}

using Qualia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tools;

namespace Tools
{
    public static class Threads
    {
        public static byte GetAffinityMaksForProcessors(byte[] proc)
        {
            byte result = 0;

            foreach (byte p in proc)
            {
                result |= (byte)(1 << (p % Environment.ProcessorCount));
            }

            return result;
        }

        public static byte GetProcessorMask(byte processor)
        {
            return (byte)(1 << (processor - 1)); 
        }

        public static byte[] GetAllProcessorsList()
        {
            byte[] result = new byte[Environment.ProcessorCount];

            for (byte i = 0; i < Environment.ProcessorCount; ++i)
            {
                result[i] = i;
            }

            return result;
        }
    }

    public class NeuronThread
    {
        [DllImport("kernel32")]
        static extern int GetCurrentThreadId();

        static List<NeuronThread> ThreadsList = new List<NeuronThread>();
        static int _counter;

        public static NeuronThread GetThread()
        {
            NeuronThread thread;
            lock (ThreadsList)
            {
                thread = ThreadsList.FirstOrDefault();
                if (thread != null)
                {
                    ThreadsList.Remove(thread);
                }
            }

            if (thread == null)
            {
                thread = new NeuronThread();
            }

            return thread;
        }

        public static void  ReleaseThread(NeuronThread thread)
        {
            lock (ThreadsList)
            {
                ThreadsList.Add(thread);
            }
        }

        public static void Exit()
        {
            foreach (var thread in ThreadsList)
            {
                thread.ExitThread();
            }
        }

        public static ManualResetEvent IsFinished = new ManualResetEvent(false);

        AutoResetEvent WaitForData = new AutoResetEvent(false);
        SafeCounter Counter;
        Thread TheThread;
        ThreadStart Start;
        ListX<NeuronDataModel> Neurons;
        NeuronDataModel NextNeuron;
        bool ForBias;

        public NeuronThread()
        {
            Start = new ThreadStart(Run);
            TheThread = new Thread(Start);
            TheThread.Priority = ThreadPriority.Highest;
            TheThread.Start();
        }

        public void Run(SafeCounter counter, ListX<NeuronDataModel> neurons, NeuronDataModel nextNeuron, bool forBias)
        {
            Counter = counter;
            Neurons = neurons;
            NextNeuron = nextNeuron;
            ForBias = forBias;
            WaitForData.Set();
        }

        public void ExitThread()
        {
            Run(null, null, null, false);
        }

        private void Run()
        {
            foreach (ProcessThread pt in Process.GetCurrentProcess().Threads)
            {
                int utid = GetCurrentThreadId();
                if (utid == pt.Id)
                {
                    pt.ProcessorAffinity = (IntPtr)(1 + ++_counter % Environment.ProcessorCount);
                }
            }

            while (true)
            {
                WaitForData.WaitOne();
                //WaitForData.Reset();

                if (NextNeuron == null)
                {
                    return;
                }

                if (ForBias)
                {
                    NextNeuron.Activation = NextNeuron.ActivationFunction.Do(Neurons.Sum(bias => bias.IsBias ? bias.AxW(NextNeuron) : 0), NextNeuron.ActivationFuncParamA);
                }
                else
                {
                    NextNeuron.Activation = NextNeuron.ActivationFunction.Do(Neurons.Sum(neuron => neuron.Activation == 0 ? 0 : neuron.AxW(NextNeuron)), NextNeuron.ActivationFuncParamA);
                }
                
                if (Counter.Add(-1) <= 0)
                {
                    IsFinished.Set();
                }
                ReleaseThread(this);
            }
        }
    }

    public class SafeCounter
    {
        readonly object locker;
        double val;

        public SafeCounter(double v = 0)
        {
            locker = new object();
            val = 0;
            Add(v);
        }

        public double Add(double v)
        {
            lock (locker)
            {
                val += v;
                return val;
            }
        }
    }
}

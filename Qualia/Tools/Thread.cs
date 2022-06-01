using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Tools
{
    public static class Threads
    {
        [DllImport("kernel32")]
        private static extern int GetCurrentThreadId();

        public enum Processor
        {
            None = 0,
            Proc0 = 1,
            Proc1 = 2,
            Proc2 = 4,
            Proc3 = 8,
            Proc4 = 16,
            Proc5 = 32,
            Proc6 = 64,
            Proc7 = 128,
            Proc8 = 254,
            Proc9 = 512,
            Proc10 = 1024,
            Proc11 = 2048,
            Proc12 = 4096,
            Proc13 = Proc12 * 2,
            Proc14 = Proc13 * 2,
            Proc15 = Proc14 * 2,

            All = Proc0 | Proc1 | Proc2 | Proc3 | Proc4 | Proc5 | Proc6 | Proc7 | Proc8 | Proc9 | Proc10 | Proc11 | Proc12 | Proc13 | Proc14 | Proc15
        }

        public static void SetProcessorAffinity(Processor processors)
        {
            foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
            {
                if (GetCurrentThreadId() == thread.Id)
                {
                    thread.ProcessorAffinity = (IntPtr)processors;

                    return;
                }
            }
        }

        public static void SetThreadPriority(ThreadPriorityLevel priority)
        {
            foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
            {
                if (GetCurrentThreadId() == thread.Id)
                {
                    thread.PriorityLevel = priority;
                    thread.PriorityBoostEnabled = true;
                    
                    return;
                }
            }
        }
    }
}

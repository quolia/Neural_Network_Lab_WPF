using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Tools
{
    public static class Threads
    {
        [DllImport("kernel32")]
        static extern int GetCurrentThreadId();

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

            All = Proc0 | Proc1 | Proc2 | Proc3 | Proc4 | Proc5 | Proc6 | Proc7
        }

        public static void SetProcessorAffinity(Processor processors)
        {
            foreach (ProcessThread pt in Process.GetCurrentProcess().Threads)
            {
                int utid = GetCurrentThreadId();
                if (utid == pt.Id)
                {
                    pt.ProcessorAffinity = (IntPtr)processors;
                }
            }
        }

        public static void SetThreadPriority(ThreadPriorityLevel priority)
        {
            foreach (ProcessThread pt in Process.GetCurrentProcess().Threads)
            {
                int utid = GetCurrentThreadId();
                if (utid == pt.Id)
                {
                    pt.PriorityLevel = priority;
                    pt.PriorityBoostEnabled = true;
                }
            }
        }
    }
}

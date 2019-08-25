using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Tools.Threads;

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

            GUI = Proc7 * 2,

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
    }

    public class ManualSpinEvent : EventWaitHandle
    {
        readonly object locker = new object();
        int Spin;

        public ManualSpinEvent(bool initialState = true)
            : base(initialState, EventResetMode.ManualReset)
        {
            //
        }

        public new void Reset()
        {
            lock (locker)
            {
                if (++Spin == 1)
                {
                    base.Reset();
                }
            }
        }

        public new void Set()
        {
            lock (locker)
            {
                if (--Spin == 0)
                {
                    base.Set();
                }   
            }
        }

        public new void WaitOne()
        {
            base.WaitOne();
        }
    }
    public class Locker : IDisposable
    {
        public readonly bool IsActionAllowed;
        public readonly Processor Processor;

        readonly ManualSpinEvent Event;

        public Locker(ManualSpinEvent ev, Processor processor, bool isActionAllowed)
        {
            IsActionAllowed = isActionAllowed;
            Processor = processor;
            Event = ev;
            Event.Reset();
        }
        public void Dispose()
        {
            Event.Set();
        }
    }
    public class SharedLock
    {
        readonly ManualSpinEvent GUILocker = new ManualSpinEvent();
        readonly ManualSpinEvent CPULocker = new ManualSpinEvent();

        public Locker GetLocker(Processor processor, Processor allowedOnProcessors = Processor.All)
        {
            if (allowedOnProcessors == Processor.All || allowedOnProcessors.HasFlag(processor))
            {
                if (processor == Processor.GUI)
                {
                    CPULocker.WaitOne();
                    return new Locker(GUILocker, processor, true);
                }

                GUILocker.WaitOne();
                return new Locker(CPULocker, processor, true);
            }
            else
            {
                GUILocker.WaitOne();
                return new Locker(CPULocker, processor, false);
            }
        }
    }
}

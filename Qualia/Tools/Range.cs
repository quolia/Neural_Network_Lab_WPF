using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    public static class Range
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void For(int range, Action<int> action)
        {
            for (int i = 0; i < range; ++i)
            {
                action(i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ForEach<T>(IEnumerable<T> range, Action<T> action)
        {
            foreach (T t in range)
            {
                action(t);
            }
        }
    }
}

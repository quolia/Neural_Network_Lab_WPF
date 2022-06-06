using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Tools
{
    public static class Range
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<int> Make(int from, int to = 0)
        {
            return from < to ? Enumerable.Range(from, to - from) : Enumerable.Range(to, from - to);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void For(int range, Action<int> action)
        {
            for (int ind = 0; ind < range; ++ind)
            {
                action(ind);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BackEach<T>(IEnumerable<T> range, Action<T> action)
        {
            range = range.Reverse();
            foreach (T t in range)
            {
                action(t);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BackEachTrimEnd<T>(IEnumerable<T> range, int trim, Action<T> action)
        {
            long ind = 0;
            range = range.Reverse();
            long count = range.Count();

            foreach (T t in range)
            {
                if (ind == count + trim)
                {
                    break;
                }

                action(t);
                ++ind;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ForEachTrimEnd<T>(IEnumerable<T> range, int trim, Action<T> action)
        {
            long ind = 0;
            long count = range.Count();

            foreach (T t in range)
            {
                if (ind == count + trim)
                {
                    break;
                }

                action(t);
                ++ind;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Back(int range, Action<int> action)
        {
            for (int ind = range - 1; ind >= 0; --ind)
            {
                action(ind);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void For(int range1, int range2, Action<int, int> action)
        {
            For(range1, ind1 => For(range2, ind2 => action(ind1, ind2)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ForEach<T1, T2>(IEnumerable<T1> range1, IEnumerable<T2> range2, Action<T1, T2> action)
        {
            ForEach(range1, t1 => ForEach(range2, t2 => action(t1, t2)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Sum(int range, Func<int, double> func)
        {
            double sum = 0;
            For(range, ind => sum += func(ind));

            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SumForEach<T>(IEnumerable<T> range, Func<T, double> func)
        {
            double sum = 0;
            ForEach(range, t => sum += func(t));

            return sum;
        }
    }
}

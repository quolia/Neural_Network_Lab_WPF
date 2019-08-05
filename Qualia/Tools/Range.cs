using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools
{
    public static class Range
    {
        public static IEnumerable<int> Make(int a, int b = 0)
        {
            return a < b ? Enumerable.Range(a, b - a) : Enumerable.Range(b, a - b);
        }

        public static void For(int range, Action<int> action)
        {
            for (int y = 0; y < range; ++y)
                action(y);
        }

        public static void ForEach<T>(IEnumerable<T> range, Action<T> action)
        {
            foreach (T y in range)
                action(y);
        }

        public static void BackEach<T>(IEnumerable<T> range, Action<T> action)
        {
            range = range.Reverse();
            foreach (T y in range)
                action(y);
        }

        public static void BackEachTrimEnd<T>(IEnumerable<T> range, int trim, Action<T> action)
        {
            long n = 0;
            range = range.Reverse();
            long count = range.Count();
            foreach (T y in range)
            {
                if (n == count + trim)
                {
                    break;
                }

                action(y);
                ++n;
            }
        }

        public static void ForEachTrimEnd<T>(IEnumerable<T> range, int trim, Action<T> action)
        {
            long n = 0;
            long count = range.Count();
            foreach (T y in range)
            {
                if (n == count + trim)
                {
                    break;
                }

                action(y);
                ++n;
            }
        }

        public static void Back(int range, Action<int> action)
        {
            for (int y = range - 1; y >= 0; --y)
                action(y);
        }

        public static void For(int range1, int range2, Action<int, int> action)
        {
            For(range1, y1 => For(range2, y2 => action(y1, y2)));
        }

        public static void ForEach<T1, T2>(IEnumerable<T1> range1, IEnumerable<T2> range2, Action<T1, T2> action)
        {
            ForEach(range1, y1 => ForEach(range2, y2 => action(y1, y2)));
        }

        public static double Sum(int range, Func<int, double> func)
        {
            double s = 0;
            For(range, x => s += func(x));
            return s;
        }

        public static double SumForEach<T>(IEnumerable<T> range, Func<T, double> func)
        {
            double s = 0;
            ForEach(range, x => s += func(x));
            return s;
        }
    }
}

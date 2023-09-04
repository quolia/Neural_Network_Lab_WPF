using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Qualia.Tools
{
    public static class Rand
    {
        public static readonly Random RandomFlat = new((int)(DateTime.UtcNow.Ticks % int.MaxValue));

        sealed public class Gauss
        {
            private static bool _hasDeviate;
            private static double _storedDeviate;

            /// <summary>
            /// Obtains normally (Gaussian) distributed random numbers, using the Box-Muller
            /// transformation.  This transformation takes two uniformly distributed deviates
            /// within the unit circle, and transforms them into two independently
            /// distributed normal deviates.
            /// </summary>
            /// <param name="meanValue">The mean of the distribution.</param>
            /// <param name="sigma">The standard deviation of the distribution.</param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double GetNormal(double meanValue, double sigma)
            {
                if (sigma <= 0)
                {
                    throw new ArgumentOutOfRangeException("sigma", "Must be greater than zero.");
                }

                if (_hasDeviate)
                {
                    _hasDeviate = false;
                    return _storedDeviate * sigma + meanValue;
                }

                double v1, v2, squared;
                do
                {
                    v1 = 2 * RandomFlat.NextDouble() - 1;
                    v2 = 2 * RandomFlat.NextDouble() - 1;
                    squared = v1 * v1 + v2 * v2;
                }
                while (squared >= 1 || squared == 0);

                var polar = Math.Sqrt(-2 * Math.Log(squared) / squared);

                _storedDeviate = v2 * polar;
                _hasDeviate = true;

                return v1 * polar * sigma + meanValue;
            }
        }
    }

    public static class UniqId
    {
        private static long s_prevId;

        public static long GetNextId(long existingId)
        {
            if (existingId > Constants.UnknownId)
            {
                return existingId;
            }

            long id;
            do
            {
                id = DateTime.UtcNow.Ticks;
                Thread.Sleep(0);
            }
            while (id <= s_prevId);

            s_prevId = id;
            return id;
        }
    }
}

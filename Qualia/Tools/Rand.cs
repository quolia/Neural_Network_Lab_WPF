using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Qualia.Tools
{
    public static class Rand
    {
        public static readonly Random RandomFlat = new((int)(DateTime.UtcNow.Ticks % int.MaxValue));

        public static class Flat
        {
            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            //public static double Get() => RandomFlat.NextDouble();

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            //public static double Get(double maxValue) => maxValue * RandomFlat.NextDouble();

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            //public static double GetInRange(double minValue, double maxValue) => RandomFlat.NextDouble() * (maxValue - minValue) + minValue;

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            //public static double GetFromCenter(double range) => RandomFlat.NextDouble() * range - range / 2;
        }

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

    /*
    public static class RandomExtensions
    {
        /// <summary>
        ///   Generates normally distributed numbers. Each operation makes two Gaussians for the price of one, and apparently they can be cached or something for better performance, but who cares.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GaussNormal(this Random rand, double meanValue, double sigma)
        {
            var u1 = rand.NextDouble();
            var u2 = rand.NextDouble();

            var rand_std_normal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                  Math.Sin(2.0 * Math.PI * u2);

            var rand_normal = meanValue + sigma * rand_std_normal;

            return rand_normal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Triangular(this Random rand, double min, double max, double mostFrequentValue)
        {
            var u = rand.NextDouble();

            return u < (mostFrequentValue - min) / (max - min)
                       ? min + Math.Sqrt(u * (max - min) * (mostFrequentValue - min))
                       : max - Math.Sqrt((1 - u) * (max - min) * (max - mostFrequentValue));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Shuffle(this Random rand, IList list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var j = rand.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    public class Gauss
    {
        private bool _available;
        private double _nextGauss;
        private Random _rand;

        public Gauss()
        {
            _rand = new Random();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Next()
        {
            if (_available)
            {
                _available = false;
                return _nextGauss;
            }

            double temp1 = Math.Sqrt(-2.0 * Math.Log(_rand.NextDouble()));
            double temp2 = 2.0 * Math.PI * _rand.NextDouble();

            _nextGauss = temp1 * Math.Sin(temp2);
            _available = true;
            return temp1 * Math.Cos(temp2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Next(double meanValue, double sigma)
        {
            return meanValue + sigma * Next();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Next(double sigma)
        {
            return sigma * Next();
        }
    }
    */
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools
{
    public static class Rand
    {
        public static Random Flat = new Random((int)(DateTime.Now.Ticks % int.MaxValue));
        public static GaussianRandom GaussianRand = new GaussianRandom(Flat);

        public static double GetFlatRandom(double upperBound = 1)
        {
            return upperBound * Flat.NextDouble();
        }

        public static double GetSpreadRandom(double lowerBound, double upperBound)
        {
            return -lowerBound + upperBound * Flat.NextDouble();
        }

        public static double GetSpreadInRange(double range)
        {
            return -range / 2 + range * Flat.NextDouble();
        }
    }

    //
    // Taken from Stackoverflow
    //
    public sealed class GaussianRandom
    {
        private bool HasDeviate;
        private double StoredDeviate;
        private readonly Random Random;

        public GaussianRandom(Random random = null)
        {
            Random = random ?? new Random((int)(DateTime.Now.Ticks % int.MaxValue));
        }

        /// <summary>
        /// Obtains normally (Gaussian) distributed random numbers, using the Box-Muller
        /// transformation.  This transformation takes two uniformly distributed deviates
        /// within the unit circle, and transforms them into two independently
        /// distributed normal deviates.
        /// </summary>
        /// <param name="mu">The mean of the distribution.  Default is zero.</param>
        /// <param name="sigma">The standard deviation of the distribution.  Default is one.</param>
        /// <returns></returns>
        public double NextGaussian(double mu = 0, double sigma = 1)
        {
            if (sigma <= 0)
                throw new ArgumentOutOfRangeException("sigma", "Must be greater than zero.");

            if (HasDeviate)
            {
                HasDeviate = false;
                return StoredDeviate * sigma + mu;
            }

            double v1, v2, squared;
            do
            {
                // two random values between -1.0 and 1.0
                v1 = 2 * Random.NextDouble() - 1;
                v2 = 2 * Random.NextDouble() - 1;
                squared = v1 * v1 + v2 * v2;
                // ensure within the unit circle
            } while (squared >= 1 || squared == 0);

            // calculate polar tranformation for each deviate
            var polar = Math.Sqrt(-2 * Math.Log(squared) / squared);
            // store first deviate
            StoredDeviate = v2 * polar;
            HasDeviate = true;
            // return second deviate
            return v1 * polar * sigma + mu;
        }
    }

    public static class Uniq
    {
        public static double[] GetUniq(double spread, long count)
        {
            var result = new double[count];

            double step = 1 / (double)count;
            double a = step;

            for (long c = 0; c < count; ++c)
            {
                result[c] = spread * a;
                a += step;
            }

            return result;
        }
    }
}

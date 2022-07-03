using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    public static class MathX
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Abs(double a)
        {
            return a < 0 ? -a : a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Ceiling(double a)
        {
            if (a == (int)a)
            {
                return a;
            }

            return Math.Ceiling(a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Max(double a, double b)
        {
            return a > b ? a : b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long Max(long a, long b)
        {
            return a > b ? a : b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float Min(float a, float b)
        {
            return a < b ? a : b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Min(double a, double b)
        {
            return a < b ? a : b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Round(double a)
        {
            return (int)Math.Round(a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Sign(double a)
        {
            return a < 0 ? -1 : 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Exp(double x)
        {
            return Math.Exp(x);
        }
    }
}

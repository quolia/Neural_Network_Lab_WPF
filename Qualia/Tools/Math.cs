using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Qualia.Tools;

public static class MathX
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Abs(double a)
    {
        return a < 0 ? -a : a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Ceiling(double a)
    {
        if (a == (int)a)
        {
            return a;
        }

        return Math.Ceiling(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Max(double a, double b)
    {
        return a > b ? a : b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Max(long a, long b)
    {
        return a > b ? a : b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Max(int a, int b)
    {
        return a > b ? a : b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Min(long a, long b)
    {
        return a < b ? a : b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Min(int a, int b)
    {
        return a < b ? a : b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Min(float a, float b)
    {
        return a < b ? a : b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Min(double a, double b)
    {
        return a < b ? a : b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Round(double a)
    {
        return (int)Math.Round(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Sign(double a)
    {
        return a < 0 ? -1 : 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Exp(double x)
    {
        return Math.Exp(x);
    }

    public static long Sum(IList<long> value)
    {
        throw new NotImplementedException();
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Tools
{
    public static class Rects
    {
        private const int POOL_SIZE = 50;
        private static int s_pointer = 0;
        private static readonly Dictionary<int, Rect> s_pool = new Dictionary<int, Rect>(POOL_SIZE);

        static Rects()
        {
            for (int ind = 0; ind < POOL_SIZE; ++ind)
            {
                s_pool[ind] = new Rect();
            }
        }

        public static Rect Get(Size size)
        {
            var rect = s_pool[s_pointer];

            rect.Size = size;

            if (++s_pointer == POOL_SIZE)
            {
                s_pointer = 0;
            }

            return rect;
        }

        public static Rect Get(double x, double y, double width, double height)
        {
            var rect = s_pool[s_pointer];

            rect.X = x;
            rect.Y = y;
            rect.Width = width;
            rect.Height = height;

            if (++s_pointer == POOL_SIZE)
            {
                s_pointer = 0;
            }

            return rect;
        }
    }

    public static class Points
    {
        private const int POOL_SIZE = 50;
        private static int s_pointer = 0;
        private static readonly Dictionary<int, Point> s_pool = new Dictionary<int, Point>(POOL_SIZE);

        static Points()
        {
            for (int ind = 0; ind < POOL_SIZE; ++ind)
            {
                s_pool[ind] = new Point();
            }
        }

        public static Point Get(double x, double y)
        {
            var point = s_pool[s_pointer];

            point.X = x;
            point.Y = y;

            if (++s_pointer == POOL_SIZE)
            {
                s_pointer = 0;
            }

            return point;
        }
    }

    public static class Render
    {
        private static readonly double s_halfPixelSize;

        static Render()
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            var dpiProperty = typeof(SystemParameters).GetProperty("Dpi", flags);

            Dpi = (int)dpiProperty.GetValue(null, null);
            PixelSize = 96.0 / Dpi;
            PixelsPerDip = Dpi / 96.0;
            s_halfPixelSize = PixelSize / 2;
        }

        public static double PixelSize { get; private set; }

        public static int Dpi { get; private set; }

        public static double PixelsPerDip { get; private set; }

        public static double SnapToPixels(double value)
        { 
            value += s_halfPixelSize;
            var div = (value * 1000) / (PixelSize * 1000);

            return (int)div * PixelSize;
        }

        public static double Scale(double x)
        {
            return x;// x / PixelsPerDip;// x;//SnapToPixels(x);
        }

        public static double ScaleThickness(double x)
        {
            return SnapToPixels(x);
        }
    }

    public static class Draw
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Drawing.Color MediaColorToSystemColor(Color wpfColor)
        {
            return System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B); ;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color SystemColorToMediaColor(System.Drawing.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color GetColor(byte alpha, Color color)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color GetColor(byte r, byte g, byte b)
        {
            return Color.FromRgb(r, g, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color GetColor(double v, byte alpha = 255)
        {
            int s = Math.Sign(v);

            v = Math.Abs(2 / (1 + Math.Exp(-v * 4)) - 1);

            if (v > 1)
            {
                v = 1;
            }

            if (s >= 0)
            {
                return Color.FromArgb(alpha, (byte)(255 * v), (byte)(50 * v), (byte)(50 * v));
            }
            else
            {
                return Color.FromArgb(alpha, (byte)(50 * v), (byte)(50 * v), (byte)(255 * v));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color GetColorDradient(Color fromColor, Color toColor, byte alpha, double fraction)
        {
            if (fraction > 1)
            {
                fraction = 1;
            }
            else if (fraction < 0)
            {
                fraction = 0;
            }

            return Color.FromArgb(alpha,
                                  (byte)(fromColor.R - fraction * (fromColor.R - toColor.R)),
                                  (byte)(fromColor.G - fraction * (fromColor.G - toColor.G)),
                                  (byte)(fromColor.B - fraction * (fromColor.B - toColor.B)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color GetColorDradient(Color fromColor, Color zeroColor, Color toColor, byte alpha, double fraction)
        {
            if (fraction < 0.5)
            {
                return GetColorDradient(fromColor, zeroColor, alpha, fraction * 2);
            }
            else
            {
                return GetColorDradient(zeroColor, toColor, alpha, 2 * (fraction - 0.5));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Brush GetBrush(double v, byte alpha = 255)
        {
            return GetBrush(GetColor(v, alpha));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Brush GetBrush(Color color)
        {
            return new SolidColorBrush(color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pen GetPen(double v, double width = 1, byte alpha = 255)
        {
            width *= Render.PixelSize;

            if (width == 0)
            {
                width = Math.Abs(v) <= 1 ? 1 : (double)Math.Abs(v);
                alpha = alpha == 255 ? (byte)(alpha / (1 + (width - 1) / 2)) : alpha;
            }

            return new Pen(GetBrush(v, alpha), Render.ScaleThickness(width));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pen GetPen(Color color, double width = 1)
        {
            width *= Render.PixelSize;
            return new Pen(GetBrush(color), Render.ScaleThickness(width));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color GetRandomColor(byte offsetFromTop, Color? baseColor = null)
        {
            if (baseColor == null)
            {
                baseColor = Colors.White;
            }

            var rand = (byte)Rand.Flat.Next(offsetFromTop);
            return Color.FromArgb(255,
                                  (byte)(Math.Max(baseColor.Value.R - offsetFromTop, 0) + rand),
                                  (byte)(Math.Max(baseColor.Value.G - offsetFromTop, 0) + rand),
                                  (byte)(Math.Max(baseColor.Value.B - offsetFromTop, 0) + rand));
        }
    }
}

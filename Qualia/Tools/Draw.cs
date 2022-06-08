using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Tools
{
    public static class Rects
    {
        //private const int POOL_SIZE = 50;
        //private static int s_pointer = 0;
        //private static readonly Rect[] s_pool = new Rect[POOL_SIZE];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect Get(in Size size)
        {
            return new Rect(size);
            /*
            var rect = s_pool[s_pointer];

            rect.Size = size;

            if (++s_pointer == POOL_SIZE)
            {
                s_pointer = 0;
            }

            return rect;
            */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect Get(double x, double y, double width, double height)
        {
            return new Rect(x, y, width, height);
            /*
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
            */
        }
    }

    public static class Points
    {
        //private const int POOL_SIZE = 50;
        //private static int s_pointer = 0;
        //private static readonly Point[] s_pool = new Point[POOL_SIZE];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point Get(double x, double y)
        {
            return new Point(x, y);
            /*
            ref var point = ref s_pool[s_pointer];

            point.X = x;
            point.Y = y;

            if (++s_pointer == POOL_SIZE)
            {
                s_pointer = 0;
            }

            return ref point;
            */
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Scale(double x)
        {
            return x;// x / PixelsPerDip;// x;//SnapToPixels(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ScaleThickness(double x)
        {
            return SnapToPixels(x);
        }
    }

    public static class Draw
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Drawing.Color MediaColorToSystemColor(in Color wpfColor)
        {
            return System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B); ;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color SystemColorToMediaColor(in System.Drawing.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color GetColor(byte alpha, in Color color)
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
        public static Color GetColorDradient(in Color fromColor, in Color toColor, byte alpha, double fraction)
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
        public static Color GetColorDradient(in Color fromColor, in Color zeroColor, in Color toColor, byte alpha, double fraction)
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
        public static Brush GetBrush(in Color color)
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

            var pen = new Pen(GetBrush(v, alpha), Render.ScaleThickness(width));
            //pen.Freeze();
            return pen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pen GetPen(in Color color, double width = 1)
        {
            width *= Render.PixelSize;

            var pen = new Pen(GetBrush(color), Render.ScaleThickness(width));
            //pen.Freeze();
            return pen;
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

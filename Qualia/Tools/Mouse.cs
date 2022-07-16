using System;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace Qualia.Tools
{
    public class MousePosition : DependencyObject
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(ref NativePoint pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativePoint
        {
            public int X;
            public int Y;
        };

        public static Point GetCurrentMousePosition()
        {
            NativePoint nativePoint = new NativePoint();
            GetCursorPos(ref nativePoint);
            return new Point(nativePoint.X, nativePoint.Y);
        }

        private Dispatcher dispatcher;

        Timer timer = new Timer(100);

        public MousePosition()
        {
            dispatcher = Application.Current.MainWindow.Dispatcher;
            timer.Elapsed += timer_Elapsed;
            timer.Start();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Point current = GetCurrentMousePosition();
            this.CurrentPosition = current;
        }

        public Point CurrentPosition
        {
            get { return (Point)GetValue(CurrentPositionProperty); }

            set
            {
                dispatcher.Invoke((Action)(() =>
                  SetValue(CurrentPositionProperty, value)));
            }
        }

        public static readonly DependencyProperty CurrentPositionProperty
          = DependencyProperty.Register(
            "CurrentPosition", typeof(Point), typeof(MousePosition));
    }

}

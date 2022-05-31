using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Tools;

namespace Qualia.Controls
{
    public class WindowResizeControl : Window
    {
        public WindowResizeControl()
        {
            SourceInitialized += Window_SourceInitialized;
        }

        bool? _adjustingHeight = null;

        internal enum SWP
        {
            NOMOVE = 0x0002
        }

        internal enum WM
        {
            WINDOWPOSCHANGING = 0x0046,
            EXITSIZEMOVE = 0x0232,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return Points.Get(w32Mouse.X, w32Mouse.Y);
        }

        public virtual void OnResizeEnd()
        {
            //
        }

        private void Window_SourceInitialized(object sender, EventArgs ea)
        {
            HwndSource hwndSource = (HwndSource)HwndSource.FromVisual((Window)sender);
            hwndSource.AddHook(DragHook);
        }

        private IntPtr DragHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((WM)msg)
            {
                case WM.WINDOWPOSCHANGING:
                    {
                        WINDOWPOS pos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                        if ((pos.flags & (int)SWP.NOMOVE) != 0)
                            return IntPtr.Zero;

                        Window wnd = (Window)HwndSource.FromHwnd(hwnd).RootVisual;
                        if (wnd == null)
                        {
                            return IntPtr.Zero;
                        }

                        if (!_adjustingHeight.HasValue)
                        {
                            Point p = GetMousePosition();

                            double diffWidth = Math.Min(Math.Abs(p.X - pos.x), Math.Abs(p.X - pos.x - pos.cx));
                            double diffHeight = Math.Min(Math.Abs(p.Y - pos.y), Math.Abs(p.Y - pos.y - pos.cy));

                            _adjustingHeight = diffHeight > diffWidth;
                        }

                        if (_adjustingHeight.Value)
                            pos.cy = (int)(9 * pos.cx / 16);
                        else
                            pos.cx = (int)(16 * pos.cy / 9);

                        Marshal.StructureToPtr(pos, lParam, true);
                        handled = true;
                    }
                    break;

                case WM.EXITSIZEMOVE:
                    _adjustingHeight = null;
                    OnResizeEnd();
                    break;
            }

            return IntPtr.Zero;
        }
    }
}

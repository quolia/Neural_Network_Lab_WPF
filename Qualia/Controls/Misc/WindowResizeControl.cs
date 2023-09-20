using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Qualia.Tools;

namespace Qualia.Controls.Misc;

public class WindowResizeControl : Window
{
    private bool? _adjustingHeight = null;

    public WindowResizeControl()
    {
        SourceInitialized += Window_OnSourceInitialized;
    }

    private enum SWP
    {
        NOMOVE = 0x0002
    }

    private enum WM
    {
        WINDOWPOSCHANGING = 0x0046,
        EXITSIZEMOVE = 0x0232,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPOS
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
    private static extern bool GetCursorPos(ref Win32Point pt);

    [StructLayout(LayoutKind.Sequential)]
    private struct Win32Point
    {
        public Int32 X;
        public Int32 Y;
    };

    public static Point GetMousePosition()
    {
        Win32Point w32Mouse = new();
        GetCursorPos(ref w32Mouse);

        return new(w32Mouse.X, w32Mouse.Y);
    }

    public virtual void OnResizeEnd()
    {
        //
    }

    private void Window_OnSourceInitialized(object sender, EventArgs ea)
    {
        var hwndSource = (HwndSource)HwndSource.FromVisual((Window)sender);
        hwndSource.AddHook(DragHook);
    }

    private IntPtr DragHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch ((WM)msg)
        {
            case WM.WINDOWPOSCHANGING:
            {
                var pos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                if ((pos.flags & (int)SWP.NOMOVE) != 0)
                {
                    return IntPtr.Zero;
                }

                var wnd = (Window)HwndSource.FromHwnd(hwnd).RootVisual;
                if (wnd == null)
                {
                    return IntPtr.Zero;
                }

                if (!_adjustingHeight.HasValue)
                {
                    var p = GetMousePosition();

                    var diffWidth = MathX.Min(MathX.Abs(p.X - pos.x), MathX.Abs(p.X - pos.x - pos.cx));
                    var diffHeight = MathX.Min(MathX.Abs(p.Y - pos.y), MathX.Abs(p.Y - pos.y - pos.cy));

                    _adjustingHeight = diffHeight > diffWidth;
                }

                if (_adjustingHeight.Value)
                {
                    pos.cy = (int)(9 * pos.cx / 16);
                }
                else
                {
                    pos.cx = (int)(16 * pos.cy / 9);
                }

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
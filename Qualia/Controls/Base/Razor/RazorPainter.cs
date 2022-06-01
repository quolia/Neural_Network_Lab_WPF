using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Qualia.Controls
{
    public class RazorPainter : IDisposable
    {
        [DllImport("gdi32")]
        private extern static int SetDIBitsToDevice(HandleRef hDC, int xDest, int yDest, int dwWidth, int dwHeight, int XSrc, int YSrc, int uStartScan, int cScanLines, ref int lpvBits, ref BITMAPINFO lpbmi, uint fuColorUse);

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public int bihSize;
            public int bihWidth;
            public int bihHeight;
            public short bihPlanes;
            public short bihBitCount;
            public int bihCompression;
            public int bihSizeImage;
            public double bihXPelsPerMeter;
            public double bihClrUsed;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER BIHeader;
            public int BIColors;
        }

        private int _width;
        private int _height;
        private int[] _array;
        private GCHandle _gcHandle;
        private BITMAPINFO _BI;

        public int Width => _width;

        public int Height => _height;

        ~RazorPainter()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }

            GC.SuppressFinalize(this);
        }

        private void Realloc(int width, int height)
        {
            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }

            _width = width;
            _height = height;

            _array = new int[_width * _height];
            _gcHandle = GCHandle.Alloc(_array, GCHandleType.Pinned);

            _BI = new BITMAPINFO
            {
                BIHeader =
                {
                    bihBitCount = 32,
                    bihPlanes = 1,
                    bihSize = 40,
                    bihWidth = _width,
                    bihHeight = -_height,
                    bihSizeImage = (_width * _height) << 2
                }
            };
        }

        public void Paint(HandleRef hRef, Bitmap bitmap)
        {
            if (bitmap == null || bitmap.Width == 0 || bitmap.Height == 0)
            {
                Console.WriteLine("Invalid bitmap in Paint");
                return;
            }

            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                Console.WriteLine("PixelFormat must be Format32bppArgb at Paint() in RazorPainter");
                return;
            }

            if (bitmap.Width != _width || bitmap.Height != _height)
            {
                Realloc(bitmap.Width, bitmap.Height);
            }

            BitmapData bmData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                                ImageLockMode.ReadOnly,
                                                PixelFormat.Format32bppArgb);

            Marshal.Copy(bmData.Scan0, _array, 0, _width * _height);
            SetDIBitsToDevice(hRef, 0, 0, _width, _height, 0, 0, 0, _height, ref _array[0], ref _BI, 0);
            bitmap.UnlockBits(bmData);
        }
    }
}

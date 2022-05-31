// Test control fronend for WPF for RazorGDIPainter library
//   (c) Mokrov Ivan
// special for habrahabr.ru
// under MIT license
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms.Integration;

namespace Qualia.Controls
{
    public partial class RazorPainterWPFCtl : WindowsFormsHost
    {
        private readonly HandleRef _hDCRef;
        private readonly Graphics _hDCGraphics;
        private readonly RazorPainter _RP;

        public Bitmap RazorBMP { get; private set; }

        /// <summary>
        /// Graphics object to paint on RazorBMP
        /// </summary>
        public Graphics RazorGFX { get; private set; }

        /// <summary>
        /// Real per-pixel width of backend Win32 control, w/o DPI resizes of WPF layout
        /// </summary>
        public int RazorWidth => RazorBackend1.Width;

        /// <summary>
        /// Real per-pixel height of backend Win32 control, w/o DPI resizes of WPF layout
        /// </summary>
        public int RazorHeight => RazorBackend1.Height;

        /// <summary>
        /// Lock it to avoid resize/repaint race
        /// </summary>
        public readonly object RazorLock = new object();

        public RazorPainterWPFCtl()
        {
            InitializeComponent();

            _hDCGraphics = RazorBackend1.CreateGraphics();
            _hDCRef = new HandleRef(_hDCGraphics, _hDCGraphics.GetHdc());
            _RP = new RazorPainter();

            RazorBMP = new Bitmap(RazorWidth, RazorHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            RazorGFX = Graphics.FromImage(RazorBMP);

            RazorBackend1.Resize += (sender, args) =>
            {
                lock (RazorLock)
                {
                    if (RazorGFX != null)
                    {
                        RazorGFX.Dispose();
                        RazorGFX = null;
                    }

                    if (RazorBMP != null)
                    {
                        RazorBMP.Dispose();
                        RazorBMP = null;
                    }

                    RazorBMP = new Bitmap(RazorWidth, RazorWidth, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    RazorGFX = Graphics.FromImage(RazorBMP);
                }
            };
        }

        /// <summary>
        /// After all in-memory paint on RazorGFX, call it to display it on control
        /// </summary>
        public void RazorPaint()
        {
            _RP.Paint(_hDCRef, RazorBMP);
        }

        protected override void Dispose(bool disposing)
        {
            lock (this)
            {
                if (RazorGFX != null)
                {
                    RazorGFX.Dispose();
                    RazorGFX = null;
                }

                if (RazorBMP != null)
                {
                    RazorBMP.Dispose();
                    RazorBMP = null;
                }

                if (_hDCGraphics != null)
                {
                    _hDCGraphics.Dispose();
                }
                
                _RP.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

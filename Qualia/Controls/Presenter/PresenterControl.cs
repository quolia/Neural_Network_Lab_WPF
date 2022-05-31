using System;
using System.Drawing;
using System.Windows.Forms;

namespace Qualia.Controls
{
    public partial class PresenterControl : UserControl
    {
        public Graphics G;

        private Bitmap _drawArea;
        private bool _isRenderNeeded = true;

        public PresenterControl()
        {
            InitializeComponent();

            SizeChanged += DrawBox_SizeChanged;
            BackColor = Color.White;

            Disposed += OnDisposed;
        }

        private void OnDisposed(object sender, EventArgs e)
        {
            if (_drawArea != null)
            {
                _drawArea.Dispose();
                _drawArea = null;
            }

            if (G != null)
            {
                G.Dispose();
                G = null;
            }
        }

        private void DrawBox_SizeChanged(object sender, EventArgs e)
        {
            _isRenderNeeded = true;
        }

        public void StartRender()
        {
            if (_isRenderNeeded && Width > 0 && Height > 0)
            {
                _isRenderNeeded = false;

                if (G != null)
                {
                    _drawArea.Dispose();
                    G.Dispose();
                }
                _drawArea = new Bitmap(Width, Height);
                CtlBox.Image = _drawArea;
                G = Graphics.FromImage(_drawArea);
                //G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            }
        }

        public void Clear()
        {
            G.Clear(BackColor);
        }
    }
}

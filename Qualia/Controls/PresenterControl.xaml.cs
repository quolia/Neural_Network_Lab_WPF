using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Qualia.Controls
{
    public partial class PresenterControl : UserControl
    {
        bool IsRenderNeeded = true;

        public PresenterControl()
        {
            InitializeComponent();

            //Disposed += DrawBox_Disposed;
            //SizeChanged += DrawBox_SizeChanged;
            //BackColor = Color.White;

            //Disposed += PresenterControl_Disposed;
        }

        private void PresenterControl_Disposed(object sender, EventArgs e)
        {
            //if (DrawArea != null)
            {
            //    DrawArea.Dispose();
            }
        }

        private void DrawBox_SizeChanged(object sender, EventArgs e)
        {
            IsRenderNeeded = true;
        }

        public void StartRender()
        {
            if (IsRenderNeeded && Width > 0 && Height > 0)
            {
                IsRenderNeeded = false;

              //  if (G != null)
              //  {
              //      DrawArea.Dispose();
              //      G.Dispose();
              //  }


            
                
                
             //   DrawArea = new BitmapImage(Width, Height);
             //   CtlBox.Source = DrawArea;
//
             //   CtlBox.Image = DrawArea;
             //   G = Graphics.FromImage(DrawArea);
                //G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            }
        }

        public void Clear()
        {
            //G.Clear(BackColor);
        }

        private void DrawBox_Disposed(object sender, EventArgs e)
        {
            //if (G != null)
            {
               // G.Dispose();
            }
        }
    }
}

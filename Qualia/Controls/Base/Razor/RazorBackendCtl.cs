//   (c) Mokrov Ivan
// special for habrahabr.ru
// under MIT license

using System.Windows.Forms;

namespace Qualia.Controls
{
    public partial class RazorBackendCtl : UserControl
    {
        public RazorBackendCtl()
        {
            InitializeComponent();
            
            SetStyle(ControlStyles.DoubleBuffer, false);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Opaque, true);
            
            this.MinimumSize = new System.Drawing.Size(1, 1);
        }
    }
}

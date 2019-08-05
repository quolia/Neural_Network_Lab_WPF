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
using Tools;

namespace Qualia.Controls
{
    public partial class StatisticPresenter : UserControl
    {
        Typeface Font;

        public StatisticPresenter()
        {
            Font = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        }

        public void Draw(Dictionary<string, string> stat)
        {
            //StartRender();
            //Clear();

            if (stat == null)
            {
                //CtlBox.Invalidate();
                return;
            }

            //G.TextRenderingHint = TextRenderingHint.AntiAlias;

            double y = 0;
            foreach (var pair in stat)
            {
                var text = new FormattedText(pair.Key + ": " + pair.Value, Culture.Current, FlowDirection.LeftToRight, Font, 6.5f, Brushes.Black, 1.25);

                CtlPresenter.G.DrawText(text, new Point(10, y));

                y += Font.CapsHeight;
            };

            //CtlBox.Invalidate();
        }
    }
}

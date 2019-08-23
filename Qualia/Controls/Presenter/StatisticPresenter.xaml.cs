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
        Typeface Font = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        public StatisticPresenter()
        {
            InitializeComponent();
        }

        public void Draw(Dictionary<string, string> stat)
        {
            CtlPresenter.Clear();

            if (stat == null)
            {
                return;
            }

            string text = "";
            foreach (var pair in stat)
            {
                text += pair.Key + ": " + pair.Value + "\r\n";
            }

            var formattedText = new FormattedText(text, Culture.Current, FlowDirection.LeftToRight, Font, 10, Brushes.Black, Render.PixelsPerDip);
            CtlPresenter.DrawText(formattedText, Points.Get(10, 0));

            Width = Math.Max(ActualWidth, formattedText.WidthIncludingTrailingWhitespace + 10);

            CtlPresenter.Update();
        }
    }
}

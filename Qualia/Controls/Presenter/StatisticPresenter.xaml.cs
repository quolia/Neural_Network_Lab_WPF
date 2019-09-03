using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public partial class StatisticsPresenter : UserControl
    {
        Typeface Font = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        public StatisticsPresenter()
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

            string text = string.Empty;
            foreach (var pair in stat)
            {
                if (pair.Key.Length == 0)
                {
                    text += "\r\n";
                }
                else
                {
                    text += pair.Key + ": " + pair.Value + "\r\n";
                }
            }

            var formattedText = new FormattedText(text, Culture.Current, FlowDirection.LeftToRight, Font, 10, Brushes.Black, Render.PixelsPerDip);
            CtlPresenter.DrawText(formattedText, Points.Get(10, 0));

            Width = Math.Max(ActualWidth, formattedText.WidthIncludingTrailingWhitespace + 10);

            CtlPresenter.Update();
        }

        public void Clear()
        {
            Draw(null);
        }
    }
}

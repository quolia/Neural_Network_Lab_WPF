using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public partial class StatisticsPresenter : UserControl
    {
        private static readonly Typeface s_font = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private static readonly StringBuilder s_stringBuilder = new StringBuilder();

        public StatisticsPresenter()
        {
            InitializeComponent();
        }

        public void Draw(Dictionary<string, string> stats)
        {
            CtlPresenter.Clear();

            if (stats == null)
            {
                return;
            }

            s_stringBuilder.Clear();
            //string text = string.Empty;
            foreach (var pair in stats)
            {
                if (pair.Key.Length == 0)
                {
                    //text += "\r\n";
                    s_stringBuilder.AppendLine();
                }
                else
                {
                    //text += pair.Key + ": " + pair.Value + "\r\n";
                    s_stringBuilder.AppendLine($"{pair.Key}: {pair.Value}");
                }
            }

            var formattedText = new FormattedText(s_stringBuilder.ToString(), Culture.Current, FlowDirection.LeftToRight, s_font, 10, Brushes.Black, Render.PixelsPerDip);
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

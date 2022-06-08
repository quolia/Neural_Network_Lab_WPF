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
            foreach (var item in stats)
            {
                if (item.Key.Length == 0)
                {
                    s_stringBuilder.AppendLine();
                }
                else
                {
                    s_stringBuilder.AppendLine($"{item.Key}: {item.Value}");
                }
            }

            var formattedText = new FormattedText(s_stringBuilder.ToString(),
                                                  Culture.Current,
                                                  FlowDirection.LeftToRight,
                                                  s_font,
                                                  10,
                                                  Brushes.Black,
                                                  Render.PixelsPerDip);

            CtlPresenter.DrawText(formattedText, ref Points.Get(10, 0));

            Width = Math.Max(ActualWidth, formattedText.WidthIncludingTrailingWhitespace + 10);

            CtlPresenter.Update();
        }

        public void Clear()
        {
            Draw(null);
        }
    }
}

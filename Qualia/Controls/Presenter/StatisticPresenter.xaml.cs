using Qualia.Tools;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public partial class StatisticsPresenter : BaseUserControl
    {
        private static readonly Typeface s_font = new(new("Tahoma"),
                                                      FontStyles.Normal,
                                                      FontWeights.Bold,
                                                      FontStretches.Normal);

        private static readonly StringBuilder s_stringBuilder = new();

        public StatisticsPresenter()
        {
            InitializeComponent();
        }

        public void Draw(Dictionary<string, string> stats)
        {
            CtlCanvas.Clear();

            if (stats == null)
            {
                return;
            }

            s_stringBuilder.Clear();
            foreach (var item in stats)
            {
                if (item.Value == null)
                {
                    s_stringBuilder.AppendLine();
                    continue;
                }

                s_stringBuilder.AppendLine($"{item.Key}: {item.Value}");
            }

            FormattedText formattedText = new(s_stringBuilder.ToString(),
                                              Culture.Current,
                                              FlowDirection.LeftToRight,
                                              s_font,
                                              10,
                                              Brushes.Black,
                                              RenderSettings.PixelsPerDip);

            CtlCanvas.DrawText(formattedText, ref Points.Get(10, 0));

            Width = MathX.Max(ActualWidth, formattedText.WidthIncludingTrailingWhitespace + 10);
        }

        public void Clear()
        {
            Draw(null);
        }
    }
}

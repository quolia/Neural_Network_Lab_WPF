using Qualia.Tools;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using System;

namespace Qualia.Controls
{
    public partial class TaskSolutionsPresenter : BaseUserControl
    {
        public TaskSolutionsPresenter()
        {
            InitializeComponent();

            ShowSolutionsData(new SolutionsData(Array.Empty<Solution>().ToList()));
        }

        public void ShowSolutionsData(SolutionsData data)
        {
            var builder = new StringBuilder();

            /*
            var solutions = data.Solutions.OrderBy(s => s.ErrorsCount)
                                          .ThenBy(s => s.AverageTime)
                                          .ThenBy(s => s.MinTime)
                                          .ThenBy(s => s.LastTime)
                                          .ToList();
            */
            var solutions = data.Solutions.OrderBy(s => s.Rating)
                                          .ToList();

            builder.AppendLine(string.Format(Culture.Current,
                                             "{0, -12} {1} {2, 6} {3, 8}",
                                             "Function",
                                             "Execution time, mcsec",
                                             "Error",
                                             "Rating"));

            builder.AppendLine(string.Format(Culture.Current,
                                             "{0, 18} {1, 5} {2, 9} {3, 6}",
                                             "Min",
                                             "Last",
                                             "Average",
                                             "count"));
            builder.AppendLine();

            foreach (var solution in solutions)
            {
                if (solution.IsExcluded)
                {
                    continue;
                }

                builder.AppendLine(string.Format(Culture.Current,
                                                 "{0, -10} {1, 7} {2, 5} {3, 9} {4, 6} {5, 8}",
                                                 solution.Name,
                                                 solution.MinTime,
                                                 solution.LastTime,
                                                 solution.AverageTime,
                                                 solution.ErrorsCount,
                                                 solution.Rating));
            }

            builder.AppendLine();
            builder.AppendLine(string.Format(Culture.Current,
                                             "{0, -10} {1, 7} {2, 5} {3, 9} {4, 6}",
                                             "Sum",
                                             Converter.DoubleToText(solutions.Sum(s => Converter.TextToDouble(s.MinTime, 0)), "F2"),
                                             Converter.DoubleToText(solutions.Sum(s => Converter.TextToDouble(s.LastTime, 0)), "F2"),
                                             Converter.DoubleToText(solutions.Sum(s => Converter.TextToDouble(s.AverageTime, 0)), "F3"),
                                             Converter.IntToText(solutions.Sum(s => s.ErrorsCount))));

            CtlText.Text = builder.ToString();
            CtlText.Background = Brushes.Lavender;
        }
    }
}

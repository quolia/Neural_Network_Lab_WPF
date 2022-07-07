using Qualia.Tools;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;

namespace Qualia.Controls
{
    public partial class TaskSolutionsPresenter : BaseUserControl
    {
        public TaskSolutionsPresenter()
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlShowTaskSolutions
                    .Initialize(false)
            };

            CtlText.Text = "No data.";
        }

        public void ShowSolutionsData(SolutionsData data)
        {
            var builder = new StringBuilder();

            var solutions = data.Solutions.OrderBy(s => s.ErrorsCount)
                                          .ThenBy(s => s.AverageTime)
                                          .ThenBy(s => s.MinTime)
                                          .ThenBy(s => s.LastTime).ToList();

            builder.AppendLine(string.Format(Culture.Current,
                                             "{0, -12} {1} {2, 6}",
                                             "Function",
                                             "Execution time, mcsec",
                                             "Error"));

            builder.AppendLine(string.Format(Culture.Current,
                                             "{0, 18} {1, 5} {2, 9} {3, 6}",
                                             "Min",
                                             "Last",
                                             "Average",
                                             "count"));
            builder.AppendLine();

            foreach (var solution in solutions)
            {
                builder.AppendLine(string.Format(Culture.Current,
                                                 "{0} {1, 15} {2, 5} {3, 9} {4, 6}",
                                                 solution.Name,
                                                 solution.MinTime,
                                                 solution.LastTime,
                                                 solution.AverageTime,
                                                 solution.ErrorsCount));
            }

            CtlText.Text = builder.ToString();
            CtlText.Background = Brushes.Lavender;
        }
    }
}

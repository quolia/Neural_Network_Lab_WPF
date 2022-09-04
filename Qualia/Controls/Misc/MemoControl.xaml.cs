using Qualia.Tools;
using System.IO;

namespace Qualia.Controls
{
    public partial class MemoControl : BaseUserControl
    {
        public string Caption { get; set; } = "Caption";

        public string Text => CtlText.Text;

        public MemoControl()
            : base(0)
        {
            InitializeComponent();

            DataContext = this;

            if (File.Exists(FileHelper.NotesPath))
            {
                CtlText.Text = File.ReadAllText(FileHelper.NotesPath);
            }
        }

        public void Save(string fileName)
        {
            File.WriteAllText(FileHelper.NotesPath, Text);
        }
    }
}

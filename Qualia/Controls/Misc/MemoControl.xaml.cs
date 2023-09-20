using System.IO;
using Qualia.Controls.Base;
using Qualia.Tools;

namespace Qualia.Controls.Misc;

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

    public void Save()
    {
        File.WriteAllText(FileHelper.NotesPath, Text);
    }
}
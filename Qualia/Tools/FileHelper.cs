using System.IO;

namespace Qualia.Tools;

public static class FileHelper
{
    public static string MainConfigName = "config.txt";
    public static string NotesName = "notes.txt";

    public static string ConfigPath = App.WorkingDirectory + MainConfigName;
    public static string NotesPath = App.WorkingDirectory + NotesName;

    public static void InitWorkingDirectories()
    {
        var networksPath = App.WorkingDirectory + "Networks";

        if (!Directory.Exists(networksPath))
        {
            Directory.CreateDirectory(networksPath);
        }

        var datasetsPath = App.WorkingDirectory + "Datasets";

        if (!Directory.Exists(datasetsPath))
        {
            Directory.CreateDirectory(datasetsPath);
        }

        var mnistPath = datasetsPath + Path.DirectorySeparatorChar + "MNIST";

        if (!Directory.Exists(mnistPath))
        {
            Directory.CreateDirectory(mnistPath);
        }
    }
}
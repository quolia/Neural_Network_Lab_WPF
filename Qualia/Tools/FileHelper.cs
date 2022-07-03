using System.IO;

namespace Qualia.Tools
{
    public static class FileHelper
    {
        public static void InitWorkingDirectories()
        {
            var networksPath = App.WorkingDirectory + "Networks";

            if (!Directory.Exists(networksPath))
            {
                Directory.CreateDirectory(networksPath);
            }

            var mnistPath = App.WorkingDirectory + "MNIST";

            if (!Directory.Exists(mnistPath))
            {
                Directory.CreateDirectory(mnistPath);
            }
        }
    }
}

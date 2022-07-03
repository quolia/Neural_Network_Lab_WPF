using System;
using System.Reflection;

namespace Qualia.Tools
{
    internal static class VersionHelper
    {
        public static (string, string) GetVersion()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            DateTime buildDate = new DateTime(2000, 1, 1)
                                    .AddDays(version.Build).AddSeconds(version.Revision * 2);

            return ($"{version}", $"{buildDate.ToString("f", Culture.Current)}");
        }
    }
}

using System.Diagnostics;

namespace Folminder.Platform
{
    public static class ShellExecuteHelper
    {
        public static void OpenFolder(string? path = null)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = "explorer.exe",
                Arguments = path ?? string.Empty,
                UseShellExecute = true,
            };
            Process.Start(psi);
        }
    }
}

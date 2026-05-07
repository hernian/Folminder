using System.CodeDom;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Folminder.Platform
{
    public static class ShellExecuteHelper
    {
        public class OpenFolderException : DirectoryNotFoundException
        {
            public string Path { get; init; }

            public OpenFolderException(string message)
                : base(message)
            {
                this.Path = string.Empty;
            }

            public OpenFolderException(string message, string path)
                : base(message)
            {
                this.Path = path;
            }
        }

        public const string GUID_PC = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
        public const string GUID_HOME = "::{f0d63f95-3643-4369-ad58-3652136415a0}";

        public static void OpenFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new OpenFolderException($"Directory don't exist. Path: {path}", path);
            }
            var psi = new ProcessStartInfo()
            {
                Verb = "Open",
                FileName = path,
                UseShellExecute = true,
            };
            Process.Start(psi);
        }
        public static void OpenExplorer(string? path = null)
        {
            if (!Directory.Exists(path))
            {
                throw new OpenFolderException($"Directory don't exist. Path: {path}", path);
            }
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

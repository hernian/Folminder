using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using SHDocVw;
using Shell32;

namespace Folminder.Models
{
    public record WndPath(IntPtr HWnd, string Path);

    public class FolderList
    {
        public FolderList()
        {

        
        }

        public IEnumerable<Folder> GetFolderList(IReadOnlyList<Folder> pinnedFolders)
        {
            foreach (var pinned in pinnedFolders)
            {
                yield return pinned;
            }
            foreach (var wp in GetWndPaths())
            {
                var exists = pinnedFolders.Any(pf => string.Equals(pf.Path, wp.Path, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    var folder = new Folder(false, wp.Path);
                    yield return folder;
                }
            }
        }

        public IntPtr FindExplorerWindow(string path)
        {
            var hWhd = GetWndPaths()
                .Where(wp => string.Equals(wp.Path, path, StringComparison.OrdinalIgnoreCase))
                .Select(wp => wp.HWnd)
                .FirstOrDefault();
            return hWhd;
        }

        private IReadOnlyList<WndPath> GetWndPaths()
        {
            Debug.WriteLine($"FolderList Update");
            var folderList = new List<WndPath>();

            // IShellWindowsを取得
            var shellWindows = new ShellWindows();
            for (int i = 0; i < shellWindows.Count; i++)
            {
                try
                {
                    var win = shellWindows.Item(i);
                    if (win == null || win!.Document == null)
                    {
                        continue;
                    }
                    string path = win!.Document.Folder.Self.Path;
                    if (path.StartsWith("::{"))
                    {
                        continue;
                    }
                    Debug.WriteLine($"    Path: {path}");
                    folderList.Add(new WndPath((IntPtr)win.HWND, path));
                }
                catch { }
            }
            return folderList;
        }
    }
}

using SHDocVw;
using System.Data;
using System.Diagnostics;

namespace Folminder.Models
{
    public record WndPath(IntPtr HWnd, string Path);

    public class FolderList
    {
        private List<Folder> _pinnedFolderList = new();

        public FolderList()
        {
        }

        public void SetPinnedFolder(IEnumerable<Folder> pinnedFolders)
        {
            _pinnedFolderList.Clear();
            _pinnedFolderList.AddRange(pinnedFolders);
        }

        public IEnumerable<Folder> GetFolderList()
        {
            // 1. ピン留めフォルダーを先に
            foreach (var pinnedFolder in _pinnedFolderList)
            {
                yield return pinnedFolder;
            }
            foreach (var wp in GetWndPaths())
            {
                var exists = _pinnedFolderList.Any(pf => PathHelper.Equals(pf.Path, wp.Path));
                if (!exists)
                {
                    yield return new Folder(pinned: false, wp.Path);
                }
            }
        }

        public IntPtr FindExplorerWindow(string path)
        {
            var hWhd = GetWndPaths()
                .Where(wp => PathHelper.Equals(wp.Path, path))
                .Select(wp => wp.HWnd)
                .FirstOrDefault();
            return hWhd;
        }

        private IReadOnlyList<WndPath> GetWndPaths()
        {
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
                    folderList.Add(new WndPath((IntPtr)win.HWND, path));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"FolderList.GetWndPaths exception occurred. {ex}");
                }
            }
            return folderList;
        }
    }
}

using Folminder.Models;
using System.Diagnostics;
using System.Text.Json;

namespace Folminder.Platform
{
    public static class SettingsStorage
    {
        private record SerializableFolder(bool Pinned, string Path);

        public static IEnumerable<Folder> LoadPinnedFolderList()
        {
            var folderList = new List<Folder>();
            try
            {
                var folderListJson = Properties.Settings.Default.PinnedFolders;
                if (!string.IsNullOrEmpty(folderListJson))
                {
                    var tempList = JsonSerializer.Deserialize<List<string>>(folderListJson);
                    if (tempList != null)
                    {
                        folderList.AddRange(tempList.Select(path => new Folder(pinned: true, path)));
                    }
                }
            }
            catch { }
            return folderList;
        }

        public static void SavePinnedFolderList(IEnumerable<Folder> pinnedList)
        {
            var tempList = pinnedList.Select(folder => folder.Path).ToList();
            var folderListJson = JsonSerializer.Serialize(tempList);
            Properties.Settings.Default.PinnedFolders = folderListJson;
            Properties.Settings.Default.Save();
        }

        public static HotKey LoadHotKey()
        {
            try
            {
                var hotKeyJson = Properties.Settings.Default.HotKey;
                var hotKey = JsonSerializer.Deserialize<HotKey>(hotKeyJson);
                if (hotKey == null)
                {
                    return HotKey.Default;
                }
                if (!HotKey.Validate(hotKey))
                {
                    Debug.WriteLine($"SettingsStorage loaded invalid hotKey. {hotKey}");
                    return HotKey.Default;
                }
                return hotKey;
            }
            catch { }
            //デフォルト値
            return HotKey.Default;
        }

        public static void SaveHotKey(HotKey hotKey)
        {
            var hotKeyJson = JsonSerializer.Serialize(hotKey);
            Properties.Settings.Default.HotKey = hotKeyJson;
            Properties.Settings.Default.Save();
        }
    }
}

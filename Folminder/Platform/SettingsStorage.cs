using Folminder.Models;
using Folminder.Platform;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Input;

namespace Folminder.Platform
{
    public static class SettingsStorage
    {
        public static IReadOnlyList<Folder> LoadPinnedFolderList()
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
                        folderList.AddRange(tempList.Select(path => new Folder(true, path)));
                    }
                }
            }
            catch { }
            return folderList;
        }

        public static void SavePinnedFolderList(IReadOnlyList<Folder> pinnedList)
        {
            var tempList = pinnedList.Select(folder => folder.Path).ToList();
            var folderListJson = JsonSerializer.Serialize(tempList);
            Properties.Settings.Default.PinnedFolders = folderListJson;
            Properties.Settings.Default.Save();
        }

        public static HotKeyHelper.HotKey LoadHotKey()
        {
            try
            {
                var hotKeyJson = Properties.Settings.Default.HotKey;
                var hotKey = JsonSerializer.Deserialize<HotKeyHelper.HotKey>(hotKeyJson);
                if (hotKey != null)
                {
                    return hotKey;
                }
            }
            catch { }
            //デフォルト値
            uint vKey = (uint)KeyInterop.VirtualKeyFromKey(Key.F);
            return new HotKeyHelper.HotKey(Alt: true, Control: true, Shift: false, Win: false, vKey);
        }

        public static void SaveHotKey(HotKeyHelper.HotKey hotKey)
        {
            var hotKeyJson = JsonSerializer.Serialize(hotKey);
            Properties.Settings.Default.HotKey = hotKeyJson;
            Properties.Settings.Default.Save();
        }
    }
}

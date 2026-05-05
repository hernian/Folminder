using Folminder.Models;
using Folminder.Platform;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Folminder.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private static readonly string KEY_SEQ = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? HideWindowRequested;

        public ObservableCollection<FolderViewModel> Items
        {
            get => _items;
        }

        private readonly ObservableCollection<FolderViewModel> _items = new();

        private FolderViewModel? _selectedItem;
        public FolderViewModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
                }
            }
        }

        private readonly FolderList _folderList;

        public MainWindowViewModel(FolderList folderList)
        {
            _folderList = folderList;
            _folderList.SetPinnedFolder(SettingsStorage.LoadPinnedFolderList());
        }

        public void UpdateCommand()
        {
            _items.Clear();
            var index = 0;
            var sortedFolders = _folderList.GetFolderList().OrderBy(x => x).Take(KEY_SEQ.Length);
            foreach (var folder in sortedFolders)
            {
                var key = KEY_SEQ[index].ToString();
                var item = new FolderViewModel(key, folder);
                _items.Add(item);
                index++;
            }
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
        }

        public void ActivateCommand()
        {
            if (this.SelectedItem == null)
            {
                return;
            }
            var path = this.SelectedItem.Path;
            var hWndFolder = _folderList.FindExplorerWindow(path);
            if (hWndFolder != IntPtr.Zero)
            {
                WinApiHelper.ActivateWindow(hWndFolder);
            }
            else
            {
                ShellExecuteHelper.OpenFolder(path);
            }
            var pinnedFolders = _items.Where(i => i.Pinned).Select(i => i.Source.WithPinned(pinned: true)).ToList();
            _folderList.SetPinnedFolder(pinnedFolders);
            SettingsStorage.SavePinnedFolderList(pinnedFolders);
            this.HideWindowRequested?.Invoke(this, EventArgs.Empty);
        }

        public void OpenExplorerCommand()
        {
            ShellExecuteHelper.OpenFolder();
            this.HideWindowRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// キー入力を処理し、マッチする項目を選択状態にする
        /// </summary>
        /// <param name="keyString">押されたキーの文字列表現（例: "A", "B"）</param>
        /// <returns>マッチする項目が見つかった場合true</returns>
        public bool ProcessKeyInput(string keyString)
        {
            var matchingItem = _items.FirstOrDefault(item => item.Key == keyString);

            if (matchingItem != null)
            {
                SelectedItem = matchingItem;
                return true;
            }

            return false;
        }
    }
}

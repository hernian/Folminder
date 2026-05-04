using Folminder.Models;
using Folminder.Platform;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace Folminder.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? HideWindowRequested;

        public ObservableCollection<RowItem> Items
        {
            get => _items;
        }

        private readonly ObservableCollection<RowItem> _items = new();

        private RowItem? _selectedItem;
        public RowItem? SelectedItem
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
        public double MaxShortNameWidth { get; set; }

        private readonly FolderList _folderList;
        private readonly IWindowMetricsService _windowMetrics;
        private readonly List<Folder> _pinnedList = new();

        public MainWindowViewModel(FolderList folderList, IWindowMetricsService windowMetrics)
        {
            _folderList = folderList;
            _windowMetrics = windowMetrics;
            _pinnedList.AddRange(SettingsStorage.LoadPinnedFolderList());
        }

        public void UpdateCommand()
        {
            var shortPathBuilder = _windowMetrics.CreateShortPathBuilder();
            var folders = _folderList.GetFolderList(_pinnedList);
            _items.Clear();
            var ch = 'A';
            foreach (var folder in folders)
            {
                Debug.WriteLine($"RowItem {folder.Path}");
                var displayName = shortPathBuilder.GetShortName(folder.Segments);
                var item = new RowItem(ch.ToString(), displayName, folder);
                _items.Add(item);
            }
            this.MaxShortNameWidth = shortPathBuilder.ActualMaxWidth;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxShortNameWidth)));
        }

        public void ActivateCommand()
        {
            if (this.SelectedItem == null)
            {
                return;
            }
            var path = this.SelectedItem.Folder.Path;
            var hWndFolder = _folderList.FindExplorerWindow(path);
            if (hWndFolder != IntPtr.Zero)
            {
                WinApiHelper.ActivateWindow(hWndFolder);
            }
            else
            {
                ShellExecuteHelper.OpenFolder(path);
            }
            _pinnedList.Clear();
            _pinnedList.AddRange(GetPinnedList());
            SettingsStorage.SavePinnedFolderList(_pinnedList);
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

        private IReadOnlyList<Folder> GetPinnedList()
        {
            var pinnedList = new List<Folder>();
            foreach (var rowItem in _items)
            {
                if (rowItem.Pinned)
                {
                    pinnedList.Add(rowItem.Folder);
                }
            }
            return pinnedList;
        }
    }
}

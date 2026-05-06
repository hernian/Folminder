using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Folminder.Models;
using Folminder.Platform;
using Folminder.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Folminder.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private static readonly string KEY_SEQ = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        public event EventHandler? HideWindowRequested;
        public event EventHandler<ConfigDialogViewModel>? SettingsDialogRequested;
        public event EventHandler? OpenWindowRequested;
        public event EventHandler? ExitApplicationRequested;

        private bool _isSettingsDialogOpen = false;

        public ObservableCollection<FolderViewModel> Items => _items;

        private readonly ObservableCollection<FolderViewModel> _items = new();

        [ObservableProperty]
        private FolderViewModel? selectedItem;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(SelectedItem))
            {
                AcceptCommand.NotifyCanExecuteChanged();
                ApplyCommand.NotifyCanExecuteChanged();
            }
        }

        private readonly FolderList _folderList;
        private readonly HotKeyService _hotKeyService;

        public MainWindowViewModel(FolderList folderList, HotKeyService hotKeyService)
        {
            _folderList = folderList;
            _hotKeyService = hotKeyService;
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
            this.OnPropertyChanged(nameof(Items));
        }

        [RelayCommand(CanExecute = nameof(CanShowSettings))]
        private void ShowSettings()
        {
            var currentHotKey = _hotKeyService.CurrentHotKey;
            var configDialogViewModel = new ConfigDialogViewModel(currentHotKey);

            _isSettingsDialogOpen = true;
            ShowSettingsCommand.NotifyCanExecuteChanged();

            // Viewにダイアログ表示を要求
            SettingsDialogRequested?.Invoke(this, configDialogViewModel);

            _isSettingsDialogOpen = false;
            ShowSettingsCommand.NotifyCanExecuteChanged();
        }

        private bool CanShowSettings()
        {
            return !_isSettingsDialogOpen;
        }

        [RelayCommand]
        private void OpenWindow()
        {
            this.OpenWindowRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Exit()
        {
            this.ExitApplicationRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 選択されたフォルダーを開き、ピン留めを保存してウィンドウを閉じます
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanActivate))]
        private void Accept()
        {
            // VerbにOpenを指定してパス名を与えればExplorerが良くしてくれる。
            var path = this.SelectedItem!.Path;
            ShellExecuteHelper.OpenFolder(path);
            SavePinnedFolder();
            this.HideWindowRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool CanActivate()
        {
            return SelectedItem != null;
        }

        [RelayCommand]
        private void Cancel()
        {
            this.HideWindowRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void OpenExplorer()
        {
            // ShellExecuteHelper.OpenExplorer();
            ShellExecuteHelper.OpenFolder(ShellExecuteHelper.GUID_PC);
            this.HideWindowRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// ピン留めの状態を保存します
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanActivate))]
        private void Apply()
        {
            SavePinnedFolder();
            // ウィンドウを閉じない（適用ボタンの一般的な動作）
        }

        private void SavePinnedFolder()
        {
            // ピン留めされた項目は2回列挙するからリストにする
            var pinnedFolders = _items
                .Where(i => i.Pinned)
                .Select(i => i.Source.WithPinned(pinned: true))
                .ToList();
            _folderList.SetPinnedFolder(pinnedFolders);
            SettingsStorage.SavePinnedFolderList(pinnedFolders);
        }
    }
}

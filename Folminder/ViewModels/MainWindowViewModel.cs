using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Folminder.Models;
using Folminder.Platform;
using Folminder.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Interop;
using System.Windows.Media.Media3D;

namespace Folminder.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        // フォルダー表示のKeyに与える文字。
        // 各1文字がKeyになる。char配列より記述しやすいので文字列で表現する。
        private static readonly string KEY_SEQ = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        // 例外をメッセージへ変換するルール群
        private static readonly ExceptionHandler.IErrorToMessage[] EXCEPTION_RULES = [
            new ExceptionToMessage<ShellExecuteHelper.OpenFolderException>(
                ex => {
                    var msg = string.IsNullOrEmpty(ex.Path)
                        ? "ディレクトリが指定されていません"
                        : $"ディレクトリが見つかりません. {ex.Path}";
                    return new (MessageKind.Error, msg);
                }),
            ];

        public event EventHandler? HideWindowRequested;
        public event EventHandler? OpenWindowRequested;
        public event EventHandler? ExitApplicationRequested;
        public event EventHandler<ConfigDialogViewModel>? SettingsDialogRequested;
        public event EventHandler<MessageRequestedEventArgs>? MessageRequested;

        private readonly ExceptionHandler _exceptionHandler;
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
            _exceptionHandler = new ExceptionHandler(EXCEPTION_RULES, ShowMessage);
            _folderList.SetPinnedFolder(SettingsStorage.LoadPinnedFolderList());
        }

        public void UpdateCommand()
        {
            _exceptionHandler.CommandHarness(() =>
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
            });
        }

        [RelayCommand(CanExecute = nameof(CanShowSettings))]
        private void ShowSettings()
        {
            _exceptionHandler.CommandHarness(() =>
            {
                var currentHotKey = _hotKeyService.CurrentHotKey;
                var configDialogViewModel = new ConfigDialogViewModel(currentHotKey);

                _isSettingsDialogOpen = true;
                ShowSettingsCommand.NotifyCanExecuteChanged();

                // Viewにダイアログ表示を要求
                SettingsDialogRequested?.Invoke(this, configDialogViewModel);

                _isSettingsDialogOpen = false;
                ShowSettingsCommand.NotifyCanExecuteChanged();
            });
        }

        private bool CanShowSettings()
        {
            return !_isSettingsDialogOpen;
        }

        [RelayCommand]
        private void OpenWindow()
        {
            _exceptionHandler.CommandHarness(() =>
            {
                this.OpenWindowRequested?.Invoke(this, EventArgs.Empty);
            });
        }

        [RelayCommand]
        private void Exit()
        {
            _exceptionHandler.CommandHarness(() =>
            {
                this.ExitApplicationRequested?.Invoke(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// 選択されたフォルダーを開き、ピン留めを保存してウィンドウを閉じます
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanActivate))]
        private void Accept()
        {
            _exceptionHandler.CommandHarness(() =>
            {
                // VerbにOpenを指定してパス名を与えればExplorerが良くしてくれる。
                var path = this.SelectedItem!.Path;
                ShellExecuteHelper.OpenFolder(path);
                SavePinnedFolder();
                this.HideWindowRequested?.Invoke(this, EventArgs.Empty);
            });
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
            _exceptionHandler.CommandHarness(() =>
            {
                // ShellExecuteHelper.OpenExplorer();
                ShellExecuteHelper.OpenFolder(ShellExecuteHelper.GUID_PC);
                this.HideWindowRequested?.Invoke(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// ピン留めの状態を保存します
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanActivate))]
        private void Apply()
        {
            _exceptionHandler.CommandHarness(() =>
            {
                SavePinnedFolder();
                ShowMessage(new Message(MessageKind.Information, "ピン留めされたフォルダー一覧を保存しました"));
                // ウィンドウを閉じない（適用ボタンの一般的な動作
            });
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

        /// <summary>
        /// メッセージを表示します
        /// </summary>
        /// <param name="message">表示するメッセージ</param>
        /// <param name="messageType">メッセージの種類（Error/Information）</param>
        public void ShowMessage(Message message)
        {
            MessageRequested?.Invoke(this, new MessageRequestedEventArgs(message));
        }
    }
}

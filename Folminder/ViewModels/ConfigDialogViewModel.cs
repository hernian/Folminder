using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Folminder.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Folminder.ViewModels
{
    public partial class ConfigDialogViewModel : ObservableObject
    {
        private static IReadOnlyList<NameKey> CreateNameKeyList()
        {
            var nameKeyList = new List<NameKey>();
            // A～Z
            for (int i = 0; i < 26; i++)
            {
                var ch = (char)('A' + i);
                nameKeyList.Add(new NameKey(ch.ToString(), (Key)(Key.A + i)));
            }
            // 1～9
            for (int i = 0; i < 9; i++)  // '1'～'9'
            {
                var ch = (char)('1' + i);
                nameKeyList.Add(new NameKey(ch.ToString(), (Key)(Key.D1 + i)));
            }
            nameKeyList.Add(new NameKey('0'.ToString(), Key.D0)); // '0'
            // F1～F24
            for (int i = 0; i < 24; i++)
            {
                nameKeyList.Add(new NameKey($"F{i + 1}", (Key)(Key.F1 + i)));
            }
            nameKeyList.Add(new NameKey("変換", Key.ImeConvert));
            nameKeyList.Add(new NameKey("無変換", Key.ImeNonConvert));
            return nameKeyList;
        }

        private static readonly IReadOnlyList<NameKey> NAME_KEY_LIST = CreateNameKeyList();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
        private bool alt = HotKey.Default.Alt;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
        private bool control = HotKey.Default.Control;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
        private bool shift = HotKey.Default.Shift;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
        private bool win = HotKey.Default.Win;

        [ObservableProperty]
        private bool? dialogResult;

        public IReadOnlyList<NameKey> Items => NAME_KEY_LIST;
        public NameKey? SelectedItem { get; set; }

        public ConfigDialogViewModel(HotKey hotKey)
        {
            this.alt = hotKey.Alt;
            this.control = hotKey.Control;
            this.shift = hotKey.Shift;
            this.win = hotKey.Win;
            this.SelectedItem = this.Items.FirstOrDefault(nk => nk.Key == hotKey.Key);
        }

        [RelayCommand(CanExecute = nameof(CanAccept))]
        private void Accept()
        {
            DialogResult = true;
        }

        [RelayCommand]
        private void Cancel()
        {
            DialogResult = false;
        }

        private bool CanAccept()
        {
            return HotKey.Validate(Alt, Control, Shift, Win);
        }

        public HotKey GetHotKey()
        {
            if (this.SelectedItem == null)
            {
                throw new InvalidOperationException("Missing SelectedItem.");
            }
            return new HotKey(Alt, Control, Shift, Win, this.SelectedItem.Key);
        }
    }
}

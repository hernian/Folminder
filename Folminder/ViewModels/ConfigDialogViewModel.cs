using Folminder.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Folminder.ViewModels
{
    public class ConfigDialogViewModel : INotifyPropertyChanged
    {
        private static IReadOnlyList<NameKey> CreateNameKeyList()
        {
            var nameKeyList = new List<NameKey>();
            // A～Z
            for (int i = 0; i <= 26; i++)
            {
                var ch = (char)('A' + i);
                nameKeyList.Add(new NameKey(ch.ToString(), (Key)(Key.A + i)));
            }
            // 1～9
            for (int i = 0; i <= 9; i++)
            {
                var ch = (char)('0' + i);
                nameKeyList.Add(new NameKey(ch.ToString(), (Key)(Key.A + i)));
            }
            // F1～F24
            for (int i = 0; i < 24; i++)
            {
                nameKeyList.Add(new NameKey($"F{i}", (Key)(Key.F1 + i)));
            }
            nameKeyList.Add(new NameKey("変換", Key.ImeConvert));
            nameKeyList.Add(new NameKey("無変換", Key.ImeNonConvert));
            return nameKeyList;
        }

        private static readonly IReadOnlyList<NameKey> NAME_KEY_LIST = CreateNameKeyList();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Alt
        {
            get => _alt;
            set
            {
                if (_alt != value)
                {
                    _alt = value;
                    NotifyPropertyChanged();
                    UpdateIsOkButtonEnabled();
                }
            }
        }
        public bool Control
        {
            get => _control;
            set
            {
                if (_control != value)
                {
                    _control = value;
                    NotifyPropertyChanged();
                    UpdateIsOkButtonEnabled();
                }
            }
        }

        public bool Shift
        {
            get => _shift;
            set
            {
                if (_shift != value)
                {
                    _shift = value;
                    NotifyPropertyChanged();
                    UpdateIsOkButtonEnabled();
                }
            }
        }

        public bool Win
        {
            get => _win;
            set
            {
                if (_win != value)
                {
                    _win = value;
                    NotifyPropertyChanged();
                    UpdateIsOkButtonEnabled();
                }
            }
        }

        public bool IsOkButtonEnabled
        {
            get => _isOkButtonEnabled;
            private set
            {
                if (_isOkButtonEnabled != value)
                {
                    _isOkButtonEnabled = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public IReadOnlyList<NameKey> Items => NAME_KEY_LIST;
        public NameKey? SelectedItem { get; set; }

        private bool _alt = HotKey.Default.Alt;
        private bool _control = HotKey.Default.Control;
        private bool _shift = HotKey.Default.Shift;
        private bool _win = HotKey.Default.Win;
        private bool _isOkButtonEnabled = true;

        public ConfigDialogViewModel(HotKey hotKey)
        {
            this._alt = hotKey.Alt;
            this._control = hotKey.Control;
            this._shift = hotKey.Shift;
            this._win = hotKey.Win;
            this.SelectedItem = this.Items.FirstOrDefault(nk => nk.Key == hotKey.Key);
            UpdateIsOkButtonEnabled();
        }

        private void UpdateIsOkButtonEnabled()
        {
            IsOkButtonEnabled = _alt || _control || _shift || _win;
        }

        public HotKey GetHotKey()
        {
            if (this.SelectedItem == null)
            {
                throw new InvalidOperationException("Missing SelectedItem.");
            }
            return new HotKey(_alt, _control, _shift, _win, this.SelectedItem.Key);
        }
    }
}

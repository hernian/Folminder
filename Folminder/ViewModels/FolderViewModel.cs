using Folminder.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Folminder.ViewModels
{
    public class FolderViewModel(string key, Folder source) : INotifyPropertyChanged
    {
        private bool _pinned = source.Pinned;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Pinned {
            get => _pinned;
            set
            {
                if (this._pinned != value)
                {
                    this._pinned = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Key { get; } = key;
        public string Path { get; } = source.Path;

        public Folder Source { get; } = source;
    }
}

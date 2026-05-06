using CommunityToolkit.Mvvm.ComponentModel;
using Folminder.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Folminder.ViewModels
{
    public partial class FolderViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool pinned;

        public string Key { get; }
        public string Path { get; }

        public Folder Source { get; }

        public FolderViewModel(string key, Folder source)
        {
            this.Pinned = source.Pinned;
            this.Key = key;
            this.Path = source.Path;
            this.Source = source;
        }
    }
}

using Folminder.Models;

namespace Folminder.ViewModels
{
    public class RowItem
    {
        public bool Pinned
        {
            get => _folder.Pinned;
            set => _folder.Pinned = value;
        }
        public string Key { get; init; }
        public string DisplayName { get; init; }

        public Folder Folder => _folder;

        private readonly Folder _folder;

        public RowItem(string key, string displayName, Folder folder)
        {
            this.Key = key;
            this.DisplayName = displayName;
            _folder = folder;
        }
    }
}

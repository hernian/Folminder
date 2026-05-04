using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.CompilerServices;
using System.Text;

namespace Folminder.Models
{
    public class Folder
    {
        public bool Pinned { get; set; }
        public string Path { get; init; }
        public string[] Segments { get; init; }
        public DateTime RecentUsedTime { get; set; }

        public Folder(bool pinned, string path)
        {
            this.Pinned = pinned;
            this.Path = path;
            this.Segments = path.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            this.RecentUsedTime = DateTime.MinValue;
        }
    }
}

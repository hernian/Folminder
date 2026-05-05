using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Folminder.Models
{
    public static class PathHelper
    {
        public static bool Equals(string path1, string path2)
            => string.Equals(path1, path2, StringComparison.OrdinalIgnoreCase);

        public static string[] SplitToSegments(string path)
            => path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        public static int Compare(string path1, string path2)
        {
            var segments1 = SplitToSegments(path1);
            var segments2 = SplitToSegments(path2);
            return Compare(segments1, segments2);
        }

        public static int Compare(string[] segments1, string[] segments2)
        {
            int minLength = Math.Min(segments1.Length, segments2.Length);
            for (int i = 0; i < minLength; i++)
            {
                int comparison = string.Compare(segments1[i], segments2[i], StringComparison.OrdinalIgnoreCase);
                if (comparison != 0)
                {
                    return comparison;
                }
            }
            return segments1.Length.CompareTo(segments2.Length);
        }
    }
}

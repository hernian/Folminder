using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;
using System.Text;

namespace Folminder.Models
{
    public class Folder : IComparable<Folder>
    {
        public bool Pinned { get; init; }

        public string Path { get; init; }

        public string[] Segments { get; init; }

        public Folder(bool pinned, string path)
        {
            this.Pinned = pinned;
            this.Path = path;
            this.Segments = PathHelper.SplitToSegments(path);
        }

        private Folder(bool pinned, string path, string[] segments)
        {
            this.Pinned = pinned;
            this.Path = path;
            this.Segments = segments;
        }

        public Folder WithPinned(bool pinned)
        {
            return new Folder(pinned, this.Path, this.Segments);
        }

        /// <summary>
        /// <para>Folderの比較</para>
        /// <para>ピン留めしているのとしていないのでは、ピン留めしている方が大。</para>
        /// <para>パスの比較はパス区切り毎に大小を比較する。<br/>
        /// 例えば c:\aa\bbb と c:\aaa\bbb を比較すると理想は後者が大。<br/>
        /// 単純に文字列比較だと aa\ と aaa を比較して前者が大となってしまう。</para>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Folder? other)
        {
            if (other == null)
            {
                return 1;
            }
            // ピン留めしているのとしていないのでは、ピン留めしている方が小（ソートすると前にくる）
            if (this.Pinned != other.Pinned)
            {
                return this.Pinned ? -1 : 1;
            }
            // Segmentsで比較する
            return PathHelper.Compare(this.Segments, other.Segments);
        }
    }
}

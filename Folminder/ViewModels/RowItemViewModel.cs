using Folminder.Models;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;

namespace Folminder.ViewModels
{
    /// <summary>
    /// RowItem の UI ラッパー。
    /// Path から Segments を生成し、列幅に応じた TruncatedName を保持する。
    /// FolderListView 専用。外部に公開しない。
    /// </summary>
    internal class RowItemViewModel : INotifyPropertyChanged
    {
        // ─── ソースデータへの参照 ─────────────────────────────────────────
        public FolderViewModel Source { get; }

        /// <summary>Path を区切り文字で分割したセグメント配列。生成後は不変。</summary>
        public string[] Segments { get; }

        // ─── バインディング用プロパティ ────────────────────────────────────

        /// <summary>
        /// Pinned の getter/setter。Source.Pinned へのパススルー。
        /// CheckBox の TwoWay バインディングで使用する。
        /// </summary>
        public bool Pinned
        {
            get => Source.Pinned;
            set
            {
                if (Source.Pinned == value) return;
                Source.Pinned = value;
                OnPropertyChanged(nameof(Pinned));
            }
        }

        public string Key => Source.Key;

        private string _truncatedName;

        /// <summary>現在の列幅に収まるよう短縮されたフォルダー名。</summary>
        public string TruncatedName
        {
            get => _truncatedName;
            private set
            {
                if (_truncatedName == value) return;
                _truncatedName = value;
                OnPropertyChanged(nameof(TruncatedName));
            }
        }

        // ─── コンストラクター ─────────────────────────────────────────────

        public RowItemViewModel(FolderViewModel source)
        {
            Source = source;
            Segments = source.Source.Segments;

            // 初期値: 列幅未確定なので末尾セグメントのみ表示
            _truncatedName = Segments.Length > 0 ? Segments[^1] : string.Empty;
        }

        // ─── FolderListView から呼ばれる更新メソッド ──────────────────────

        /// <summary>
        /// 列幅が変化したときに FolderListView から呼び出される。
        /// TruncationHelper で短縮名を再計算して TruncatedName を更新する。
        /// </summary>
        internal void UpdateTruncatedName(
            double columnWidth, Typeface typeface, double fontSize, double pixelsPerDip)
        {
            TruncatedName = TruncationHelper.Truncate(
                Segments, columnWidth, typeface, fontSize, pixelsPerDip);
        }

        // ─── INotifyPropertyChanged ───────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}


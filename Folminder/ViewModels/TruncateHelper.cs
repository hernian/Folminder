using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Folminder.ViewModels
{
    /// <summary>
    /// フォルダーパスの短縮表示アルゴリズム。
    /// Segments[0] と Segments[^1] を保護しながら中央から順に "..." へ置換する。
    /// </summary>
    internal static class TruncationHelper
    {
        private const string Ellipsis = "...";

        // ─── 公開メソッド ─────────────────────────────────────────────────

        /// <summary>
        /// フル表示が columnWidth に収まらない場合、中央のセグメントから順に
        /// "..." へ置換して列幅に収まる文字列を返す。
        /// Segments[0] と Segments[^1] は置換しない。
        /// </summary>
        public static string Truncate(
            string[] segments,
            double columnWidth,
            Typeface typeface,
            double fontSize,
            double pixelsPerDip)
        {
            if (segments == null || segments.Length == 0) return string.Empty;

            string sep = Path.DirectorySeparatorChar.ToString();
            string full = string.Join(sep, segments);

            // 列幅 0 以下はそのまま返す（未レンダリング時等）
            if (columnWidth <= 0)
                return full;

            // フル表示が収まるか確認
            if (MeasureText(full, typeface, fontSize, pixelsPerDip) <= columnWidth)
                return full;

            // Segments が 2 つ以下の場合は省略できない
            if (segments.Length <= 2)
                return full;

            // 省略候補インデックス: 1 ～ segments.Length-2 を中央から外側へ
            bool[] replaced = new bool[segments.Length]; // 全 false（0 と ^1 は常に false）

            foreach (int idx in MiddleOutIndices(1, segments.Length - 2))
            {
                replaced[idx] = true;

                string candidatePath = BuildPath(segments, replaced, sep);
                var candidateWidth = MeasureText(candidatePath, typeface, fontSize, pixelsPerDip);
                if (candidateWidth <= columnWidth)
                    return candidatePath;
            }

            // 全て置換しても収まらない場合は最小形（先頭 + "..." + 末尾）を返す
            return BuildPath(segments, replaced, sep);
        }

        /// <summary>
        /// テキストの表示幅をピクセル単位で計測する。
        /// FolderListView.CalculatePreferredWidth からも使用する。
        /// </summary>
        internal static double MeasureText(
            string text,
            Typeface typeface,
            double fontSize,
            double pixelsPerDip)
        {
            if (string.IsNullOrEmpty(text)) return 0.0;

            // FormattedText: https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.media.formattedtext
            var ft = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                pixelsPerDip);

            return ft.Width;
        }

        // ─── 非公開ヘルパー ───────────────────────────────────────────────

        /// <summary>
        /// replaced フラグに従ってパス文字列を構築する。
        /// 連続する置換セグメントは 1 つの "..." にまとめる。
        /// </summary>
        private static string BuildPath(string[] segments, bool[] replaced, string sep)
        {
            var parts = new List<string>(segments.Length);
            bool prevWasEllipsis = false;

            for (int i = 0; i < segments.Length; i++)
            {
                if (replaced[i])
                {
                    if (!prevWasEllipsis)
                    {
                        parts.Add(Ellipsis);
                        prevWasEllipsis = true;
                    }
                }
                else
                {
                    parts.Add(segments[i]);
                    prevWasEllipsis = false;
                }
            }

            return string.Join(sep, parts);
        }

        /// <summary>
        /// [start, end] の範囲を中央から外側に向かう順序で列挙する。
        /// 例: start=1, end=4 → 2, 3, 1, 4
        /// </summary>
        private static IEnumerable<int> MiddleOutIndices(int start, int end)
        {
            if (start > end) yield break;

            int center = (start + end) / 2;
            yield return center;

            int lo = center - 1;
            int hi = center + 1;

            while (lo >= start || hi <= end)
            {
                if (hi <= end) yield return hi++;
                if (lo >= start) yield return lo--;
            }
        }
    }
}

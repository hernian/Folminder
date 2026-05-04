using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Folminder.Models
{
    public class ShortPathBuilder
    {
        private const string TRUNCATED_SEGMENT = "…";

        private static IReadOnlyList<int> CreateReplaceOrderList(int n)
        {
            // 1..n を作る
            var src = Enumerable.Range(0, n).ToList();

            // 先頭と末尾を除いた中央部分
            var midList = src.Skip(1).Take(n - 2).ToList();

            int count = midList.Count;
            if (count == 0)
                return new List<int>(); // N <= 2 の場合は空

            // 中央インデックス（0-based）
            int center = count / 2;

            var result = new List<int>(count);
            result.Add(midList[center]);

            int offset = 1;
            while (center - offset >= 0 || center + offset < count)
            {
                if (center + offset < count)
                    result.Add(midList[center + offset]);

                if (center - offset >= 0)
                    result.Add(midList[center - offset]);

                offset++;
            }

            return result;
        }

        public double ActualMaxWidth => _actualMaxWidth;
        private readonly FontSpec _fontSpec;
        private readonly double _maxWidth;
        private double _actualMaxWidth;

        public ShortPathBuilder(FontSpec fontSpec, double maxWidth)
        {
            _fontSpec = fontSpec;
            _maxWidth = maxWidth;
        }

        public string GetShortName(IReadOnlyList<string> pathSegments)
        {
            if (pathSegments == null || pathSegments.Count == 0)
                return string.Empty;

            if (pathSegments.Count == 1)
                return pathSegments[0];

            // 置き換え可能なインデックスのリスト
            var replaceableIndices = CreateReplaceOrderList(pathSegments.Count);

            // 現在の状態を保持する配列
            var segments = new string[pathSegments.Count];
            for (int i = 0; i < pathSegments.Count; i++)
            {
                segments[i] = pathSegments[i];
            }

            // 文字列幅が_maxWidth以下になるまで要素を"…"に置き換える
            string path = string.Join("\\", segments);
            var width = _fontSpec.CreateFormattedText(path).Width;
            if (width > _maxWidth)
            {
                foreach (var index in replaceableIndices)
                {
                    // 現在のインデックスを省略名に置き換える
                    segments[index] = TRUNCATED_SEGMENT;
                    path = string.Join("\\", segments);
                    width = _fontSpec.CreateFormattedText(path).Width;
                    if (width <= _maxWidth)
                    {
                        break;
                    }
                }
            }
            Debug.WriteLine($"ShortPathBuilder.GetShortName. Path: {width}");
            _actualMaxWidth = Math.Max(_actualMaxWidth, width);
            return path;
        }
    }
}

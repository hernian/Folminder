using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Folminder.Helpers;

namespace Folminder.Controls;

/// <summary>
/// 選択された項目が常に表示されるListView
/// </summary>
public class AutoScrollListView : ListView
{
    public event EventHandler? RowDoubleClick;

    public double GetPreferredColumWidth(GridViewColumn column, double textWidth)
    {
        double width = textWidth;

        // セルのパディングを取得
        double padding = GetCellHorizontalPadding(column);
        width += padding;
        width += 16; // ちょっとマージンを追加しないと文字列の末尾が収まらないことがある。
        return width;
    }

    private double GetCellHorizontalPadding(GridViewColumn column)
    {
        // 最初のアイテムからContentPresenterを取得してパディングを測定
        if (this.Items.Count > 0)
        {
            var firstItem = this.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
            if (firstItem != null)
            {
                var rowPresenter = VisualTreeHelpers.FindVisualChild<GridViewRowPresenter>(firstItem);
                if (rowPresenter != null && this.View is GridView gridView)
                {
                    // カラムのインデックスを取得
                    int columnIndex = gridView.Columns.IndexOf(column);
                    if (columnIndex >= 0 && columnIndex < VisualTreeHelper.GetChildrenCount(rowPresenter))
                    {
                        var cell = VisualTreeHelper.GetChild(rowPresenter, columnIndex);
                        if (cell is ContentPresenter contentPresenter)
                        {
                            // ContentPresenterのマージンを基本とする
                            double padding = contentPresenter.Margin.Left + contentPresenter.Margin.Right;

                            // ContentPresenter内の最初のFrameworkElementのマージンとパディングを加算
                            if (VisualTreeHelper.GetChildrenCount(contentPresenter) > 0)
                            {
                                var content = VisualTreeHelper.GetChild(contentPresenter, 0);
                                if (content is FrameworkElement element)
                                {
                                    padding += element.Margin.Left + element.Margin.Right;

                                    // Control（TextBlock、CheckBoxなど）の場合はPaddingも考慮
                                    if (element is Control control)
                                    {
                                        padding += control.Padding.Left + control.Padding.Right;
                                    }
                                }
                            }

                            return padding;
                        }
                    }
                }
            }
        }

        // デフォルト値（MainWindow.xaml.csのCELL_HORZ_PADDINGと同じ）
        return 16.0;
    }

    public double PreferredHeight
    {
        get
        {
            double height = this.BorderThickness.Top + this.BorderThickness.Bottom;

            // カラムヘッダの高さを取得
            double headerHeight = GetHeaderHeight();
            height += headerHeight;

            if (this.Items.Count > 0)
            {
                var first = (ListViewItem)this.ItemContainerGenerator.ContainerFromIndex(0);
                height += first.ActualHeight * (this.Items.Count + 0.5); // 0.5は下端のマージン
            }
            return height;
        }
    }

    private double GetHeaderHeight()
    {
        if (this.View is GridView gridView)
        {
            // GridViewHeaderRowPresenterを検索
            var headerRow = VisualTreeHelpers.FindVisualChild<GridViewHeaderRowPresenter>(this);
            if (headerRow != null)
            {
                return headerRow.ActualHeight;
            }
            // デフォルトのヘッダー高さ（ヘッダーがまだレンダリングされていない場合）
            return 25.0;
        }
        return 0.0;
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);

        if (SelectedItem != null)
        {
            ScrollIntoView(SelectedItem);
        }
    }

    protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
    {
        base.OnMouseDoubleClick(e);

        // ダブルクリックされた要素を取得
        var clickedElement = e.OriginalSource as DependencyObject;
        if (clickedElement != null)
        {
            // クリックされた要素から親のListViewItemを検索
            var listViewItem = VisualTreeHelpers.FindVisualParent<ListViewItem>(clickedElement);
            if (listViewItem != null && listViewItem.IsEnabled)
            {
                // 有効な行がダブルクリックされた場合、イベントを発火
                RowDoubleClick?.Invoke(this, EventArgs.Empty);
            }
        }
    }

}

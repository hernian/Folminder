using Folminder.Helpers;
using Folminder.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Folminder.Controls;

/// <summary>
/// ピン留め・ショートカットキー・フォルダー名の 3 列を持つ特殊 ListView。
/// <list type="bullet">
///   <item>RowItemsSource に RowItem のコレクションをバインドする。</item>
///   <item>PreferredWidth / PreferredHeight でコンテンツに最適なサイズを公開する。</item>
///   <item>サイズが変化したとき PreferredSizeChanged イベントを発火する。</item>
///   <item>列幅が不足する場合はフォルダー名を中央から "..." に短縮表示する。</item>
/// </list>
/// </summary>
public class FolderListView : ListView
{
    // WIDTH_MARGIN: GridViewが内部的に各列に追加するレイアウトマージン（列間セパレーター、
    // リサイズグリッパー領域等）による固定オーバーヘッド。
    // 実測により、列幅の合計は計算値より約8ピクセル大きくなることが確認されている。
    // この値は文字形状（s/W/M等）に依存しない。
    private const double WIDTH_MARGIN = 8.0;
    // GridViewColumn ヘッダーのデフォルト左右パディング合計 (4px × 2)
    private const double ColumnHeaderSidePadding = 8.0;
    // フォルダー名セルの左右パディング合計 (GridViewColumn デフォルト)
    private const double FolderCellPadding = 6.0;

    // =====================================================================
    #region DependencyProperties
    // =====================================================================

    // ── CheckBoxColumnWidth ──────────────────────────────────────────────
    // ピン留め列の幅。XAML で指定する固定値。
    public static readonly DependencyProperty CheckBoxColumnWidthProperty =
    DependencyProperty.Register(
        nameof(CheckBoxColumnWidth), typeof(double), typeof(FolderListView),
        new PropertyMetadata(28.0, OnFixedColumnWidthChanged));

    public double CheckBoxColumnWidth
    {
        get => (double)GetValue(CheckBoxColumnWidthProperty);
        set => SetValue(CheckBoxColumnWidthProperty, value);
    }

    // ── KeyColumnWidth ───────────────────────────────────────────────────
    // ショートカットキー列の幅。XAML で指定する固定値。
    public static readonly DependencyProperty KeyColumnWidthProperty =
    DependencyProperty.Register(
        nameof(KeyColumnWidth), typeof(double), typeof(FolderListView),
        new PropertyMetadata(28.0, OnFixedColumnWidthChanged));

    public double KeyColumnWidth
    {
        get => (double)GetValue(KeyColumnWidthProperty);
        set => SetValue(KeyColumnWidthProperty, value);
    }

    // ── FolderNameColumnWidth (読み取り専用) ─────────────────────────────
    // ActualWidth から固定列を差し引いた残り幅。XAML バインディングのソース。
    // https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.dependencyproperty.registerreadonly
    private static readonly DependencyPropertyKey FolderNameColumnWidthKey =
    DependencyProperty.RegisterReadOnly(
        nameof(FolderNameColumnWidth), typeof(double), typeof(FolderListView),
        new PropertyMetadata(0.0));

    public static readonly DependencyProperty FolderNameColumnWidthProperty =
        FolderNameColumnWidthKey.DependencyProperty;

    public double FolderNameColumnWidth
        => (double)GetValue(FolderNameColumnWidthProperty);

    // ── RowItemsSource ───────────────────────────────────────────────────
    // 外部（MainWindow）から FolderViewModel のコレクションを受け取る。
    // ObservableCollection<FolderViewModel> を渡すと差分更新に対応する。
    public static readonly DependencyProperty RowItemsSourceProperty =
        DependencyProperty.Register(
            nameof(RowItemsSource),
            typeof(IEnumerable<FolderViewModel>),
            typeof(FolderListView),
            new PropertyMetadata(null, OnRowItemsSourceChanged));

    public IEnumerable<FolderViewModel> RowItemsSource
    {
        get => (IEnumerable<FolderViewModel>)GetValue(RowItemsSourceProperty);
        set => SetValue(RowItemsSourceProperty, value);
    }

    // ── SelectedRowItem ──────────────────────────────────────────────────
    // 選択された行のFolderViewModel。MainWindowのViewModelとバインドする。
    public static readonly DependencyProperty SelectedRowItemProperty =
        DependencyProperty.Register(
            nameof(SelectedRowItem),
            typeof(FolderViewModel),
            typeof(FolderListView),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedRowItemChanged));

    public FolderViewModel? SelectedRowItem
    {
        get => (FolderViewModel?)GetValue(SelectedRowItemProperty);
        set => SetValue(SelectedRowItemProperty, value);
    }

        #endregion

        // =====================================================================
        #region 公開 API
        // =====================================================================

        /// <summary>
        /// フォルダー名列に最長パスが収まり、余白が生じない横幅。
        /// MainWindow はこの値を基にウィンドウ幅を決定する。
        /// </summary>
        public double PreferredWidth { get; private set; }

        /// <summary>
        /// 全行・ヘッダー・ボーダーが収まり、余白が生じない縦幅。
        /// MainWindow はこの値を基にウィンドウ高さを決定する。
        /// </summary>
        public double PreferredHeight { get; private set; }

        /// <summary>
        /// Items の変化により PreferredWidth または PreferredHeight が変化したときに発火する。
        /// MainWindow はこのイベントを受けてウィンドウサイズを更新する。
        /// </summary>
        public event EventHandler? PreferredSizeChanged;

        /// <summary>
        /// 行がダブルクリックされたときに発火する。
        /// </summary>
        public event EventHandler? RowDoubleClick;

        #endregion

        // =====================================================================
        #region 内部フィールド
        // =====================================================================

        /// <summary>
        /// ListView の実際の ItemsSource。外部には公開しない。
        /// RowItemsSource の変化を受けて同期される。
        /// </summary>
        private readonly ObservableCollection<RowItemViewModel> _internalItems = new();

        /// <summary>
        /// GridViewHeaderRowPresenterのキャッシュ。一度検索したら保持する。
        /// </summary>
        private GridViewHeaderRowPresenter? _cachedHeaderPresenter;

        #endregion

    // =====================================================================
    #region コンストラクター & テンプレート
    // =====================================================================

    public FolderListView()
    {
        SizeChanged += OnSizeChanged;
        SelectionChanged += OnSelectionChanged;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // _internalItems を ListView の唯一の ItemsSource として設定する。
        // 外部から base.ItemsSource を直接設定されないよう、
        // RowItemsSource DP 経由でのみデータを受け付ける。
        base.ItemsSource = _internalItems;

        UpdateFolderNameColumnWidth();
    }

    #endregion

    // =====================================================================
    #region 列幅の計算
    // =====================================================================

    private static void OnFixedColumnWidthChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((FolderListView)d).UpdateFolderNameColumnWidth();

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged) return;
        UpdateFolderNameColumnWidth();
    }

    /// <summary>
    /// FolderNameColumnWidth を再計算し、全行の短縮名を更新する。
    /// SizeChanged または固定列幅変化時に呼ばれる。
    /// </summary>
    private void UpdateFolderNameColumnWidth()
    {
        // デザインモード時やActualWidthが0の場合は処理をスキップ
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this) || ActualWidth <= 0)
            return;

        var (typeface, fontSize, ppd) = GetTextMetrics();

        // ピン留め列とキー列は固定幅（XAMLでバインドされている値）
        double checkBoxColumnWidth = CheckBoxColumnWidth;
        double keyColumnWidth = KeyColumnWidth;

        // フォルダー列のヘッダー幅
        double folderHeaderWidth = TruncationHelper.MeasureText("フォルダー", typeface, fontSize, ppd);

        // フォルダー列に使用可能な幅を計算
        double usable = ActualWidth
            - checkBoxColumnWidth
            - keyColumnWidth
            - (BorderThickness.Left + BorderThickness.Right)
            - SystemParameters.VerticalScrollBarWidth;

        // ヘッダーに必要な最小幅（ヘッダー用パディング含む）
        double minFolderWidth = folderHeaderWidth + ColumnHeaderSidePadding;
        double newColumnWidth = Math.Max(minFolderWidth, usable);

        // テキストが実際に使える幅（セル用パディングのみを引く）
        double textWidth = newColumnWidth - FolderCellPadding;

        // デバッグ出力（簡潔版）
        System.Diagnostics.Debug.WriteLine($"=== UpdateFolderNameColumnWidth: ActualWidth={ActualWidth:F1}, NewColumnWidth={newColumnWidth:F1}, TextWidth={textWidth:F1} ===");

        // 変化が 0.5px 未満なら更新しない（浮動小数の揺れを吸収）
        if (Math.Abs(newColumnWidth - FolderNameColumnWidth) < 0.5) return;

        SetValue(FolderNameColumnWidthKey, newColumnWidth);
        UpdateAllTruncatedNames(textWidth);
    }

    /// <summary>全 ViewModel の TruncatedName を現在の列幅で再計算する。</summary>
    private void UpdateAllTruncatedNames(double textWidth)
    {
        var (typeface, fontSize, ppd) = GetTextMetrics();
        foreach (var vm in _internalItems)
            vm.UpdateTruncatedName(textWidth, typeface, fontSize, ppd);
    }

    /// <summary>
    /// フォルダー列の列幅からテキストが実際に使える幅を計算する。
    /// セルのパディング（FolderCellPadding）のみを引く。
    /// ColumnHeaderSidePaddingはヘッダー用なので引かない。
    /// </summary>
    private double GetTextWidthFromColumnWidth()
    {
        return Math.Max(0.0, FolderNameColumnWidth - FolderCellPadding);
    }

    #endregion

        // =====================================================================
        #region コレクション同期
        // =====================================================================

        private static void OnRowItemsSourceChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (FolderListView)d;

        // 旧コレクションの変更通知を解除
        if (e.OldValue is INotifyCollectionChanged oldCol)
            oldCol.CollectionChanged -= self.OnSourceCollectionChanged;

        self.RebuildInternalItems((IEnumerable<FolderViewModel>)e.NewValue);

        // 新コレクションが Observable なら差分更新を購読
        if (e.NewValue is INotifyCollectionChanged newCol)
            newCol.CollectionChanged += self.OnSourceCollectionChanged;
    }

    private void RebuildInternalItems(IEnumerable<FolderViewModel> source)
    {
        _internalItems.Clear();

        if (source != null)
        {
            // デザインモード時はテキストメトリクスの計算をスキップ
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                foreach (var item in source)
                {
                    var vm = new RowItemViewModel(item);
                    _internalItems.Add(vm);
                }
            }
            else
            {
                var (typeface, fontSize, ppd) = GetTextMetrics();
                double textWidth = GetTextWidthFromColumnWidth();

                foreach (var item in source)
                {
                    var vm = new RowItemViewModel(item);
                    vm.UpdateTruncatedName(textWidth, typeface, fontSize, ppd);
                    _internalItems.Add(vm);
                }
            }
        }

        RecalculatePreferredSizeDeferred();
    }

    private void OnSourceCollectionChanged(
        object? sender, NotifyCollectionChangedEventArgs e)
    {
        // デザインモード時は処理をスキップ
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            return;

        var (typeface, fontSize, ppd) = GetTextMetrics();
        double textWidth = GetTextWidthFromColumnWidth();

        switch (e.Action)
        {
        case NotifyCollectionChangedAction.Add:
        {
            if (e.NewItems is not null)
            {
                int insertAt = e.NewStartingIndex;
                foreach (FolderViewModel item in e.NewItems)
                {
                    var vm = new RowItemViewModel(item);
                    vm.UpdateTruncatedName(textWidth, typeface, fontSize, ppd);
                    _internalItems.Insert(insertAt++, vm);
                }
            }
            break;
        }
        case NotifyCollectionChangedAction.Remove:
        {
            if (e.OldItems is not null)
            {
                for (int i = 0; i < e.OldItems.Count; i++)
                    _internalItems.RemoveAt(e.OldStartingIndex);
            }
            break;
        }
        case NotifyCollectionChangedAction.Replace:
        {
            if (e.NewItems is not null)
            {
                for (int i = 0; i < e.NewItems.Count; i++)
                {
                    var item = e.NewItems[i] as FolderViewModel;
                    if (item is not null)
                    {
                        var vm = new RowItemViewModel(item);
                        vm.UpdateTruncatedName(textWidth, typeface, fontSize, ppd);
                        _internalItems[e.NewStartingIndex + i] = vm;
                    }
                }
            }
            break;
        }
        case NotifyCollectionChangedAction.Move:
        {
            var moved = _internalItems[e.OldStartingIndex];
            _internalItems.RemoveAt(e.OldStartingIndex);
            _internalItems.Insert(e.NewStartingIndex, moved);
            break;
        }
        case NotifyCollectionChangedAction.Reset:
        {
            // Reset は RebuildInternalItems 内で Deferred を呼ぶので return
            RebuildInternalItems(RowItemsSource);
            return;
        }
        }

        RecalculatePreferredSizeDeferred();
    }

    #endregion

    // =====================================================================
    #region 選択項目の同期
    // =====================================================================

    /// <summary>
    /// SelectedRowItemプロパティが変更された時のコールバック。
    /// 外部からSelectedRowItemが設定された場合、内部のSelectedItemを同期する。
    /// </summary>
    private static void OnSelectedRowItemChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (FolderListView)d;
        var newFolder = e.NewValue as FolderViewModel;

        // 循環参照を避けるため、フラグでチェック
        if (self._isUpdatingSelection) return;

        self._isUpdatingSelection = true;
        try
        {
            if (newFolder == null)
            {
                self.SelectedItem = null;
            }
            else
            {
                // FolderViewModelに対応するRowItemViewModelを検索して選択
                var rowItem = self._internalItems.FirstOrDefault(
                    vm => vm.Source == newFolder);
                self.SelectedItem = rowItem;
            }
        }
        finally
        {
            self._isUpdatingSelection = false;
        }
    }

    /// <summary>
    /// ListViewのSelectionChangedイベントのハンドラー。
    /// 内部のSelectedItemが変更された場合、SelectedRowItemを同期する。
    /// </summary>
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 循環参照を避けるため、フラグでチェック
        if (_isUpdatingSelection) return;

        _isUpdatingSelection = true;
        try
        {
            var selectedRowItem = SelectedItem as RowItemViewModel;
            SelectedRowItem = selectedRowItem?.Source;
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    /// <summary>
    /// 選択項目の同期中に循環参照を防ぐフラグ
    /// </summary>
    private bool _isUpdatingSelection = false;

    #endregion

    // =====================================================================
    #region キー入力処理
    // =====================================================================

    /// <summary>
    /// キー入力を処理し、マッチする項目を選択する。
    /// 英数字キーが押された場合、対応するKeyを持つ項目を選択状態にする。
    /// </summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // 押されたキーを文字列に変換（例: Key.A -> "A"）
        string keyString = e.Key.ToString();

        // マッチする項目を検索
        var matchingItem = _internalItems.FirstOrDefault(
            item => item.Key == keyString);

        if (matchingItem != null)
        {
            // 選択項目を更新（SelectedRowItemも自動的に同期される）
            SelectedItem = matchingItem;

            // フォーカスを当てる
            var container = ItemContainerGenerator
                .ContainerFromItem(matchingItem) as ListViewItem;
            container?.Focus();

            e.Handled = true; // イベントを処理済みとしてマーク
        }
    }

    #endregion

    // =====================================================================
    #region PreferredSize の計算とイベント発火
    // =====================================================================

    /// <summary>
    /// レイアウトが確定した後に計測するため Dispatcher で遅延実行する。
    /// Items 変化直後は ListViewItem がまだレンダリングされていないため必要。
    /// </summary>
    private void RecalculatePreferredSizeDeferred()
    {
        // デザインモード時は処理をスキップ
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            return;

        // DispatcherPriority.Loaded: レイアウト完了後・描画前に実行される
        Dispatcher.InvokeAsync(RecalculatePreferredSize, DispatcherPriority.Loaded);
    }

    private void RecalculatePreferredSize()
    {
        // デザインモード時は処理をスキップ
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            return;

        double newW = CalculatePreferredWidth();
        double newH = CalculatePreferredHeight();

        bool changed = Math.Abs(newW - PreferredWidth) > 0.5
            || Math.Abs(newH - PreferredHeight) > 0.5;
        if (!changed) return;

        PreferredWidth = newW;
        PreferredHeight = newH;
        PreferredSizeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// フォルダー名を短縮せずに表示できる理想的な横幅を計算する。
    /// 各列のヘッダーとコンテンツの実際のテキスト幅を測定して必要な幅を計算する。
    /// </summary>
    private double CalculatePreferredWidth()
    {
        var (typeface, fontSize, ppd) = GetTextMetrics();

        // ピン留め列: XAMLで固定幅にバインドされているため、その値を使用
        double checkBoxColumnWidth = CheckBoxColumnWidth;

        // ショートカットキー列: XAMLで固定幅にバインドされているため、その値を使用
        double keyColumnWidth = KeyColumnWidth;

        // フォルダー名列: ヘッダーとコンテンツで必要なパディングが異なる
        double folderHeaderWidth = TruncationHelper.MeasureText("フォルダー", typeface, fontSize, ppd);
        string maxFolderPath = string.Empty;
        double maxFolderContentWidth = 0.0;
        if (_internalItems.Count > 0)
        {
            var maxItem = _internalItems.MaxBy(vm =>
                TruncationHelper.MeasureText(vm.Source.Path, typeface, fontSize, ppd));
            if (maxItem is not null)
            {
                maxFolderPath = maxItem.Source.Path;
                maxFolderContentWidth = TruncationHelper.MeasureText(maxFolderPath, typeface, fontSize, ppd);
            }
        }

        // ヘッダーに必要な幅（ヘッダー用パディング含む）
        double folderHeaderRequiredWidth = folderHeaderWidth + ColumnHeaderSidePadding;
        // コンテンツに必要な幅（セル用パディングのみ）
        double folderContentRequiredWidth = maxFolderContentWidth + FolderCellPadding;
        // 列幅は両者の大きい方（切り上げて丸め誤差を吸収）
        double folderColumnWidth = Math.Ceiling(Math.Max(folderHeaderRequiredWidth, folderContentRequiredWidth));

        double borderWidth = BorderThickness.Left + BorderThickness.Right;
        double scrollBarWidth = SystemParameters.VerticalScrollBarWidth;
        double totalWidth = checkBoxColumnWidth
            + keyColumnWidth
            + folderColumnWidth
            + borderWidth
            + scrollBarWidth
            + WIDTH_MARGIN;

        // 実際の列幅を取得（レンダリング後のみ取得可能）
        double actualCheckBoxColWidth = 0.0;
        double actualKeyColWidth = 0.0;
        double actualFolderColWidth = 0.0;
        if (View is GridView gridView && gridView.Columns.Count >= 3)
        {
            actualCheckBoxColWidth = gridView.Columns[0].ActualWidth;
            actualKeyColWidth = gridView.Columns[1].ActualWidth;
            actualFolderColWidth = gridView.Columns[2].ActualWidth;
        }

        // 実際のセルコンテンツの幅を測定（最長パスの行から取得）
        double actualCellContentWidth = 0.0;
        double measuredTextWidth = maxFolderContentWidth;
        if (_internalItems.Count > 0)
        {
            // 最長パスを持つ行のインデックスを取得
            int maxIndex = 0;
            double maxWidth = 0.0;
            for (int i = 0; i < _internalItems.Count; i++)
            {
                double w = TruncationHelper.MeasureText(_internalItems[i].Source.Path, typeface, fontSize, ppd);
                if (w > maxWidth)
                {
                    maxWidth = w;
                    maxIndex = i;
                }
            }

            // その行のListViewItemとTextBlockを取得
            var container = ItemContainerGenerator.ContainerFromIndex(maxIndex) as ListViewItem;
            if (container != null)
            {
                // GridViewRowPresenterを探す
                var rowPresenter = VisualTreeHelpers.FindVisualChild<GridViewRowPresenter>(container);
                if (rowPresenter != null && rowPresenter.Columns.Count > 2)
                {
                    // フォルダー列（3番目の列）のContentPresenterを取得
                    var folderCell = VisualTreeHelper.GetChild(rowPresenter, 2) as ContentPresenter;
                    if (folderCell != null)
                    {
                        var textBlock = VisualTreeHelpers.FindVisualChild<TextBlock>(folderCell);
                        if (textBlock != null)
                        {
                            actualCellContentWidth = textBlock.ActualWidth;
                        }
                    }
                }
            }
        }

        // デバッグ出力（簡潔版）
        System.Diagnostics.Debug.WriteLine("=== CalculatePreferredWidth ===");
        System.Diagnostics.Debug.WriteLine($"  MaxPath: {maxFolderPath}");
        System.Diagnostics.Debug.WriteLine($"  Calculated TotalWidth: {totalWidth:F1}, ListView ActualWidth: {ActualWidth:F1}");
        if (actualCellContentWidth > 0)
        {
            System.Diagnostics.Debug.WriteLine($"  TEXT MEASUREMENT: FormattedText={measuredTextWidth:F1}, TextBlock={actualCellContentWidth:F1}, Diff={actualCellContentWidth - measuredTextWidth:F1}");
        }
        System.Diagnostics.Debug.WriteLine($"  Columns Overhead (GridView internal margin): {(actualCheckBoxColWidth + actualKeyColWidth + actualFolderColWidth) - (checkBoxColumnWidth + keyColumnWidth + folderColumnWidth):F1}");

        return totalWidth;
    }

    /// <summary>
    /// 全行・ヘッダー・ボーダーを収める理想的な縦幅を計算する。
    /// レンダリング済みの ListViewItem から実際の行高さを取得する。
    /// </summary>
    private double CalculatePreferredHeight()
    {
        double rowHeight = GetActualRowHeight();
        double headerHeight = GetActualHeaderHeight();

        return headerHeight
            + rowHeight * (_internalItems.Count + 0.5) // 行の半分の高さ分余裕を持たせる
            + (BorderThickness.Top + BorderThickness.Bottom);
    }

    /// <summary>
    /// 実際にレンダリングされた行の高さを取得する。
    /// 未レンダリング時はフォントサイズからの推定値を返す。
    /// </summary>
    private double GetActualRowHeight()
    {
        // VirtualizingPanel.IsVirtualizing="False" のため全行が生成済みのはずだが、
        // 初回レイアウト前は null になる場合があるためフォールバックを用意する。
        if (_internalItems.Count > 0)
        {
            // https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.controls.itemcontainergenerator.containerfromindex
            var container = ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
            if (container?.ActualHeight > 0)
                return container.ActualHeight;
        }

        return FontSize + 8.0; // 上下パディング 4px × 2 の推定値
    }

    /// <summary>
    /// GridViewHeaderRowPresenter の実際の高さを取得する。
    /// 未レンダリング時はフォントサイズからの推定値を返す。
    /// </summary>
    private double GetActualHeaderHeight()
    {
        // キャッシュがなければ検索して保存
        if (_cachedHeaderPresenter == null)
        {
            _cachedHeaderPresenter = VisualTreeHelpers.FindVisualChild<GridViewHeaderRowPresenter>(this);
        }

        if (_cachedHeaderPresenter?.ActualHeight > 0)
            return _cachedHeaderPresenter.ActualHeight;

        return FontSize + 12.0; // ヘッダーパディングの推定値
    }

    #endregion

    // =====================================================================
    #region テキスト計測ヘルパー
    // =====================================================================

    private (Typeface typeface, double fontSize, double pixelsPerDip) GetTextMetrics()
    {
        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        return (typeface, FontSize, GetPixelsPerDip());
    }

    /// <summary>
    /// 現在の DPI スケールから PixelsPerDip を取得する。
    /// Visual がまだウィンドウに接続されていない場合は 1.0 を返す。
    /// </summary>
    private double GetPixelsPerDip()
    {
        // PresentationSource 経由で正確な DPI を取得する。
        // VisualTreeHelper.GetDpi より確実にフォールバックが効く。
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
    }

    #endregion

    // =====================================================================
    #region イベントハンドリング
    // =====================================================================

    protected override void OnMouseDoubleClick(System.Windows.Input.MouseButtonEventArgs e)
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

    #endregion
}

using Folminder.Platform;
using Folminder.Services;
using Folminder.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Folminder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int HOTKEY_ID = 1;
        private const float WORKING_AREA_SCALE = 0.7f;

        private readonly MainWindowViewModel _viewModel = null!;
        private readonly HotKeyService _hotKeyService = null!;
        private bool _isActivated = false;
        private bool _isShowed = false;
        private bool _isReallyClosing = false;

        // デザインタイム用のパラメータレスコンストラクタ
        public MainWindow() : this(null!, null!)
        {
        }

        public MainWindow(MainWindowViewModel viewModel, HotKeyService hotKeyService)
        {
            InitializeComponent();

            // デザインモード時はnullチェックして早期リターン
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            this.DataContext = viewModel;
            _viewModel = viewModel;
            _hotKeyService = hotKeyService;

            _viewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
            _viewModel.HideWindowRequested += (_, __) => MinimizeAndHide();
            _viewModel.SettingsDialogRequested += OnSettingsDialogRequested;
            _viewModel.OpenWindowRequested += (_, __) => UpdateContents();
            _viewModel.ExitApplicationRequested += (_, __) => ReallyClose();
            _hotKeyService.HotKeyPressed += (_, __) => UpdateContents();

            this.SourceInitialized += MainWindow_SourceInitialized;
            this.Loaded += MainWindow_Loaded;
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            MainListView.RowDoubleClick += (_, __) => _viewModel.AcceptCommand.Execute(null);
            MainListView.PreferredSizeChanged += MainListView_PreferredSizeChanged;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            Debug.WriteLine($"MainWindow_SourceInitialized. isActivated: {_isActivated}");

            var helper = new WindowInteropHelper(this);
            var h = helper.Handle;
            Debug.WriteLine($"MainWindow_SourceInitialized. hWnd: {h:x8}");

            // 先にWndProcフックを追加してからHotKeyを登録
            // PresentationSource.FromVisualではなくHwndSource.FromHwndを使う
            var hwndSource = HwndSource.FromHwnd(h);
            if (hwndSource != null)
            {
                hwndSource.AddHook(WndProc);
                Debug.WriteLine("WndProc hook added successfully");
            }
            else
            {
                Debug.WriteLine("ERROR: Failed to get HwndSource");
            }

            // HotKeyServiceを初期化（ウィンドウ登録とHotKey登録）
            _hotKeyService.Initialize(this, HOTKEY_ID);

            /*
            // まだ表示されていない状態でレイアウトを確定させる
            this.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            this.Arrange(new Rect(0, 0, this.DesiredSize.Width, this.DesiredSize.Height));
            this.UpdateLayout();
            SetWindowCenter();
            */
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"MainWindow_Loaded. isActivated: {_isActivated}");
            /*
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Debug.WriteLine("MainWindow_Loaded Set MainWindow center of working area.");
                SetWindowCenter();
            }), DispatcherPriority.Render);
            */
        }

        private void MainListView_PreferredSizeChanged(object? sender, EventArgs e)
        {
            var workingArea = ScreenHelper.GetWorkingArea(this);
            var maxWidth = workingArea.Width * WORKING_AREA_SCALE;
            var maxHeight = workingArea.Height * WORKING_AREA_SCALE;
            var horzGap = this.ActualWidth - MainListView.ActualWidth;
            var vertGap = this.ActualHeight - MainListView.ActualHeight;
            var newWidth = horzGap + MainListView.PreferredWidth;
            var newHeight = vertGap + MainListView.PreferredHeight;

            Debug.WriteLine("=== MainListView_PreferredSizeChanged ===");
            Debug.WriteLine($"  MainWindow.ActualWidth: {this.ActualWidth:F1}");
            Debug.WriteLine($"  MainWindow.ActualHeight: {this.ActualHeight:F1}");
            Debug.WriteLine($"  MainListView.ActualWidth: {MainListView.ActualWidth:F1}");
            Debug.WriteLine($"  MainListView.ActualHeight: {MainListView.ActualHeight:F1}");
            Debug.WriteLine($"  MainListView.PreferredWidth: {MainListView.PreferredWidth:F1}");
            Debug.WriteLine($"  MainListView.PreferredHeight: {MainListView.PreferredHeight:F1}");
            Debug.WriteLine($"  horzGap (Window - ListView): {horzGap:F1}");
            Debug.WriteLine($"  vertGap (Window - ListView): {vertGap:F1}");
            Debug.WriteLine($"  newWidth (horzGap + PreferredWidth): {newWidth:F1}");
            Debug.WriteLine($"  newHeight (vertGap + PreferredHeight): {newHeight:F1}");

            this.Width = newWidth;
            this.Height = newHeight;

            Debug.WriteLine($"  AFTER: MainWindow.Width set to: {this.Width:F1}");
            Debug.WriteLine($"  AFTER: MainWindow.Height set to: {this.Height:F1}");
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.AcceptCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void MainWindowViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.Items))
            {
                AdjustWindow();
            }
        }

        private void OnFolderListViewPreferredSizeChanged(object sender, EventArgs e)
        {
            AdjustWindow();
        }

        private void AdjustWindow()
        {
            Debug.WriteLine($"AdjustWindow. isActivated: {_isActivated}");
            if (!_isActivated)
            {
                return;
            }

            // 実際のサイズを計算
            var workingArea = ScreenHelper.GetWorkingArea(this);
            var widthGap = this.ActualWidth - MainListView.ActualWidth;
            var heightGap = this.ActualHeight - MainListView.ActualHeight;
            var newWidth = widthGap + MainListView.PreferredWidth;
            var newHeight = heightGap + MainListView.PreferredHeight;
            var newLeft = Math.Max(workingArea.Left + (workingArea.Width - newWidth) / 2, workingArea.Left);
            var newTop = Math.Max(workingArea.Top + (workingArea.Height - newHeight) / 2, workingArea.Top);
            Debug.WriteLine($"AdjustWindow. WorkingArea: left: {workingArea.Left} top: {workingArea.Top} width: {workingArea.Width} height: {workingArea.Height}");
            Debug.WriteLine($"AdjustWindow. left: {newLeft}, top: {newTop}, width: {newWidth}, height: {newHeight}");
            if (!_isShowed)
            {
                this.Show();
                _isShowed = true;
            }
            if (this.WindowState != WindowState.Normal)
            {
                this.WindowState = WindowState.Normal;
            }
            // WPFだと非表示のWindowは位置・サイズが不確定になるらしい
            // なので Win32 API SetWindowPos を使う。その後、WPFの位置・サイズも指定しておく
            var swp = new WinApiHelper.SetWindowPosParam(
                ChangeSize: true,
                ChangePosition: true,
                Visibility: WinApiHelper.WindowVisibility.Show,
                Left: newLeft,
                Top: newTop,
                Width: newWidth,
                Height: newHeight);
            WinApiHelper.SetWindowPos(this, swp);
            this.Activate();
            Dispatcher.BeginInvoke(() =>
            {
                Debug.WriteLine($"=== AdjustWindow: newWidth={newWidth:F1}, newHeight={newHeight:F1} ===");

                // サイズと位置を設定
                this.Width = newWidth;
                this.Height = newHeight;
                this.Left = newLeft;
                this.Top = newTop;
                this.Opacity = 1;
                if (MainListView.Items.Count > 0)
                {
                    MainListView.SelectedIndex = 0;
                    var firstItem = MainListView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                    firstItem?.Focus();
                }
            }, DispatcherPriority.Render);
        }
        private void MinimizeAndHide()
        {
            Debug.WriteLine($"MinimizeAndHide.");
            // WPFのWindowの位置・サイズは非表示になると不確定になるらしい
            // なのでWin32 API SetWindowPos で非表示にし、再度表示するときも SetWindowPosを使う
            var swp = new WinApiHelper.SetWindowPosParam(Visibility: WinApiHelper.WindowVisibility.Hide);
            WinApiHelper.SetWindowPos(this, swp);
            _isActivated = false;
        }

        private void OnSettingsDialogRequested(object? sender, ConfigDialogViewModel viewModel)
        {
            var configDialog = new ConfigDialog(viewModel);
            configDialog.Owner = this;
            configDialog.ShowDialog();

            if (viewModel.DialogResult == true)
            {
                try
                {
                    var newHotKey = viewModel.GetHotKey();
                    _hotKeyService.UpdateHotKey(newHotKey);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"設定の保存に失敗しました: {ex.Message}",
                                  "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            UpdateContents();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // sender から MenuItem を取得
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;

            // MessageBox表示中に再度クリックされないようにMenuItemを無効化
            menuItem.IsEnabled = false;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var name = asm.GetName();
                var version = name.Version;
                var company = asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Unknown";
                var product = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "Unknown";
                var message = $"{product}\nVer. {version}\nProduced by {company}. 2026";
                MessageBox.Show(
                    this,
                    $"Folminder by {company}.\nVer. {version}",
                    "Folminderについて",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                // MessageBoxを閉じたら再度有効化
                menuItem.IsEnabled = true;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // コンテキストメニューから閉じるを選択されたら _isReallyClosing がtrueになる
            if (!_isReallyClosing)
            {
                // MainWindowの右上のXがクリックされた等
                // ウィンドウを閉じる代わりに最小化して非表示にする
                e.Cancel = true;
                MinimizeAndHide();
                return;
            }
            // 本当に終了する場合
            _hotKeyService.Shutdown();
            base.OnClosing(e);
            NotifyIcon.Dispose();
        }

        private void ReallyClose()
        {
            _isReallyClosing = true;
            Application.Current.Shutdown();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = _hotKeyService.ProcessWindowMessage(msg, wParam);
            return IntPtr.Zero;
        }

        private void UpdateContents()
        {
            _isActivated = true;
            _viewModel.UpdateCommand();
        }
    }
}
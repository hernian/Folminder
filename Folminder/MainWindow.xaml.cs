using Folminder.Platform;
using Folminder.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
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
        private const float WORKING_AREA_SCALE = 0.7f;
        private const int HOTKEY_ID = 1;

        private MainWindowViewModel _viewModel;
        private bool _isActivated = false;
        private bool _isShowed = false;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();

            this.DataContext = viewModel;
            _viewModel = viewModel;
            _viewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
            _viewModel.HideWindowRequested += (_, __) => this.MinimizeAndHide();

            this.SourceInitialized += MainWindow_SourceInitialized;
            this.Loaded += MainWindow_Loaded;
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            mainListView.RowDoubleClick += (_, __) => _viewModel.ActivateCommand();
            mainListView.PreferredSizeChanged += mainListView_PreferredSizeChanged;
            acceptButton.Click += (_, __) => _viewModel.ActivateCommand();
            cancelButton.Click += (_, __) => this.MinimizeAndHide();
            openExplorerButton.Click += (_, __) => _viewModel.OpenExplorerCommand();
        }

        private bool IsHandleCreated()
        {
            var source = (HwndSource)HwndSource.FromVisual(this);
            return source?.Handle != IntPtr.Zero;
        }

        private void SetWindowCenter()
        {
            var workingArea = ScreenHelper.GetWorkingArea(this);
            var w = this.ActualWidth;
            var h = this.ActualHeight;
            this.Left = Math.Max(workingArea.Left + (workingArea.Width - w) / 2, 0);
            this.Top = Math.Max(workingArea.Top + (workingArea.Height - h) / 2, 0);
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

            var hotKey = SettingsStorage.LoadHotKey();
            HotKeyHelper.RegisterHotKey(this, HOTKEY_ID, hotKey);

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

        private void mainListView_PreferredSizeChanged(object? sender, EventArgs e)
        {
            var workingArea = ScreenHelper.GetWorkingArea(this);
            var maxWidth = workingArea.Width * WORKING_AREA_SCALE;
            var maxHeight = workingArea.Height * WORKING_AREA_SCALE;
            var horzGap = this.ActualWidth - mainListView.ActualWidth;
            var vertGap = this.ActualHeight - mainListView.ActualHeight;
            var newWidth = horzGap + mainListView.PreferredWidth;
            var newHeight = vertGap + mainListView.PreferredHeight;

            Debug.WriteLine("=== mainListView_PreferredSizeChanged ===");
            Debug.WriteLine($"  MainWindow.ActualWidth: {this.ActualWidth:F1}");
            Debug.WriteLine($"  MainWindow.ActualHeight: {this.ActualHeight:F1}");
            Debug.WriteLine($"  mainListView.ActualWidth: {mainListView.ActualWidth:F1}");
            Debug.WriteLine($"  mainListView.ActualHeight: {mainListView.ActualHeight:F1}");
            Debug.WriteLine($"  mainListView.PreferredWidth: {mainListView.PreferredWidth:F1}");
            Debug.WriteLine($"  mainListView.PreferredHeight: {mainListView.PreferredHeight:F1}");
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
                _viewModel.ActivateCommand();
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
            var widthGap = this.ActualWidth - mainListView.ActualWidth;
            var heightGap = this.ActualHeight - mainListView.ActualHeight;
            var newWidth = widthGap + mainListView.PreferredWidth;
            var newHeight = heightGap + mainListView.PreferredHeight;
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
                if (mainListView.Items.Count > 0)
                {
                    mainListView.SelectedIndex = 0;
                    var firstItem = mainListView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
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

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            UpdateContents();
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var hotKey = SettingsStorage.LoadHotKey();
            var configDialogViewModel = new ConfigDialogViewModel(hotKey);
            var configDialog = new ConfigDialog(configDialogViewModel);
            configDialog.Owner = this;
            if (configDialog.ShowDialog() == true)
            {
                hotKey = configDialogViewModel.GetHotKey();
                // 古いHotKeyを解除してから新しいものを登録
                HotKeyHelper.UnregisterHotKey(this, HOTKEY_ID);
                HotKeyHelper.RegisterHotKey(this, HOTKEY_ID, hotKey);
                SettingsStorage.SaveHotKey(hotKey);
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            UpdateContents();
        }

        private void ListView_KeyDown(object sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"ListView_KeyDown: {e.Key}");

            // ListViewで処理するキーの例（これらはWindowに届かない）
            // if (e.Key == Key.Delete)
            // {
            //     // 削除処理
            //     e.Handled = true; // これをtrueにするとWindowに届かない
            // }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Window_KeyDown: {e.Key}");

            // 押されたキーを文字列に変換（例: Key.A -> "A"）
            string keyString = e.Key.ToString();

            // ViewModelでキー入力を処理（マッチング＆選択）
            if (_viewModel.ProcessKeyInput(keyString))
            {
                e.Handled = true; // イベントを処理済みとしてマーク
            }

            // 他のキーの例
            // if (e.Key == Key.Enter)
            // {
            //     // Enterキーの処理
            // }
            // if (e.Key == Key.Escape)
            // {
            //     // Escapeキーの処理
            // }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            HotKeyHelper.UnregisterHotKey(this, HOTKEY_ID);
            base.OnClosing(e);
            notifyIcon.Dispose();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == HotKeyHelper.WM_HOTKEY)
            {
                Debug.WriteLine($"WM_HOTKEY. ID: {wParam.ToInt32()}");
                if (wParam.ToInt32() == HOTKEY_ID)
                {
                    UpdateContents();
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        private void UpdateContents()
        {
            _isActivated = true;
            _viewModel.UpdateCommand();
        }
    }
}
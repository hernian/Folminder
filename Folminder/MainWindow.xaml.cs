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
        private bool _isSourceInitialized = false;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();

            this.DataContext = viewModel;
            _viewModel = viewModel;
            _viewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
            _viewModel.HideWindowRequested += viewModel_HideWindowRequested;

            this.SourceInitialized += MainWindow_SourceInitialized;
            this.Loaded += MainWindow_Loaded;
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            mainListView.RowDoubleClick += (_, __) => _viewModel.ActivateCommand();
            mainListView.PreferredSizeChanged += mainListView_PreferredSizeChanged;
            acceptButton.Click += (_, __) => _viewModel.ActivateCommand();
            cancelButton.Click += (_, __) => this.Hide();
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
            Debug.WriteLine("MainWindow_SourceInitialized");
            _isSourceInitialized = true;

            var h = new WindowInteropHelper(this).Handle;
            Debug.WriteLine($"MainWindow_SourceInitialized. hWnd: {h:x8}");
            var hotKey = SettingsStorage.LoadHotKey();
            HotKeyHelper.RegisterHotKey(this, HOTKEY_ID, hotKey);

            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            hwndSource?.AddHook(WndProc);

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
            Debug.WriteLine("MainWindow_Loaded");
            /*
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Debug.WriteLine("MainWindow_Loaded Set MainWindow center of working area.");
                SetWindowCenter();
            }), DispatcherPriority.Render);
            */
        }

        private void viewModel_HideWindowRequested(object? sender, EventArgs e)
        {
            this.Hide();
            // 次回表示するときに必ず小さい状態から大きく広げる方向で調整が入るようにする
            // 一瞬大きなウィンドウが出てしまうと驚いてしまう
            this.Width = this.MinWidth;
            this.Height = this.MinHeight;
            // workingAreaの中央に配置
            var workingArea = ScreenHelper.GetWorkingArea(this);
            this.Left = Math.Max(workingArea.Left + (workingArea.Width - this.Width) / 2, 0.0);
            this.Top = Math.Max(workingArea.Top + (workingArea.Height - this.Height) / 2, 0.0);

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
            Debug.WriteLine($"AdjustWindow. isSourceInitialized: {_isSourceInitialized}");
            if (!_isSourceInitialized)
            {
                return;
            }
            /*
            this.Show();
            this.WindowState = WindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
            */
            Dispatcher.BeginInvoke(new Action(() =>
            {
                /*
                // 実際のサイズを計算
                var workingArea = ScreenHelper.GetWorkingArea(this);
                var widthGap = this.ActualWidth - mainListView.ActualWidth;
                var heightGap = this.ActualHeight - mainListView.ActualHeight;
                var newWidth = widthGap + mainListView.PreferredWidth;
                var newHeight = heightGap + mainListView.PreferredHeight;

                Debug.WriteLine($"=== AdjustWindow: newWidth={newWidth:F1}, newHeight={newHeight:F1} ===");

                // サイズと位置を設定
                this.Width = newWidth;
                this.Height = newHeight;
                this.Left = workingArea.Left + (workingArea.Width - newWidth) / 2;
                this.Top = workingArea.Top + (workingArea.Height - newHeight) / 2;
                */
                if (mainListView.Items.Count > 0)
                {
                    mainListView.SelectedIndex = 0;
                    var firstItem = mainListView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                    firstItem?.Focus();
                }
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _viewModel!.UpdateCommand();
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
            _viewModel.UpdateCommand();
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
                    _viewModel.UpdateCommand();
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }
    }
}
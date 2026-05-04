using Folminder.Helpers;
using Folminder.Models;
using Folminder.Platform;
using Folminder.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Folminder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IWindowMetricsService
    {
        private const int HOTKEY_ID = 1;
        private MainWindowViewModel? _viewModel;


        public MainWindow()
        {
            InitializeComponent();

            this.SourceInitialized += MainWindow_SourceInitialized;
            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += MainWindow_SizeChanged;
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
            mainListView.RowDoubleClick += (_, __) => _viewModel!.ActivateCommand();
            acceptButton.Click += (_, __) => _viewModel!.ActivateCommand();
            cancelButton.Click += (_, __) => this.Hide();
            openExplorerButton.Click += (_, __) => _viewModel!.OpenExplorerCommand();
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var hotKey = SettingsStorage.LoadHotKey();
            HotKeyHelper.RegisterHotKey(this, HOTKEY_ID, hotKey);

            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            hwndSource?.AddHook(WndProc);
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel!.ActivateCommand();
                e.Handled = true;
            }
        }

        public void SetViewModel(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
            this.DataContext = viewModel;
            _viewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
            _viewModel.HideWindowRequested += (_, __) => this.Hide();
        }

        private void MainWindowViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.Items))
            {
                AdjustWindow();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateFolderColumnWidth();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateFolderColumnWidth();
        }

        private void UpdateFolderColumnWidth()
        {
            if (mainListView.View is not GridView gridView)
                return;

            double fixedColumnsWidth = 0;
            foreach (var column in gridView.Columns)
            {
                if (column != folderColumn)
                {
                    fixedColumnsWidth += column.Width;
                }
            }

            const double margin = 30; // スクロールバーとマージン用
            var availableWidth = mainListView.ActualWidth - fixedColumnsWidth - margin;

            if (availableWidth > 0)
            {
                folderColumn.Width = availableWidth;
            }
        }

        private void AdjustWindow()
        {
            this.ShowInTaskbar = true;
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var widthGap = this.ActualWidth - folderColumn.ActualWidth;
                var heightGap = this.ActualHeight - mainListView.ActualHeight;
                this.Width = widthGap + mainListView.GetPreferredColumWidth(folderColumn, _viewModel!.MaxShortNameWidth);
                var height = heightGap + mainListView.PreferredHeight;
                this.Height = height;
                Debug.WriteLine($"MainWindow.Width: {this.Width}, MaxShortNameWidth: {_viewModel.MaxShortNameWidth}");
                Debug.WriteLine($"MainWindow.Height: {this.Height}, RequiredHeight: {height}");
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
            var configDialog = new ConfigDialog();
            configDialog.Owner = this;
            if (configDialog.ShowDialog() == true)
            {
                // 設定が保存された場合の処理
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            _viewModel!.UpdateCommand();
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
            if (_viewModel == null)
            {
                throw new InvalidOperationException("Missing view model.");
            }

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
            if (msg == WinApiHelper.WM_HOTKEY)
            {
                Debug.WriteLine($"WH_HOTKEY. ID: {wParam.ToInt32()}");
                if (wParam.ToInt32() == HOTKEY_ID)
                {
                    _viewModel!.UpdateCommand();
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        public Rect GetWorkingArea()
        {
            return ScreenHelper.GetWorkingArea(this);
        }
        public double GetNonVariableWidth()
        {
            double gap = this.Width - folderColumn.Width;
            return gap;
        }

        private const float WORKING_AREA_SCALE = 0.7f;


        public ShortPathBuilder CreateShortPathBuilder()
        {
            var workingArea =  ScreenHelper.GetWorkingArea(this);
            var scaledWorkingWidth = workingArea.Width * WORKING_AREA_SCALE;
            var maxPathWidth = Math.Max(scaledWorkingWidth - this.GetNonVariableWidth(), 400); // 400は定数に変更せよ
            var fontSpec = new FontSpec(mainListView);
            return new ShortPathBuilder(fontSpec, maxPathWidth);
        }
    }
}
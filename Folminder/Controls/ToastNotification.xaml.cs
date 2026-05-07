using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Folminder.ViewModels;

namespace Folminder.Controls
{
    /// <summary>
    /// ToastNotification.xaml の相互作用ロジック
    /// エラーメッセージ等を一時的に表示するトースト通知コントロール
    /// </summary>
    public partial class ToastNotification : UserControl
    {
        private DispatcherTimer? _timer;
        private Storyboard? _fadeInStoryboard;
        private Storyboard? _fadeOutStoryboard;

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(ToastNotification),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                nameof(Duration),
                typeof(int),
                typeof(ToastNotification),
                new PropertyMetadata(3000)); // デフォルト3秒

        public static readonly DependencyProperty ToastBackgroundProperty =
            DependencyProperty.Register(
                nameof(ToastBackground),
                typeof(Brush),
                typeof(ToastNotification),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(244, 67, 54)))); // デフォルト赤

        public static readonly DependencyProperty ToastForegroundProperty =
            DependencyProperty.Register(
                nameof(ToastForeground),
                typeof(Brush),
                typeof(ToastNotification),
                new PropertyMetadata(Brushes.White)); // デフォルト白

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(
                nameof(Type),
                typeof(MessageKind),
                typeof(ToastNotification),
                new PropertyMetadata(MessageKind.Error));

        /// <summary>
        /// 表示するメッセージ
        /// </summary>
        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        /// <summary>
        /// 表示時間（ミリ秒）
        /// </summary>
        public int Duration
        {
            get => (int)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        /// <summary>
        /// トーストの背景色
        /// </summary>
        public Brush ToastBackground
        {
            get => (Brush)GetValue(ToastBackgroundProperty);
            set => SetValue(ToastBackgroundProperty, value);
        }

        /// <summary>
        /// トーストの文字色
        /// </summary>
        public Brush ToastForeground
        {
            get => (Brush)GetValue(ToastForegroundProperty);
            set => SetValue(ToastForegroundProperty, value);
        }

        /// <summary>
        /// トーストの種類
        /// </summary>
        public MessageKind Type
        {
            get => (MessageKind)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }

        public ToastNotification()
        {
            InitializeComponent();
            Loaded += ToastNotification_Loaded;
        }

        private void ToastNotification_Loaded(object sender, RoutedEventArgs e)
        {
            _fadeInStoryboard = (Storyboard)FindResource("FadeInStoryboard");
            _fadeOutStoryboard = (Storyboard)FindResource("FadeOutStoryboard");

            if (_fadeOutStoryboard != null)
            {
                _fadeOutStoryboard.Completed += FadeOutStoryboard_Completed;
            }
        }

        /// <summary>
        /// トーストを表示します
        /// </summary>
        public void Show()
        {
            if (string.IsNullOrWhiteSpace(Message))
            {
                return;
            }

            // 既存のタイマーを停止
            _timer?.Stop();

            // 背景色と文字色を適用
            ToastBorder.Background = ToastBackground;
            MessageText.Foreground = ToastForeground;

            // フェードイン
            Visibility = Visibility.Visible;
            _fadeInStoryboard?.Begin(ToastBorder);

            // 指定時間後に自動的に閉じる
            if (Duration > 0)
            {
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(Duration)
                };
                _timer.Tick += Timer_Tick;
                _timer.Start();
            }
        }

        /// <summary>
        /// トーストを非表示にします
        /// </summary>
        public void Hide()
        {
            _timer?.Stop();
            _fadeOutStoryboard?.Begin(ToastBorder);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timer?.Stop();
            Hide();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void FadeOutStoryboard_Completed(object? sender, EventArgs e)
        {
            Visibility = Visibility.Collapsed;
        }
    }
}

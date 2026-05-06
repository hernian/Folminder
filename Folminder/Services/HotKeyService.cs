using Folminder.Models;
using Folminder.Platform;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;

namespace Folminder.Services
{
    /// <summary>
    /// HotKeyの管理を一元化するサービス
    /// HotKeyとHotKeyIDの所有者として、全てのHotKey関連操作を管理します
    /// </summary>
    public class HotKeyService
    {
        private Window? _window;
        private int _hotKeyId;
        private HotKey _currentHotKey;
        private bool _isRegistered = false;

        public const int WM_HOTKEY = HotKeyHelper.WM_HOTKEY;

        /// <summary>
        /// HotKeyが押されたときに発火するイベント
        /// </summary>
        public event EventHandler? HotKeyPressed;

        /// <summary>
        /// 現在登録されているHotKeyを取得
        /// </summary>
        public HotKey CurrentHotKey => _currentHotKey;

        /// <summary>
        /// HotKeyServiceのコンストラクタ
        /// </summary>
        /// <param name="hotKeyId">管理するHotKeyのID</param>
        public HotKeyService()
        {
            _currentHotKey = LoadHotKeyFromStorage();
            Debug.WriteLine($"HotKeyService: 構築完了 ID={_hotKeyId}");
        }

        /// <summary>
        /// ウィンドウを登録してHotKeyを初期化
        /// MainWindowのSourceInitializedイベントで呼び出される
        /// </summary>
        /// <param name="window">登録先のウィンドウ</param>
        public void Initialize(Window window, int hotKeyId)
        {
            if (_window != null)
            {
                throw new InvalidOperationException("HotKeyServiceは既に初期化されています。");
            }

            _window = window;
            _hotKeyId = hotKeyId;
            RegisterCurrentHotKey();
            Debug.WriteLine($"HotKeyService.Initialize: ウィンドウ登録完了");
        }

        /// <summary>
        /// ストレージからHotKeyを読み込む
        /// </summary>
        private HotKey LoadHotKeyFromStorage()
        {
            var hotKey = SettingsStorage.LoadHotKey();
            Debug.WriteLine($"HotKeyService.LoadHotKeyFromStorage: Alt={hotKey.Alt}, Control={hotKey.Control}, Shift={hotKey.Shift}, Win={hotKey.Win}, Key={hotKey.Key}");
            return hotKey;
        }

        /// <summary>
        /// HotKeyをストレージに保存
        /// </summary>
        private void SaveHotKeyToStorage(HotKey hotKey)
        {
            SettingsStorage.SaveHotKey(hotKey);
            Debug.WriteLine($"HotKeyService.SaveHotKeyToStorage: Alt={hotKey.Alt}, Control={hotKey.Control}, Shift={hotKey.Shift}, Win={hotKey.Win}, Key={hotKey.Key}");
        }

        /// <summary>
        /// 現在のHotKeyを登録
        /// </summary>
        private void RegisterCurrentHotKey()
        {
            if (_window == null)
            {
                throw new InvalidOperationException("ウィンドウが登録されていません。Initialize()を先に呼び出してください。");
            }

            if (_isRegistered)
            {
                UnregisterCurrentHotKey();
            }

            HotKeyHelper.RegisterHotKey(_window, _hotKeyId, _currentHotKey);
            _isRegistered = true;
            Debug.WriteLine($"HotKeyService.RegisterCurrentHotKey: ID={_hotKeyId}");
        }

        /// <summary>
        /// 現在登録されているHotKeyを解除
        /// </summary>
        private void UnregisterCurrentHotKey()
        {
            if (_window != null && _isRegistered)
            {
                HotKeyHelper.UnregisterHotKey(_window, _hotKeyId);
                _isRegistered = false;
                Debug.WriteLine($"HotKeyService.UnregisterCurrentHotKey: ID={_hotKeyId}");
            }
        }

        /// <summary>
        /// HotKeyを更新（解除 → 登録 → 保存）
        /// </summary>
        /// <param name="newHotKey">新しいHotKey</param>
        public void UpdateHotKey(HotKey newHotKey)
        {
            if (_window == null)
            {
                throw new InvalidOperationException("ウィンドウが登録されていません。Initialize()を先に呼び出してください。");
            }

            _currentHotKey = newHotKey;
            RegisterCurrentHotKey();
            SaveHotKeyToStorage(newHotKey);
            Debug.WriteLine($"HotKeyService.UpdateHotKey: 完了");
        }

        /// <summary>
        /// サービスの終了処理（HotKeyの解除）
        /// </summary>
        public void Shutdown()
        {
            UnregisterCurrentHotKey();
            Debug.WriteLine($"HotKeyService.Shutdown: 完了");
        }

        /// <summary>
        /// WndProcでのメッセージ処理
        /// </summary>
        /// <param name="msg">メッセージID</param>
        /// <param name="wParam">wParam</param>
        /// <returns>処理した場合true</returns>
        public bool ProcessWindowMessage(int msg, IntPtr wParam)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _hotKeyId)
            {
                Debug.WriteLine($"HotKeyService.ProcessWindowMessage: HotKey押下検出 ID={wParam.ToInt32()}");
                HotKeyPressed?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }
    }
}

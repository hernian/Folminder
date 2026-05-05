using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Folminder.Platform
{
    public static class WinApiHelper
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;
        public static void ActivateWindow(IntPtr hWnd)
        {
            ShowWindow(hWnd, SW_RESTORE);
            SetForegroundWindow(hWnd);
        }

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_HIDEWINDOW = 0x0080;

        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        public static void ForceSizeAndPos(IntPtr hWnd, double left = 0, double top = 0, double width = 0, double height = 0)
        {
            SetWindowPos(hWnd, IntPtr.Zero, (int)left, (int)top, (int)width, (int)height, SWP_NOZORDER | SWP_NOACTIVATE);
        }

        public enum ZOrderOption
        {
            Unchanged,
            Top,
            Bottom,
            TopMost,
            NoTopMost
        }

        public enum WindowVisibility
        {
            Unchanged,
            Show,
            Hide
        }

        public record struct SetWindowPosParam(
            bool ChangeSize = false,
            bool ChangePosition = false,
            ZOrderOption ZOrder = ZOrderOption.Unchanged,
            bool Activate = false,
            WindowVisibility Visibility = WindowVisibility.Unchanged,
            double Left = 0,
            double Top = 0,
            double Width = 0,
            double Height = 0);

        public static void SetWindowPos(Window window, SetWindowPosParam p)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            // WPF Window → HWND
            var hwnd = new WindowInteropHelper(window).EnsureHandle();

            // hWndInsertAfter の決定
            IntPtr insertAfter = p.ZOrder switch
            {
                ZOrderOption.Top => HWND_TOP,
                ZOrderOption.Bottom => HWND_BOTTOM,
                ZOrderOption.TopMost => HWND_TOPMOST,
                ZOrderOption.NoTopMost => HWND_NOTOPMOST,
                _ => IntPtr.Zero // Unchanged
            };

            // フラグ生成
            uint flags = 0;

            if (!p.ChangeSize)
                flags |= SWP_NOSIZE;

            if (!p.ChangePosition)
                flags |= SWP_NOMOVE;

            if (p.ZOrder == ZOrderOption.Unchanged)
                flags |= SWP_NOZORDER;

            if (!p.Activate)
                flags |= SWP_NOACTIVATE;

            flags |= p.Visibility switch
            {
                WindowVisibility.Show => SWP_SHOWWINDOW,
                WindowVisibility.Hide => SWP_HIDEWINDOW,
                _ => 0
            };

            // double → int（再現性のため Math.Round）
            int x = (int)Math.Round(p.Left);
            int y = (int)Math.Round(p.Top);
            int w = (int)Math.Round(p.Width);
            int h = (int)Math.Round(p.Height);

            SetWindowPos(hwnd, insertAfter, x, y, w, h, flags);
        }

    }
}

using Folminder.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Folminder.Platform
{
    public static class HotKeyHelper
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int WM_HOTKEY = 0x0312;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        public static void RegisterHotKey(Window window, int id, HotKey hotKey)
        {
            Debug.WriteLine($"RegisterHotKey. Alt: {hotKey.Alt}, Control: {hotKey.Control}, Shift: {hotKey.Shift}, Win: {hotKey.Win}, Key: {hotKey.Key}");
            var helper = new WindowInteropHelper(window);
            IntPtr hWnd = helper.Handle;
            uint mods = 0;
            mods |= hotKey.Alt ? MOD_ALT : 0;
            mods |= hotKey.Control ? MOD_CONTROL : 0;
            mods |= hotKey.Shift ? MOD_SHIFT : 0;
            mods |= hotKey.Win ? MOD_WIN : 0;
            uint vk = (uint)KeyInterop.VirtualKeyFromKey(hotKey.Key);
            Debug.WriteLine($"RegisterHotKey. mods: {mods:x4} vk: {vk:x4}");
            var r = RegisterHotKey(hWnd, id, mods, vk);
            Debug.WriteLine($"RegisterHotKey. r: {r}");
        }

        public static void UnregisterHotKey(Window window, int id)
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hWnd = helper.Handle;
            UnregisterHotKey(hWnd, id);
        }
    }
}

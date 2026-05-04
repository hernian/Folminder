using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Folminder.Platform
{
    public static class HotKeyHelper
    {
        public record HotKey(bool Alt, bool Control, bool Shift, bool Win, uint VKey);


        public static void RegisterHotKey(Window window, int id, HotKey hotKey)
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hWnd = helper.Handle;
            uint mods = 0;
            mods |= hotKey.Alt ? WinApiHelper.MOD_ALT : 0;
            mods |= hotKey.Control ? WinApiHelper.MOD_CONTROL : 0;
            mods |= hotKey.Shift ? WinApiHelper.MOD_SHIFT : 0;
            mods |= hotKey.Win ? WinApiHelper.MOD_WIN : 0;
            Debug.WriteLine($"RegisterHotKey. id: {id}, mods: {mods:x4}, key: {hotKey.VKey}");
            WinApiHelper.RegisterHotKey(hWnd, id, mods, hotKey.VKey);
        }

        public static void UnregisterHotKey(Window window, int id)
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hWnd = helper.Handle;
            WinApiHelper.UnregisterHotKey(hWnd, id);
        }
    }
}

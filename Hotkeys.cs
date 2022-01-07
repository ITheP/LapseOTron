using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Interop;

namespace LapseOTron
{
    // VK key Modifiers
    public enum KeyModifiers
    {
        None = 0x0,
        Alt = 0x1,
        Control = 0x2,
        Shift = 0x4,
        WinKey = 0x8,
        CapsLock = 0x14   // Capital
    }

    public class Hotkey
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public string Description { get; set; }
        public string KeyDescription { get; set; }
        public IntPtr Handle { get; set; }
        public int Id { get; set; }
        private uint Modifiers { get; set; }
        public Keys Key { get; set; }
        private int KeyHashCode { get; set; }

        public bool Alt { get; set; } = false;
        public bool Control { get; set; } = false;
        public bool Shift { get; set; } = false;
        public bool WinKey { get; set; } = false;
        public bool CapsLock { get; set; } = false;

        public int IntTag { get; set; }

        public void Register(IntPtr hwnd)
        {
            Handle = hwnd;
            KeyHashCode = Key.GetHashCode();
            KeyDescription = ((Alt ? "Alt+" : string.Empty) + (Control ? "Ctrl+" : string.Empty) + (Shift ? "Shift+" : string.Empty) + (WinKey ? "WinKey+" : string.Empty)) + (CapsLock ? "CapsLock+" : string.Empty) + Keys.GetName(typeof(Keys), Key);
            Modifiers = (uint)((Alt ? 0x01 : 0) + (Control ? 0x02 : 0) + (Shift ? 0x04 : 0) + (WinKey ? 0x08 : 0) + (CapsLock ? 0x14 : 0));
            RegisterHotKey(Handle, Id, Modifiers, (uint)KeyHashCode);
        }

        public void Unregister()
        {
            UnregisterHotKey(Handle, Id);
        }
    }

    public static class Hotkeys
    {
        public static List<Hotkey> RegisteredKeys = new List<Hotkey>();

        private static int HotkeyId { get; set; } = 0;

        private static HwndSource SrcWindow { get; set; }
        private static IntPtr HWnd { get; set; }

        public static void RegisterDefaults(MainWindow window)
        {
            HWnd = new WindowInteropHelper(window).Handle;
            SrcWindow = HwndSource.FromHwnd(HWnd);
            SrcWindow.AddHook(window.HotkeyHandler);

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.F1,
                Description = "Real time speed"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.F2,
                Description = "2x speed"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.F3,
                Description = "3x speed"
            });

          //  RegisteredKeys.Add(new Hotkey()
          //  {
          //      Alt = true,
          //      Id = HotkeyId++,
          //      Key = Keys.F4,
          //      Description = "4x speed"
          //  });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.F5,
                Description = "5x speed"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.F6,
                Description = "10x speed"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.F7,
                Description = "20x speed"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.F8,
                Description = "30x speed"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.F9,
                Description = "40x speed"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.F10,
                Description = "50x speed"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.F11,
                Description = "60x speed"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.F12,
                Description = "120x speed"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.Escape,
                Description = "Save snapshot"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.Z,
                Description = "Zoom In"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.X,
                Description = "Zoom Out"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.A,
                Description = "Zoom In Instantly"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.S,
                Description = "Zoom Out Instantly"
            });

            RegisteredKeys.Add(new Hotkey()
            {
                Alt = true,
                Id = HotkeyId++,
                Key = Keys.Escape,
                Description = "Save snapshot"
            });

            foreach (Hotkey key in RegisteredKeys)
                key.Register(HWnd);
        }

        public static void UnregisterDefaults()
        {
            foreach (Hotkey key in RegisteredKeys)
                key.Unregister();
        }

        //public static void RegisterHotkey(int hotkeyId, uint modifiers, uint vk )
        //{
        //    RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL, VK_CAPITAL); //CTRL + CAPS_LOCK
        //}

        //private static IntPtr HotkeyHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        //{
        //    const int WM_HOTKEY = 0x0312;

        //    if (msg == WM_HOTKEY)
        //    {
        //        int key = (((int)lParam >> 16) & 0xffff);
        //        int modifier = ((int)lParam & 0xffff);
        //        int hotkeyId = lParam.ToInt32();

        //        //switch (wParam.ToInt32())
        //        //{
        //        //    case 1:
        //        //        int vkey = (((int)lParam >> 16) & 0xFFFF);
        //        //        if (vkey == 2)
        //        //        {
        //        //            //handle global hot key here...
        //        //        }
        //        //        handled = true;
        //        //        break;
        //        //}

        //        if (modifier == (int)KeyModifiers.Alt)
        //        {
        //            Debug.Print($"KEY: {key}");
        //            //if (key == Keys.F1.GetHashCode() && )
        //            //{
        //            //    //if (this.WindowState == FormWindowState.Normal)
        //            //    //    MinimiseWindows();
        //            //    //else
        //            //    //    RestoreWindows();
        //            //    int i = 1;
        //            //}
        //        }
        //    }
        //    return IntPtr.Zero;
        //}
    }
}

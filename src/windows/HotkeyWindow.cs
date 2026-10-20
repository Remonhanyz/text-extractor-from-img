using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ArabicTextExtractor
{
    public class HotkeyWindow : NativeWindow, IDisposable
    {
        private SnipperContext context;
        private const int WM_HOTKEY = 0x0312;
        public const int HOTKEY_SNIP_ID = 9001;
        public const int HOTKEY_HIST_ID = 9002;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public HotkeyWindow(SnipperContext ctx)
        {
            context = ctx;
            CreateHandle(new CreateParams());
            RegisterCurrentHotkeys();
        }

        public bool RegisterCurrentHotkeys()
        {
            UnregisterHotKey(this.Handle, HOTKEY_SNIP_ID);
            UnregisterHotKey(this.Handle, HOTKEY_HIST_ID);

            AppConfig cfg = ConfigManager.GetConfig();

            // Try registering Snip hotkey
            bool okSnip = RegisterHotKey(this.Handle, HOTKEY_SNIP_ID, cfg.Snip.GetModifiers(), (int)cfg.Snip.Key);
            int errSnip = okSnip ? 0 : Marshal.GetLastWin32Error();

            // If user's configured shortcut is blocked by another app (e.g. old Alt+Shift+A collision)
            if (!okSnip)
            {
                // Try safe fallback: Ctrl+Shift+A
                HotkeyConfig safeFallback = new HotkeyConfig(false, true, true, false, Keys.A);
                if (cfg.Snip.ToString() != safeFallback.ToString())
                {
                    bool fallbackOk = RegisterHotKey(this.Handle, HOTKEY_SNIP_ID, safeFallback.GetModifiers(), (int)safeFallback.Key);
                    if (fallbackOk)
                    {
                        cfg.Snip = safeFallback;
                        ConfigManager.Save(cfg);
                        okSnip = true;
                        context.NotifyFallbackHotkey(cfg.Snip.ToString());
                    }
                }
            }

            // Try registering History hotkey
            bool okHist = RegisterHotKey(this.Handle, HOTKEY_HIST_ID, cfg.History.GetModifiers(), (int)cfg.History.Key);
            int errHist = okHist ? 0 : Marshal.GetLastWin32Error();

            if (!okHist)
            {
                HotkeyConfig safeFallbackHist = new HotkeyConfig(false, true, true, false, Keys.H);
                if (cfg.History.ToString() != safeFallbackHist.ToString())
                {
                    bool fallbackOk = RegisterHotKey(this.Handle, HOTKEY_HIST_ID, safeFallbackHist.GetModifiers(), (int)safeFallbackHist.Key);
                    if (fallbackOk)
                    {
                        cfg.History = safeFallbackHist;
                        ConfigManager.Save(cfg);
                        okHist = true;
                    }
                }
            }

            return okSnip && okHist;
        }

        public static bool TestRegister(HotkeyConfig cfg, out string error)
        {
            error = "";
            if (cfg == null || cfg.Key == Keys.None)
            {
                error = "Please select a valid key.";
                return false;
            }

            NativeWindow testWin = new NativeWindow();
            try
            {
                testWin.CreateHandle(new CreateParams());
                int testId = 9999;
                bool ok = RegisterHotKey(testWin.Handle, testId, cfg.GetModifiers(), (int)cfg.Key);
                int err = ok ? 0 : Marshal.GetLastWin32Error();

                if (ok)
                {
                    UnregisterHotKey(testWin.Handle, testId);
                    return true;
                }
                else
                {
                    if (err == 1409) // ERROR_HOTKEY_ALREADY_REGISTERED
                    {
                        error = "The shortcut [" + cfg.ToString() + "] is already in use by Windows or another application. Please choose another combination.";
                    }
                    else
                    {
                        error = "Windows could not register [" + cfg.ToString() + "] (Error code: " + err + ").";
                    }
                    return false;
                }
            }
            finally
            {
                try { testWin.DestroyHandle(); } catch { }
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_SNIP_ID)
                {
                    context.StartSnipping();
                }
                else if (id == HOTKEY_HIST_ID)
                {
                    context.OpenHistory();
                }
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            UnregisterHotKey(this.Handle, HOTKEY_SNIP_ID);
            UnregisterHotKey(this.Handle, HOTKEY_HIST_ID);
            DestroyHandle();
        }
    }
}

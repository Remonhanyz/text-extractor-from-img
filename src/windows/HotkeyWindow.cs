using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ArabicTextExtractor
{
    public class HotkeyWindow : NativeWindow, IDisposable
    {
        private SnipperContext context;
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_SNIP_ID = 9001;
        private const int HOTKEY_HIST_ID = 9002;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public HotkeyWindow(SnipperContext ctx)
        {
            context = ctx;
            CreateHandle(new CreateParams());
            RegisterCurrentHotkeys();
        }

        public void RegisterCurrentHotkeys()
        {
            UnregisterHotKey(this.Handle, HOTKEY_SNIP_ID);
            UnregisterHotKey(this.Handle, HOTKEY_HIST_ID);

            AppConfig cfg = ConfigManager.GetConfig();
            RegisterHotKey(this.Handle, HOTKEY_SNIP_ID, cfg.Snip.GetModifiers(), (int)cfg.Snip.Key);
            RegisterHotKey(this.Handle, HOTKEY_HIST_ID, cfg.History.GetModifiers(), (int)cfg.History.Key);
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

//+:cnd:noEmit
using System.Runtime.InteropServices;

namespace Boilerplate.Client.Windows.Infrastructure.Services;

/// <summary>
/// The lockdown a kiosk needs on top of <see cref="KioskModeManager"/>, which only takes the window full screen:
/// the key combinations that reach the desktop are swallowed, and the window refuses to close - the app's own exit
/// is the only way out. Ctrl+Alt+Del is out of reach of every user mode process - block it with the Windows policy
/// of the kiosk machine.
/// </summary>
public static partial class WindowsKioskGuard
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private delegate nint HookProc(int code, nint wParam, nint lParam);

    private static Form kioskForm = default!;

    // The field roots the delegate; a local would let the GC collect what the hook calls back into.
    private static HookProc hookProc = default!;

    /// <summary>
    /// Locks <paramref name="form"/> down for as long as the process runs; Windows drops the hook when it exits.
    /// </summary>
    public static void Apply(Form form)
    {
        kioskForm = form;
        hookProc = OnKey;
        SetWindowsHookExW(WH_KEYBOARD_LL, hookProc, 0, 0);

        // Windows itself still gets to shut down; only the person in front of the machine is refused.
        form.FormClosing += (_, e) => e.Cancel = e.CloseReason is CloseReason.UserClosing;
    }

    private static nint OnKey(int code, nint wParam, nint lParam)
    {
        // Only while the kiosk window is in front: a global block would take the desktop away from whatever else runs.
        if (code >= 0 && wParam is WM_KEYDOWN or WM_SYSKEYDOWN
            && GetForegroundWindow() == kioskForm.Handle && IsBlocked((Keys)Marshal.ReadInt32(lParam)))
            return 1;

        return CallNextHookEx(0, code, wParam, lParam);
    }

    private static bool IsBlocked(Keys key) => key switch
    {
        Keys.LWin or Keys.RWin => true,                                                  // the start menu and every Win+? combination
        Keys.Apps => true,                                                               // the context menu key
        Keys.Tab or Keys.Escape or Keys.F4 or Keys.Space when IsDown(Keys.Menu) => true, // Alt+Tab, Alt+Esc, Alt+F4, Alt+Space
        Keys.Escape when IsDown(Keys.ControlKey) => true,                                // Ctrl+Esc, Ctrl+Shift+Esc
        _ => false
    };

    // GetAsyncKeyState, not Control.ModifierKeys: in a low level hook the key has not reached the queue the latter reads.
    private static bool IsDown(Keys key) => (GetAsyncKeyState((int)key) & 0x8000) is not 0;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookExW(int idHook, HookProc lpfn, nint hMod, uint dwThreadId);

    // hhk is documented as ignored.
    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}

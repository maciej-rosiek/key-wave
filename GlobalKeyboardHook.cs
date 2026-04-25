using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LenovoRipple.Input;

/// <summary>
/// Low-level Win32 keyboard hook (WH_KEYBOARD_LL). Fires <see cref="KeyPressed"/> on the
/// thread that installed the hook (typically the WPF UI thread, since it has a message pump).
///
/// IMPORTANT: handlers must return quickly. Do NOT do LampArray work directly in the
/// callback — kick off a Task and let it run on the SynchronizationContext.
/// </summary>
public sealed class GlobalKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private IntPtr _hookId = IntPtr.Zero;
    private readonly LowLevelKeyboardProc _proc;
    private bool _disposed;

    public event Action<int>? KeyPressed;

    public GlobalKeyboardHook()
    {
        _proc = HookCallback;
        // Pin the delegate's containing module via GetModuleHandle of the running process.
        // For WH_KEYBOARD_LL with dwThreadId=0, Windows accepts any valid module handle.
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
        if (_hookId == IntPtr.Zero)
        {
            int err = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"SetWindowsHookEx failed (Win32 error {err}).");
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                try { KeyPressed?.Invoke(vkCode); } catch { /* never let an exception escape */ }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }
}

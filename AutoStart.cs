using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace KeyWave;

/// <summary>
/// Manages a per-user "run at login" entry under
/// HKCU\Software\Microsoft\Windows\CurrentVersion\Run.
/// When enabled, the entry includes the --hidden flag so the app starts in tray.
/// </summary>
internal static class AutoStart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "KeyWave";
    private const string HiddenFlag = "--hidden";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(ValueName) as string;
        return !string.IsNullOrWhiteSpace(value);
    }

    public static void Enable()
    {
        var exe = GetExecutablePath();
        if (string.IsNullOrEmpty(exe)) return;
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                       ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (key == null) return;
        // Quote the exe path so spaces in Program Files etc. don't break it.
        key.SetValue(ValueName, $"\"{exe}\" {HiddenFlag}", RegistryValueKind.String);
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static string? GetExecutablePath()
    {
        // Prefer Process.MainModule (the actual .exe), not Assembly.Location (which is the
        // managed dll for single-file or framework-dependent deployments).
        try
        {
            var path = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(path)) return path;
        }
        catch { }
        return Environment.ProcessPath;
    }
}

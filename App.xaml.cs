using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using LenovoRipple.Input;
using LenovoRipple.Lighting;
using WinColor = Windows.UI.Color;
using WinFormsApp = System.Windows.Forms.Application;

namespace LenovoRipple;

public partial class App : System.Windows.Application
{
    private LampArrayController? _controller;
    private GlobalKeyboardHook? _hook;
    private NotifyIcon? _tray;
    private ToolStripMenuItem? _autostartItem;
    private readonly System.Collections.Generic.List<ToolStripMenuItem> _themeItems = new();
    private ColorTheme _activeTheme = ColorThemes.Default;
    private MainWindow? _window;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        bool startHidden = e.Args.Any(a => a.Equals("--hidden", StringComparison.OrdinalIgnoreCase));

        _activeTheme = ColorThemes.Default;
        _controller = new LampArrayController
        {
            Theme = _activeTheme,
            FadeMs = 200,
        };

        _window = new MainWindow(_controller);
        _controller.DeviceChanged += _window.OnDeviceChanged;

        // Always register the on-screen simulator. Real LampArrays attach on top
        // of it as the device watcher discovers them.
        _controller.AddSurface(_window.SimulatorSurface);
        _controller.Start();
        _window.ShowTheme(_activeTheme.Name);

        if (!startHidden) _window.Show();

        _hook = new GlobalKeyboardHook();
        _hook.KeyPressed += OnKeyPressed;

        BuildTrayIcon();
    }

    private void OnKeyPressed(int vkCode)
    {
        var key = (Windows.System.VirtualKey)vkCode;
        _window?.ShowLastKey($"{key} ({vkCode})");
        // Hook callback runs on the UI thread; the async fade yields immediately
        // on its first await, so the hook returns promptly.
        _ = _controller!.FlashKeyAsync(key);
    }

    private void BuildTrayIcon()
    {
        var menu = new ContextMenuStrip();

        var showItem = new ToolStripMenuItem("Show window");
        showItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(showItem);

        var reapplyItem = new ToolStripMenuItem("Re-apply base color");
        reapplyItem.Click += (_, _) => _controller?.ApplyBaseColorAll();
        menu.Items.Add(reapplyItem);

        var themeItem = new ToolStripMenuItem("Theme");
        foreach (var theme in ColorThemes.Presets)
        {
            var item = new ToolStripMenuItem(theme.Name) { Tag = theme };
            item.Checked = ReferenceEquals(theme, _activeTheme);
            item.Click += (_, _) => ApplyTheme((ColorTheme)item.Tag!);
            _themeItems.Add(item);
            themeItem.DropDownItems.Add(item);
        }
        menu.Items.Add(themeItem);

        menu.Items.Add(new ToolStripSeparator());

        _autostartItem = new ToolStripMenuItem("Start with Windows")
        {
            Checked = AutoStart.IsEnabled(),
            CheckOnClick = false,
        };
        _autostartItem.Click += (_, _) =>
        {
            if (AutoStart.IsEnabled()) AutoStart.Disable();
            else AutoStart.Enable();
            _autostartItem.Checked = AutoStart.IsEnabled();
        };
        menu.Items.Add(_autostartItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApp();
        menu.Items.Add(exitItem);

        _tray = new NotifyIcon
        {
            Icon = LoadAppIcon(),
            Visible = true,
            Text = "Lenovo Ripple",
            ContextMenuStrip = menu,
        };
        _tray.DoubleClick += (_, _) => ShowMainWindow();
    }

    private static System.Drawing.Icon LoadAppIcon()
    {
        try
        {
            var sri = System.Windows.Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/AppIcon.png", UriKind.Absolute));
            if (sri == null) return SystemIcons.Application;
            using var bmp = new System.Drawing.Bitmap(sri.Stream);
            // Bitmap.GetHicon returns a transient HICON — Icon.FromHandle wraps it.
            // The OS releases the handle when the process exits.
            return System.Drawing.Icon.FromHandle(bmp.GetHicon());
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    private void ApplyTheme(ColorTheme theme)
    {
        if (_controller == null) return;
        _activeTheme = theme;
        _controller.Theme = theme;
        _controller.ApplyBaseColorAll();
        foreach (var item in _themeItems)
            item.Checked = ReferenceEquals(item.Tag, theme);
        _window?.ShowTheme(theme.Name);
    }

    private void ShowMainWindow()
    {
        if (_window == null) return;
        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
            _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private void ExitApp()
    {
        if (_window != null) _window.RequestActualClose();
        Shutdown();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _hook?.Dispose();
        _controller?.Dispose();
        if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }
    }
}

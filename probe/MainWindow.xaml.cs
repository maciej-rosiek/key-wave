using System;
using System.Windows;
using Windows.Devices.Enumeration;
using Windows.Devices.Lights;

namespace LenovoRipple.Probe;

public partial class MainWindow : Window
{
    private DeviceWatcher? _watcher;
    private int _addedCount;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SetStatus("watching for LampArray devices...");
        Log($"OS: {Environment.OSVersion.VersionString}");
        Log($"Process: {Environment.ProcessPath}");

        string selector;
        try
        {
            selector = LampArray.GetDeviceSelector();
        }
        catch (Exception ex)
        {
            Log($"FATAL: LampArray.GetDeviceSelector threw {ex.GetType().Name}: {ex.Message}");
            SetStatus("error — see log");
            return;
        }

        Log($"Selector: {selector}");
        Log("---");

        try
        {
            _watcher = DeviceInformation.CreateWatcher(selector);
            _watcher.Added += OnAdded;
            _watcher.Removed += OnRemoved;
            _watcher.Updated += OnUpdated;
            _watcher.EnumerationCompleted += OnEnumerationCompleted;
            _watcher.Stopped += OnStopped;
            _watcher.Start();
        }
        catch (Exception ex)
        {
            Log($"FATAL: CreateWatcher/Start threw {ex.GetType().Name}: {ex.Message}");
            SetStatus("error — see log");
        }
    }

    private async void OnAdded(DeviceWatcher sender, DeviceInformation info)
    {
        int idx = ++_addedCount;
        Log($"[#{idx} Added]");
        Log($"  Id    : {info.Id}");
        Log($"  Name  : {info.Name}");
        Log($"  Kind  : {info.Kind}");
        Log($"  IsEnabled : {info.IsEnabled}");

        try
        {
            var lamp = await LampArray.FromIdAsync(info.Id);
            if (lamp == null)
            {
                Log("  -> LampArray.FromIdAsync returned null (could not bind).");
                return;
            }

            Log($"  LampArrayKind     : {lamp.LampArrayKind}");
            Log($"  LampCount         : {lamp.LampCount}");
            Log($"  IsConnected       : {lamp.IsConnected}");
            Log($"  IsEnabled         : {lamp.IsEnabled}");
            Log($"  MinUpdateInterval : {lamp.MinUpdateInterval}");
            Log($"  BoundingBox       : {lamp.BoundingBox}");
            Log($"  HardwareVendorId  : 0x{lamp.HardwareVendorId:X4}");
            Log($"  HardwareProductId : 0x{lamp.HardwareProductId:X4}");
            Log("---");
        }
        catch (Exception ex)
        {
            Log($"  -> FromIdAsync threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
        => Log($"[Removed] Id={update.Id}");

    private void OnUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
        => Log($"[Updated] Id={update.Id}");

    private void OnEnumerationCompleted(DeviceWatcher sender, object args)
    {
        Log($"[EnumerationCompleted] {_addedCount} device(s) seen so far. Watcher continues for hot-plug.");
        SetStatus(_addedCount == 0
            ? "no LampArray found — check Settings → Personalization → Dynamic Lighting"
            : $"{_addedCount} device(s) found");
    }

    private void OnStopped(DeviceWatcher sender, object args)
        => Log("[Stopped]");

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_watcher != null)
        {
            try
            {
                _watcher.Added -= OnAdded;
                _watcher.Removed -= OnRemoved;
                _watcher.Updated -= OnUpdated;
                _watcher.EnumerationCompleted -= OnEnumerationCompleted;
                _watcher.Stopped -= OnStopped;
                if (_watcher.Status == DeviceWatcherStatus.Started ||
                    _watcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    _watcher.Stop();
                }
            }
            catch { }
        }
    }

    private void Log(string line)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            OutputBox.AppendText(line + Environment.NewLine);
            OutputBox.ScrollToEnd();
        }));
    }

    private void SetStatus(string text)
    {
        Dispatcher.BeginInvoke(new Action(() => StatusText.Text = text));
    }
}

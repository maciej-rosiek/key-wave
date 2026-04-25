using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Lights;
using Windows.System;
using WinColor = Windows.UI.Color;

namespace KeyWave.Lighting;

/// <summary>
/// Manages a set of <see cref="ILampSurface"/>s and orchestrates flash+fade reactions.
/// Owns its own DeviceWatcher to discover real LampArrays, but also accepts surfaces
/// added manually (used for the simulator panel).
/// </summary>
public sealed class LampArrayController : IDisposable
{
    private readonly object _surfacesLock = new();
    private readonly List<ILampSurface> _surfaces = new();
    private readonly Dictionary<string, RealLampSurface> _realById = new();

    // Per-zone cancellation tokens. A zone "belongs to" whichever flash most recently
    // claimed it; the previous owner sees its CTS cancelled and stops touching that zone.
    private readonly ConcurrentDictionary<(ILampSurface surface, int index), CancellationTokenSource> _zoneOwners = new();

    private DeviceWatcher? _watcher;
    private bool _disposed;

    public ColorTheme Theme { get; set; } = ColorThemes.Default;
    public Effect Effect { get; set; } = Effects.Default;
    public EffectParameters EffectParameters { get; set; } = EffectParameters.Default;
    public int FadeMs { get; set; } = 200;
    public int FadeSteps { get; set; } = 10;

    public event Action<ILampSurface, bool>? DeviceChanged; // (surface, added)

    public IReadOnlyList<ILampSurface> Snapshot()
    {
        lock (_surfacesLock) return _surfaces.ToArray();
    }

    public int RealDeviceCount
    {
        get { lock (_surfacesLock) return _realById.Count; }
    }

    public int TotalLampCount
    {
        get { lock (_surfacesLock) return _surfaces.Sum(s => s.LampCount); }
    }

    public void AddSurface(ILampSurface surface)
    {
        lock (_surfacesLock) _surfaces.Add(surface);
        ApplyBaseColor(surface);
        DeviceChanged?.Invoke(surface, true);
    }

    public void Start()
    {
        if (_watcher != null) return;
        _watcher = DeviceInformation.CreateWatcher(LampArray.GetDeviceSelector());
        _watcher.Added += OnAdded;
        _watcher.Removed += OnRemoved;
        _watcher.Start();
    }

    private async void OnAdded(DeviceWatcher sender, DeviceInformation info)
    {
        try
        {
            var lamp = await LampArray.FromIdAsync(info.Id);
            if (lamp == null) return;
            var surface = new RealLampSurface(lamp);
            lock (_surfacesLock)
            {
                _realById[info.Id] = surface;
                _surfaces.Add(surface);
            }
            ApplyBaseColor(surface);
            DeviceChanged?.Invoke(surface, true);
        }
        catch
        {
            // Swallow: device may have been removed or access denied. Watcher continues.
        }
    }

    private void OnRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        RealLampSurface? removed = null;
        lock (_surfacesLock)
        {
            if (_realById.TryGetValue(update.Id, out removed))
            {
                _realById.Remove(update.Id);
                _surfaces.Remove(removed);
            }
        }
        if (removed != null) DeviceChanged?.Invoke(removed, false);
    }

    public void ApplyBaseColorAll()
    {
        foreach (var s in Snapshot()) ApplyBaseColor(s);
    }

    public void ApplyBaseColor(ILampSurface surface)
    {
        try
        {
            int total = surface.LampCount;
            if (Theme.IsUniform || total <= 0)
            {
                surface.SetColor(Theme.BaseColorFor(0, total));
                return;
            }
            var colors = new WinColor[total];
            var indices = new int[total];
            for (int i = 0; i < total; i++)
            {
                indices[i] = i;
                colors[i] = Theme.BaseColorFor(i, total);
            }
            surface.SetColorsForIndices(colors, indices);
        }
        catch { /* surface may have detached */ }
    }

    public Task FlashKeyAsync(VirtualKey key)
    {
        var surfaces = Snapshot();
        var effect = Effect;
        var tasks = new List<Task>(surfaces.Count);
        foreach (var s in surfaces)
        {
            int[] indices;
            try { indices = s.GetIndicesForKey(key); }
            catch { continue; }
            if (indices == null || indices.Length == 0) continue;
            tasks.Add(effect.RunAsync(this, s, indices, key));
        }
        return tasks.Count == 0 ? Task.CompletedTask : Task.WhenAll(tasks);
    }

    /// <summary>
    /// Effects call this to take ownership of a set of zones. Any prior owner
    /// of those zones gets its CTS cancelled, which makes its in-flight loop
    /// stop touching the zones it lost. Returns one CTS per index.
    /// </summary>
    internal CancellationTokenSource[] ClaimZones(ILampSurface surface, int[] indices)
    {
        var mine = new CancellationTokenSource[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            var cts = new CancellationTokenSource();
            var key = (surface, indices[i]);
            if (_zoneOwners.TryGetValue(key, out var prev)) prev.Cancel();
            _zoneOwners[key] = cts;
            mine[i] = cts;
        }
        return mine;
    }

    /// <summary>Release zones the effect still owns. Safe to call in a finally.</summary>
    internal void ReleaseZones(ILampSurface surface, int[] indices, CancellationTokenSource[] mine)
    {
        for (int i = 0; i < indices.Length; i++)
        {
            var pair = new KeyValuePair<(ILampSurface, int), CancellationTokenSource>(
                (surface, indices[i]), mine[i]);
            _zoneOwners.TryRemove(pair);
            mine[i].Dispose();
        }
    }

    /// <summary>
    /// Push <paramref name="colors"/> to the surface, but only for indices whose
    /// CTS hasn't been cancelled (i.e. ones we still own). Returns false if we
    /// own no zones — caller should bail out of its animation loop.
    /// </summary>
    internal static bool WriteActive(ILampSurface surface, int[] indices, CancellationTokenSource[] owners, WinColor[] colors)
    {
        var activeIdx = new List<int>(indices.Length);
        var activeColors = new List<WinColor>(indices.Length);
        for (int i = 0; i < indices.Length; i++)
        {
            if (!owners[i].IsCancellationRequested)
            {
                activeIdx.Add(indices[i]);
                activeColors.Add(colors[i]);
            }
        }
        if (activeIdx.Count == 0) return false;
        try { surface.SetColorsForIndices(activeColors.ToArray(), activeIdx.ToArray()); }
        catch { /* surface may have detached mid-fade */ }
        return true;
    }

    internal static WinColor Lerp(WinColor a, WinColor b, float t)
    {
        byte L(byte x, byte y) => (byte)(x + (y - x) * t);
        return WinColor.FromArgb(255, L(a.R, b.R), L(a.G, b.G), L(a.B, b.B));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            if (_watcher != null)
            {
                _watcher.Added -= OnAdded;
                _watcher.Removed -= OnRemoved;
                if (_watcher.Status == DeviceWatcherStatus.Started ||
                    _watcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    _watcher.Stop();
                }
            }
        }
        catch { }
    }
}

using Windows.Devices.Lights;
using Windows.System;
using WinColor = Windows.UI.Color;

namespace KeyWave.Lighting;

/// <summary>Adapter over the WinRT LampArray.</summary>
public sealed class RealLampSurface : ILampSurface
{
    private readonly LampArray _lamp;

    public RealLampSurface(LampArray lamp)
    {
        _lamp = lamp;
    }

    public string DeviceId => _lamp.DeviceId;
    public string DisplayName => _lamp.DeviceId;
    public int LampCount => _lamp.LampCount;

    // The LampArray API doesn't expose per-zone physical positions, so we
    // approximate the grid: 24-zone keyboards are typically laid out roughly
    // 6 rows × 4 columns; for other sizes, fall back to a single row.
    public int Rows => _lamp.LampCount == 24 ? 6 : 1;
    public int Cols => _lamp.LampCount == 24 ? 4 : _lamp.LampCount;

    public int[] GetIndicesForKey(VirtualKey key) => _lamp.GetIndicesForKey(key);
    public void SetColor(WinColor color) => _lamp.SetColor(color);
    public void SetColorsForIndices(WinColor[] colors, int[] indices)
        => _lamp.SetColorsForIndices(colors, indices);
}

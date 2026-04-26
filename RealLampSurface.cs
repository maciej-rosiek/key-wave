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

    // Real LampArray keyboards expose zones as a single horizontal strip —
    // index 0 is the left edge, index N-1 the right edge. The Ripple effect
    // uses this to propagate left/right from whatever zone(s) the pressed
    // key lights up.
    public int Rows => 1;
    public int Cols => _lamp.LampCount;

    public int[] GetIndicesForKey(VirtualKey key) => _lamp.GetIndicesForKey(key);
    public void SetColor(WinColor color) => _lamp.SetColor(color);
    public void SetColorsForIndices(WinColor[] colors, int[] indices)
        => _lamp.SetColorsForIndices(colors, indices);
}

using Windows.Devices.Lights;
using Windows.System;
using WinColor = Windows.UI.Color;

namespace LenovoRipple.Lighting;

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
    public int[] GetIndicesForKey(VirtualKey key) => _lamp.GetIndicesForKey(key);
    public void SetColor(WinColor color) => _lamp.SetColor(color);
    public void SetColorsForIndices(WinColor[] colors, int[] indices)
        => _lamp.SetColorsForIndices(colors, indices);
}

using Windows.System;
using WinColor = Windows.UI.Color;

namespace LenovoRipple.Lighting;

/// <summary>
/// Common abstraction over a real LampArray and the on-screen simulator,
/// so the controller can drive both with one code path.
/// </summary>
public interface ILampSurface
{
    string DisplayName { get; }
    int LampCount { get; }
    int[] GetIndicesForKey(VirtualKey key);
    void SetColor(WinColor color);
    void SetColorsForIndices(WinColor[] colors, int[] indices);
}

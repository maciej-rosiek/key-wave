using Windows.System;
using WinColor = Windows.UI.Color;

namespace KeyWave.Lighting;

/// <summary>
/// Common abstraction over a real LampArray and the on-screen simulator,
/// so the controller can drive both with one code path.
/// </summary>
public interface ILampSurface
{
    string DisplayName { get; }
    int LampCount { get; }

    /// <summary>Logical grid rows. Effects like Ripple use this to know zone neighbors.</summary>
    int Rows { get; }

    /// <summary>Logical grid columns. Zone index = row * Cols + col.</summary>
    int Cols { get; }

    int[] GetIndicesForKey(VirtualKey key);
    void SetColor(WinColor color);
    void SetColorsForIndices(WinColor[] colors, int[] indices);
}

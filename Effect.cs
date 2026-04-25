using System.Threading.Tasks;
using KeyWave.Lighting;
using Windows.System;

namespace KeyWave;

/// <summary>
/// What happens when a key is pressed: just flash that key's zone, or trigger
/// a ripple across neighbors, etc. Effects are independent of color choice
/// (that's <see cref="ColorTheme"/>'s job) and reuse the controller's per-zone
/// fade-cancellation bookkeeping so a new keypress cleanly takes over from
/// any in-flight effect on the same zones.
/// </summary>
public abstract class Effect
{
    public abstract string Name { get; }
    public abstract Task RunAsync(LampArrayController controller, ILampSurface surface, int[] pressedIndices, VirtualKey key);
}

public sealed record EffectParameters(
    int Distance,
    int Width,
    int SpeedMs,
    bool WallBounce)
{
    public static EffectParameters Default => new(Distance: 3, Width: 1, SpeedMs: 60, WallBounce: false);
}

public static class Effects
{
    public static readonly Effect[] All = new Effect[]
    {
        new FlashAndFadeEffect(),
        new RippleEffect(),
    };

    public static Effect Default => All[0];
}

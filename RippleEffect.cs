using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeyWave.Lighting;
using Windows.System;
using WinColor = Windows.UI.Color;

namespace KeyWave;

/// <summary>
/// A wave that propagates outward from the pressed zone through neighboring
/// zones using Manhattan distance on the surface's logical grid.
///
/// Parameters (from <see cref="EffectParameters"/>):
///   Distance   = how many zone-steps the wave travels outward.
///   Width      = thickness of the wave at any given moment, in zones.
///   SpeedMs    = ms between radius advances.
///   WallBounce = if true, the wave returns inward after reaching Distance.
/// </summary>
public sealed class RippleEffect : Effect
{
    public override string Name => "Ripple";

    public override async Task RunAsync(LampArrayController controller, ILampSurface surface, int[] pressedIndices, VirtualKey key)
    {
        if (pressedIndices.Length == 0) return;

        int rows = Math.Max(1, surface.Rows);
        int cols = Math.Max(1, surface.Cols);
        int total = surface.LampCount;
        // If the surface didn't expose a sensible grid, fall back to flash-only.
        if (rows * cols != total)
        {
            await new FlashAndFadeEffect().RunAsync(controller, surface, pressedIndices, key);
            return;
        }

        // Center the ripple on the average position of the pressed zones —
        // handles multi-zone keys like Space gracefully.
        float cr = 0, cc = 0;
        foreach (var idx in pressedIndices) { cr += idx / cols; cc += idx % cols; }
        cr /= pressedIndices.Length;
        cc /= pressedIndices.Length;

        var p = controller.EffectParameters;
        int maxR = Math.Max(1, p.Distance);
        int width = Math.Max(1, p.Width);
        int speed = Math.Max(1, p.SpeedMs);
        bool bounce = p.WallBounce;
        float halfWidth = width / 2f + 0.5f;

        // Precompute distances per zone and gather the ones the wave will ever
        // touch. Claiming only those (not all 24) keeps non-rippled zones in
        // their current ambient state.
        var zoneDistances = new float[total];
        var touched = new List<int>(total);
        for (int z = 0; z < total; z++)
        {
            int r = z / cols, c = z % cols;
            float d = Math.Abs(r - cr) + Math.Abs(c - cc);
            zoneDistances[z] = d;
            if (d <= maxR + halfWidth) touched.Add(z);
        }
        if (touched.Count == 0) return;

        var indices = touched.ToArray();
        var owners = controller.ClaimZones(surface, indices);

        // Resolve theme colors per zone once per run.
        var baseColors  = new WinColor[indices.Length];
        var flashColors = new WinColor[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            baseColors[i]  = controller.Theme.BaseColorFor(indices[i], total);
            flashColors[i] = controller.Theme.FlashColorFor(indices[i], total);
        }

        // Total step count: outward to maxR + width tail; with bounce, return back.
        int outSteps = maxR + width;
        int totalSteps = bounce ? outSteps * 2 : outSteps;

        var stepColors = new WinColor[indices.Length];
        try
        {
            for (int step = 0; step <= totalSteps; step++)
            {
                float radius = bounce
                    ? (step <= outSteps ? step : 2 * outSteps - step)
                    : step;

                for (int i = 0; i < indices.Length; i++)
                {
                    float d = zoneDistances[indices[i]];
                    float dist = Math.Abs(d - radius);
                    float intensity = dist <= halfWidth
                        ? Math.Max(0f, 1f - dist / halfWidth)
                        : 0f;
                    stepColors[i] = LampArrayController.Lerp(baseColors[i], flashColors[i], intensity);
                }

                if (!LampArrayController.WriteActive(surface, indices, owners, stepColors)) return;
                await Task.Delay(speed).ConfigureAwait(true);
            }

            // After the wave finishes, settle every touched zone at base.
            LampArrayController.WriteActive(surface, indices, owners, baseColors);
        }
        finally
        {
            controller.ReleaseZones(surface, indices, owners);
        }
    }
}

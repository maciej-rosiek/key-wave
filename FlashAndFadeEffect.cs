using System;
using System.Threading;
using System.Threading.Tasks;
using KeyWave.Lighting;
using Windows.System;
using WinColor = Windows.UI.Color;

namespace KeyWave;

/// <summary>
/// The original behavior: flash just the pressed key's zone(s) to the theme's
/// flash color, then linearly fade back to the base color.
/// </summary>
public sealed class FlashAndFadeEffect : Effect
{
    public override string Name => "Flash & Fade";

    public override async Task RunAsync(LampArrayController controller, ILampSurface surface, int[] pressedIndices, VirtualKey key)
    {
        if (pressedIndices.Length == 0) return;

        var owners = controller.ClaimZones(surface, pressedIndices);
        try
        {
            int total = surface.LampCount;
            var flashColors = new WinColor[pressedIndices.Length];
            var baseColors  = new WinColor[pressedIndices.Length];
            for (int i = 0; i < pressedIndices.Length; i++)
            {
                flashColors[i] = controller.Theme.FlashColorFor(pressedIndices[i], total);
                baseColors[i]  = controller.Theme.BaseColorFor(pressedIndices[i], total);
            }

            LampArrayController.WriteActive(surface, pressedIndices, owners, flashColors);

            int steps = Math.Max(1, controller.FadeSteps);
            int stepDelay = Math.Max(1, controller.FadeMs / steps);
            var stepColors = new WinColor[pressedIndices.Length];
            for (int s = 1; s <= steps; s++)
            {
                await Task.Delay(stepDelay).ConfigureAwait(true);
                float t = (float)s / steps;
                for (int i = 0; i < pressedIndices.Length; i++)
                    stepColors[i] = LampArrayController.Lerp(flashColors[i], baseColors[i], t);
                if (!LampArrayController.WriteActive(surface, pressedIndices, owners, stepColors)) return;
            }
        }
        finally
        {
            controller.ReleaseZones(surface, pressedIndices, owners);
        }
    }
}

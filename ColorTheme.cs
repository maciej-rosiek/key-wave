using System;
using WinColor = Windows.UI.Color;

namespace KeyWave;

/// <summary>
/// A color theme decides what each zone shows at rest (base) and what color it
/// flashes to on a keypress. Themes can be uniform (same color everywhere) or
/// per-zone (e.g. a rainbow gradient across the keyboard).
/// </summary>
public abstract class ColorTheme
{
    public string Name { get; }
    protected ColorTheme(string name) { Name = name; }

    /// <summary>True if BaseColorFor / FlashColorFor return the same color for every zone.</summary>
    public virtual bool IsUniform => false;

    public abstract WinColor BaseColorFor(int zone, int totalZones);
    public abstract WinColor FlashColorFor(int zone, int totalZones);

    public override string ToString() => Name;
}

/// <summary>Same base color and same flash color for every zone.</summary>
public sealed class SolidTheme : ColorTheme
{
    public WinColor BaseColor { get; }
    public WinColor FlashColor { get; }

    public SolidTheme(string name, WinColor baseColor, WinColor flashColor) : base(name)
    {
        BaseColor = baseColor;
        FlashColor = flashColor;
    }

    public override bool IsUniform => true;
    public override WinColor BaseColorFor(int zone, int total) => BaseColor;
    public override WinColor FlashColorFor(int zone, int total) => FlashColor;
}

/// <summary>
/// Each zone gets a different hue spread evenly around the color wheel.
/// Base is dim/saturated, flash is the same hue at full brightness.
/// </summary>
public sealed class RainbowTheme : ColorTheme
{
    private readonly float _baseSaturation;
    private readonly float _baseValue;

    public RainbowTheme(string name, float baseSaturation = 0.85f, float baseValue = 0.30f) : base(name)
    {
        _baseSaturation = baseSaturation;
        _baseValue = baseValue;
    }

    public override WinColor BaseColorFor(int zone, int total)
    {
        float h = total <= 0 ? 0 : (float)zone / total;
        return HsvToRgb(h, _baseSaturation, _baseValue);
    }

    public override WinColor FlashColorFor(int zone, int total)
    {
        float h = total <= 0 ? 0 : (float)zone / total;
        return HsvToRgb(h, 1f, 1f);
    }

    private static WinColor HsvToRgb(float h, float s, float v)
    {
        h = (h % 1f + 1f) % 1f * 6f;
        int i = (int)Math.Floor(h);
        float f = h - i;
        float p = v * (1 - s);
        float q = v * (1 - s * f);
        float t = v * (1 - s * (1 - f));
        float r, g, b;
        switch (i % 6)
        {
            case 0:  r = v; g = t; b = p; break;
            case 1:  r = q; g = v; b = p; break;
            case 2:  r = p; g = v; b = t; break;
            case 3:  r = p; g = q; b = v; break;
            case 4:  r = t; g = p; b = v; break;
            default: r = v; g = p; b = q; break;
        }
        return WinColor.FromArgb(255,
            (byte)Math.Clamp(r * 255f, 0, 255),
            (byte)Math.Clamp(g * 255f, 0, 255),
            (byte)Math.Clamp(b * 255f, 0, 255));
    }
}

public static class ColorThemes
{
    public static readonly ColorTheme[] Presets = new ColorTheme[]
    {
        new SolidTheme("Cyan on Navy",
            WinColor.FromArgb(255,   0,   0,  64),
            WinColor.FromArgb(255,   0, 255, 255)),
        new SolidTheme("White on Off",
            WinColor.FromArgb(255,   6,   6,  10),
            WinColor.FromArgb(255, 255, 255, 255)),
        new SolidTheme("Amber on Dark",
            WinColor.FromArgb(255,  30,  12,   0),
            WinColor.FromArgb(255, 255, 170,   0)),
        new SolidTheme("Lime on Forest",
            WinColor.FromArgb(255,   0,  32,   0),
            WinColor.FromArgb(255,   0, 255,  64)),
        new SolidTheme("Red Alert",
            WinColor.FromArgb(255,  32,   0,   0),
            WinColor.FromArgb(255, 255,  32,  32)),
        new SolidTheme("Magenta Pop",
            WinColor.FromArgb(255,  32,   0,  32),
            WinColor.FromArgb(255, 255,   0, 200)),
        new SolidTheme("Ice Blue",
            WinColor.FromArgb(255,   8,  16,  32),
            WinColor.FromArgb(255, 180, 220, 255)),
        new RainbowTheme("Rainbow"),
    };

    public static ColorTheme Default => Presets[0];
}

using WinColor = Windows.UI.Color;

namespace LenovoRipple;

public sealed record ColorTheme(string Name, WinColor BaseColor, WinColor FlashColor);

public static class ColorThemes
{
    public static readonly ColorTheme[] Presets = new[]
    {
        new ColorTheme("Cyan on Navy",
            WinColor.FromArgb(255,   0,   0,  64),
            WinColor.FromArgb(255,   0, 255, 255)),
        new ColorTheme("White on Off",
            WinColor.FromArgb(255,   6,   6,  10),
            WinColor.FromArgb(255, 255, 255, 255)),
        new ColorTheme("Amber on Dark",
            WinColor.FromArgb(255,  30,  12,   0),
            WinColor.FromArgb(255, 255, 170,   0)),
        new ColorTheme("Lime on Forest",
            WinColor.FromArgb(255,   0,  32,   0),
            WinColor.FromArgb(255,   0, 255,  64)),
        new ColorTheme("Red Alert",
            WinColor.FromArgb(255,  32,   0,   0),
            WinColor.FromArgb(255, 255,  32,  32)),
        new ColorTheme("Magenta Pop",
            WinColor.FromArgb(255,  32,   0,  32),
            WinColor.FromArgb(255, 255,   0, 200)),
        new ColorTheme("Ice Blue",
            WinColor.FromArgb(255,   8,  16,  32),
            WinColor.FromArgb(255, 180, 220, 255)),
    };

    public static ColorTheme Default => Presets[0];
}

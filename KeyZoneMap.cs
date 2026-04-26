using System;
using System.Collections.Generic;
using Windows.System;

namespace KeyWave.Lighting.Simulator;

/// <summary>
/// Static fallback mapping from VirtualKey to one of 24 zones, arranged as a
/// single horizontal strip (matches what real LampArray keyboards actually
/// expose — index 0 is the left edge, index 23 the right edge).
/// Used by the simulator so we can develop without hardware.
///
/// Zones approximate the horizontal position of each key column on a standard
/// QWERTY laptop keyboard. Real hardware uses Windows's GetIndicesForKey,
/// which is authoritative — this map is only for the on-screen simulator.
/// </summary>
internal static class KeyZoneMap
{
    public const int Rows = 1;
    public const int Cols = 24;
    public const int ZoneCount = Rows * Cols;

    private static readonly Dictionary<VirtualKey, int[]> _map = Build();

    public static int[] GetIndicesForKey(VirtualKey key)
        => _map.TryGetValue(key, out var v) ? v : Array.Empty<int>();

    private static Dictionary<VirtualKey, int[]> Build()
    {
        var m = new Dictionary<VirtualKey, int[]>();

        void Set(int zone, params VirtualKey[] keys)
        {
            foreach (var k in keys) m[k] = new[] { zone };
        }
        void SetRaw(int zone, params int[] vks)
        {
            foreach (var v in vks) m[(VirtualKey)v] = new[] { zone };
        }

        // Each "column" of a typical 14-column QWERTY layout maps to one zone
        // along the 24-zone horizontal strip. Modifier keys at the row edges
        // share the leftmost/rightmost zones.

        // Col 0 — left edge (Esc / ` / Tab / Caps / Shift / Ctrl)
        Set(0, VirtualKey.Escape, VirtualKey.Tab, VirtualKey.CapitalLock,
               VirtualKey.Shift, VirtualKey.LeftShift,
               VirtualKey.Control, VirtualKey.LeftControl);
        SetRaw(0, 0xC0); // `

        // Col 1 — 1 / Q / A / Z / LWin
        Set(2, VirtualKey.F1, VirtualKey.Number1, VirtualKey.Q, VirtualKey.A,
               VirtualKey.Z, VirtualKey.LeftWindows);

        // Col 2 — 2 / W / S / X / LAlt
        Set(3, VirtualKey.F2, VirtualKey.Number2, VirtualKey.W, VirtualKey.S,
               VirtualKey.X, VirtualKey.LeftMenu, VirtualKey.Menu);

        // Col 3 — 3 / E / D / C
        Set(5, VirtualKey.F3, VirtualKey.Number3, VirtualKey.E, VirtualKey.D, VirtualKey.C);

        // Col 4 — 4 / R / F / V
        Set(6, VirtualKey.F4, VirtualKey.Number4, VirtualKey.R, VirtualKey.F, VirtualKey.V);

        // Col 5 — 5 / T / G / B
        Set(8, VirtualKey.F5, VirtualKey.Number5, VirtualKey.T, VirtualKey.G, VirtualKey.B);

        // Col 6 — 6 / Y / H / N
        Set(10, VirtualKey.F6, VirtualKey.Number6, VirtualKey.Y, VirtualKey.H, VirtualKey.N);

        // Col 7 — 7 / U / J / M (right of split, around Space center)
        Set(11, VirtualKey.F7, VirtualKey.Number7, VirtualKey.U, VirtualKey.J, VirtualKey.M);

        // Space spans the middle of the strip.
        m[VirtualKey.Space] = new[] { 9, 10, 11, 12, 13, 14 };

        // Col 8 — 8 / I / K / ,
        Set(13, VirtualKey.F8, VirtualKey.Number8, VirtualKey.I, VirtualKey.K);
        SetRaw(13, 0xBC); // ,

        // Col 9 — 9 / O / L / .
        Set(15, VirtualKey.F9, VirtualKey.Number9, VirtualKey.O, VirtualKey.L);
        SetRaw(15, 0xBE); // .

        // Col 10 — 0 / P / ; / /
        Set(16, VirtualKey.F10, VirtualKey.Number0, VirtualKey.P);
        SetRaw(16, 0xBA, 0xBF); // ;  /

        // Col 11 — - / [ / ' / RShift
        SetRaw(18, 0xBD, 0xDB, 0xDE); // -  [  '
        Set(18, VirtualKey.F11, VirtualKey.RightShift);

        // Col 12 — = / ] / Enter
        SetRaw(19, 0xBB, 0xDD); // =  ]
        Set(19, VirtualKey.F12, VirtualKey.Enter);

        // Col 13 — Backspace / \ / RCtrl / RAlt / RWin / Apps
        Set(21, VirtualKey.Back, VirtualKey.RightMenu, VirtualKey.RightControl,
                VirtualKey.RightWindows, VirtualKey.Application);
        SetRaw(21, 0xDC); // \

        // Right edge — PrintScreen / Insert / Home / PgUp / arrows up/left.
        Set(22, VirtualKey.Snapshot, VirtualKey.Insert, VirtualKey.Home,
                VirtualKey.PageUp, VirtualKey.Up, VirtualKey.Left);

        // Far right — Delete / End / PgDn / arrows down/right / extra.
        Set(23, VirtualKey.Delete, VirtualKey.End, VirtualKey.PageDown,
                VirtualKey.Down, VirtualKey.Right,
                VirtualKey.Scroll, VirtualKey.Pause);

        return m;
    }
}

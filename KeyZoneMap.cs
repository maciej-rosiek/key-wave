using System.Collections.Generic;
using Windows.System;

namespace KeyWave.Lighting.Simulator;

/// <summary>
/// Static fallback mapping from VirtualKey to one of 24 zones, arranged 6 rows by 4 columns.
/// Used by the simulator so we can develop without LampArray hardware.
/// Layout (row, column) -> zone index = row*4 + column:
///
///   row 0: [Esc/F1–F4] [F5–F8] [F9–F12] [PrtSc/Ins/Del/Home/End/PgUp/PgDn]
///   row 1: [`~ 1 2 3 4] [5 6 7]  [8 9 0]  [- = Bksp]
///   row 2: [Tab Q W E ] [R T Y]  [U I O]  [P [ ] \ ]
///   row 3: [Caps A S  ] [D F G]  [H J K]  [L ; ' Enter]
///   row 4: [LShift Z X] [C V B]  [N M ,]  [. / RShift]
///   row 5: [LCtrl Win LAlt] [Space-L] [Space-R] [RAlt Apps RCtrl Arrows]
/// </summary>
internal static class KeyZoneMap
{
    public const int Rows = 6;
    public const int Cols = 4;
    public const int ZoneCount = Rows * Cols;

    private static readonly Dictionary<VirtualKey, int[]> _map = Build();

    public static int[] GetIndicesForKey(VirtualKey key)
        => _map.TryGetValue(key, out var v) ? v : System.Array.Empty<int>();

    private static Dictionary<VirtualKey, int[]> Build()
    {
        var m = new Dictionary<VirtualKey, int[]>();

        void One(VirtualKey k, int zone) => m[k] = new[] { zone };
        void Many(int zone, params VirtualKey[] keys) { foreach (var k in keys) m[k] = new[] { zone }; }

        // Row 0
        Many(0, VirtualKey.Escape, VirtualKey.F1, VirtualKey.F2, VirtualKey.F3, VirtualKey.F4);
        Many(1, VirtualKey.F5, VirtualKey.F6, VirtualKey.F7, VirtualKey.F8);
        Many(2, VirtualKey.F9, VirtualKey.F10, VirtualKey.F11, VirtualKey.F12);
        Many(3, VirtualKey.Snapshot, VirtualKey.Scroll, VirtualKey.Pause,
                VirtualKey.Insert, VirtualKey.Delete,
                VirtualKey.Home, VirtualKey.End,
                VirtualKey.PageUp, VirtualKey.PageDown);

        // Row 1 (numbers row). VK_OEM_3 (`~) = 0xC0, OEM_MINUS=0xBD, OEM_PLUS=0xBB
        m[(VirtualKey)0xC0] = new[] { 4 }; // ` ~
        Many(4, VirtualKey.Number1, VirtualKey.Number2, VirtualKey.Number3, VirtualKey.Number4);
        Many(5, VirtualKey.Number5, VirtualKey.Number6, VirtualKey.Number7);
        Many(6, VirtualKey.Number8, VirtualKey.Number9, VirtualKey.Number0);
        m[(VirtualKey)0xBD] = new[] { 7 }; // -
        m[(VirtualKey)0xBB] = new[] { 7 }; // =
        One(VirtualKey.Back, 7);

        // Row 2
        Many(8, VirtualKey.Tab, VirtualKey.Q, VirtualKey.W, VirtualKey.E);
        Many(9, VirtualKey.R, VirtualKey.T, VirtualKey.Y);
        Many(10, VirtualKey.U, VirtualKey.I, VirtualKey.O);
        One(VirtualKey.P, 11);
        m[(VirtualKey)0xDB] = new[] { 11 }; // [
        m[(VirtualKey)0xDD] = new[] { 11 }; // ]
        m[(VirtualKey)0xDC] = new[] { 11 }; // \

        // Row 3
        Many(12, VirtualKey.CapitalLock, VirtualKey.A, VirtualKey.S);
        Many(13, VirtualKey.D, VirtualKey.F, VirtualKey.G);
        Many(14, VirtualKey.H, VirtualKey.J, VirtualKey.K);
        One(VirtualKey.L, 15);
        m[(VirtualKey)0xBA] = new[] { 15 }; // ;
        m[(VirtualKey)0xDE] = new[] { 15 }; // '
        One(VirtualKey.Enter, 15);

        // Row 4
        Many(16, VirtualKey.Shift, VirtualKey.LeftShift, VirtualKey.Z, VirtualKey.X);
        Many(17, VirtualKey.C, VirtualKey.V, VirtualKey.B);
        One(VirtualKey.N, 18);
        One(VirtualKey.M, 18);
        m[(VirtualKey)0xBC] = new[] { 18 }; // ,
        m[(VirtualKey)0xBE] = new[] { 19 }; // .
        m[(VirtualKey)0xBF] = new[] { 19 }; // /
        One(VirtualKey.RightShift, 19);

        // Row 5
        Many(20, VirtualKey.Control, VirtualKey.LeftControl,
                 VirtualKey.LeftWindows, VirtualKey.LeftMenu, VirtualKey.Menu);
        // Space spans the two middle bottom zones.
        m[VirtualKey.Space] = new[] { 21, 22 };
        Many(23, VirtualKey.RightMenu, VirtualKey.RightWindows, VirtualKey.Application,
                 VirtualKey.RightControl,
                 VirtualKey.Left, VirtualKey.Right, VirtualKey.Up, VirtualKey.Down);

        return m;
    }
}

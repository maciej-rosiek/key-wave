using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.System;
using MediaColor = System.Windows.Media.Color;
using WinColor = Windows.UI.Color;

namespace KeyWave.Lighting.Simulator;

public partial class SimulatorPanel : System.Windows.Controls.UserControl, ILampSurface
{
    private readonly Border[] _zones = new Border[KeyZoneMap.ZoneCount];
    private readonly SolidColorBrush[] _brushes = new SolidColorBrush[KeyZoneMap.ZoneCount];
    private readonly Dispatcher _dispatcher;

    public SimulatorPanel()
    {
        InitializeComponent();
        _dispatcher = Dispatcher;

        for (int i = 0; i < _zones.Length; i++)
        {
            var brush = new SolidColorBrush(MediaColor.FromArgb(255, 20, 20, 30));
            _brushes[i] = brush;

            var label = new TextBlock
            {
                Text = i.ToString(),
                Foreground = System.Windows.Media.Brushes.White,
                Opacity = 0.55,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                FontSize = 10,
            };
            // Tighter cell margins because the 1×24 strip squeezes 24 cells in
            // the available width.
            var border = new Border
            {
                Background = brush,
                Margin = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Child = label,
            };
            _zones[i] = border;
            ZoneGrid.Children.Add(border);
        }
    }

    public string DisplayName => "Simulator (24-zone)";
    public int LampCount => _zones.Length;
    public int Rows => KeyZoneMap.Rows;
    public int Cols => KeyZoneMap.Cols;

    public int[] GetIndicesForKey(VirtualKey key) => KeyZoneMap.GetIndicesForKey(key);

    public void SetColor(WinColor color)
    {
        DispatchSet(() =>
        {
            for (int i = 0; i < _brushes.Length; i++)
                _brushes[i].Color = ToMedia(color);
        });
    }

    public void SetColorsForIndices(WinColor[] colors, int[] indices)
    {
        // Snapshot inputs in case the caller reuses the arrays.
        var c = (WinColor[])colors.Clone();
        var idx = (int[])indices.Clone();
        DispatchSet(() =>
        {
            for (int i = 0; i < idx.Length; i++)
            {
                int z = idx[i];
                if ((uint)z < (uint)_brushes.Length)
                    _brushes[z].Color = ToMedia(c[i]);
            }
        });
    }

    private void DispatchSet(Action action)
    {
        if (_dispatcher.CheckAccess()) action();
        else _dispatcher.BeginInvoke(action);
    }

    private static MediaColor ToMedia(WinColor c) => MediaColor.FromArgb(c.A, c.R, c.G, c.B);
}

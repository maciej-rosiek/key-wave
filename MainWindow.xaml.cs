using System.ComponentModel;
using System.Windows;
using LenovoRipple.Lighting;
using LenovoRipple.Lighting.Simulator;

namespace LenovoRipple;

public partial class MainWindow : Window
{
    private readonly LampArrayController _controller;
    private bool _allowClose;

    public MainWindow(LampArrayController controller)
    {
        InitializeComponent();
        _controller = controller;
    }

    /// <summary>The simulator surface embedded in this window. Always present.</summary>
    public SimulatorPanel SimulatorSurface => Simulator;

    /// <summary>
    /// Set to true before calling <see cref="Window.Close"/> when the user has chosen
    /// to actually exit (e.g. via the tray "Exit" command). Otherwise close acts as
    /// "minimize to tray".
    /// </summary>
    public void RequestActualClose()
    {
        _allowClose = true;
        Close();
    }

    public void OnDeviceChanged(ILampSurface surface, bool added)
    {
        Dispatcher.BeginInvoke(new System.Action(() =>
        {
            DeviceCountText.Text = _controller.RealDeviceCount.ToString();
            LampCountText.Text = _controller.TotalLampCount.ToString();
            ModeText.Text = _controller.RealDeviceCount > 0 ? "real + simulator" : "simulator";
        }));
    }

    public void ShowLastKey(string label)
    {
        Dispatcher.BeginInvoke(new System.Action(() => LastKeyText.Text = label));
    }

    public void ShowTheme(string name)
    {
        Dispatcher.BeginInvoke(new System.Action(() => ThemeText.Text = name));
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            // Close-to-tray: hide rather than exit.
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}

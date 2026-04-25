using System;
using System.ComponentModel;
using System.Windows;
using KeyWave.Lighting;
using KeyWave.Lighting.Simulator;

namespace KeyWave;

public partial class MainWindow : Window
{
    private readonly LampArrayController _controller;
    private bool _allowClose;
    private bool _suppressParameterEvents;

    /// <summary>Set by App; called when the user moves a slider or toggles wall bounce.</summary>
    public Action<EffectParameters>? EffectParametersChanged { get; set; }

    public MainWindow(LampArrayController controller)
    {
        InitializeComponent();
        _controller = controller;

        DistanceSlider.ValueChanged   += (_, _) => RaiseParametersChanged();
        WidthSlider.ValueChanged      += (_, _) => RaiseParametersChanged();
        SpeedSlider.ValueChanged      += (_, _) => RaiseParametersChanged();
        WallBounceCheck.Checked       += (_, _) => RaiseParametersChanged();
        WallBounceCheck.Unchecked     += (_, _) => RaiseParametersChanged();
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

    public void ShowEffect(string name)
    {
        Dispatcher.BeginInvoke(new System.Action(() => EffectText.Text = name));
    }

    /// <summary>Push current parameter values into the sliders without triggering the changed event.</summary>
    public void LoadParameters(EffectParameters p)
    {
        Dispatcher.BeginInvoke(new System.Action(() =>
        {
            _suppressParameterEvents = true;
            try
            {
                DistanceSlider.Value    = p.Distance;
                WidthSlider.Value       = p.Width;
                SpeedSlider.Value       = p.SpeedMs;
                WallBounceCheck.IsChecked = p.WallBounce;
                UpdateValueLabels();
            }
            finally
            {
                _suppressParameterEvents = false;
            }
        }));
    }

    private void RaiseParametersChanged()
    {
        UpdateValueLabels();
        if (_suppressParameterEvents) return;
        var p = new EffectParameters(
            Distance:   (int)DistanceSlider.Value,
            Width:      (int)WidthSlider.Value,
            SpeedMs:    (int)SpeedSlider.Value,
            WallBounce: WallBounceCheck.IsChecked == true);
        EffectParametersChanged?.Invoke(p);
    }

    private void UpdateValueLabels()
    {
        DistanceValue.Text = ((int)DistanceSlider.Value).ToString();
        WidthValue.Text    = ((int)WidthSlider.Value).ToString();
        SpeedValue.Text    = $"{(int)SpeedSlider.Value}ms";
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

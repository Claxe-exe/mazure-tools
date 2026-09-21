using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using MazureTools.Core.Native;
using MazureTools.Services;
using MazureTools.ViewModels;

namespace MazureTools.Views;

/// <summary>
/// Transparent, borderless, always-on-top window that hosts Mazu. Pure view behaviour: placement, dragging
/// and the "poke" hop. All decisions (mood, bubble text) live in <see cref="MascotViewModel"/>.
/// </summary>
public partial class MascotWindow : Window
{
    private const double DragThreshold = 4;
    private const double ScreenMargin = 10;

    private readonly MascotViewModel _viewModel;
    private readonly ISettingsService _settings;
    private Point _pressPoint;
    private bool _pressed;

    public MascotWindow(MascotViewModel viewModel, ISettingsService settings)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        _settings = settings;

        SourceInitialized += (_, _) => NativeMethods.MakeNonActivatingToolWindow(new WindowInteropHelper(this).Handle);
        Loaded += (_, _) => Place();

        Hit.MouseLeftButtonDown += OnPress;
        Hit.MouseMove += OnMove;
        Hit.MouseLeftButtonUp += OnRelease;
    }

    /// <summary>Restores the position the user dragged Mazu to, or uses the bottom-right corner above the taskbar.</summary>
    private void Place()
    {
        var area = SystemParameters.WorkArea;
        if (_settings.Current.MascotLeft is { } left && _settings.Current.MascotTop is { } top
            && left > SystemParameters.VirtualScreenLeft - Width / 2
            && left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width / 2
            && top > SystemParameters.VirtualScreenTop
            && top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height / 2)
        {
            Left = left;
            Top = top;
            return;
        }

        Left = area.Right - Width - ScreenMargin;
        Top = area.Bottom - Height - ScreenMargin;
    }

    private void OnPress(object sender, MouseButtonEventArgs e)
    {
        _pressPoint = e.GetPosition(this);
        _pressed = true;
        Hit.CaptureMouse();
    }

    private void OnMove(object sender, MouseEventArgs e)
    {
        if (!_pressed || e.LeftButton != MouseButtonState.Pressed)
            return;

        var now = e.GetPosition(this);
        if (Math.Abs(now.X - _pressPoint.X) < DragThreshold && Math.Abs(now.Y - _pressPoint.Y) < DragThreshold)
            return;

        _pressed = false;
        Hit.ReleaseMouseCapture();
        DragMove(); // returns when the button is released
        _settings.Current.MascotLeft = Left;
        _settings.Current.MascotTop = Top;
        _settings.Save();
    }

    private void OnRelease(object sender, MouseButtonEventArgs e)
    {
        if (!_pressed)
            return;

        _pressed = false;
        Hit.ReleaseMouseCapture();
        ((Storyboard)Resources["Hop"]).Begin(this);
        _viewModel.PokeCommand.Execute(null);
    }
}

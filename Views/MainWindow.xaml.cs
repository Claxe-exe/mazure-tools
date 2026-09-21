using System.Windows;
using System.Windows.Interop;
using MazureTools.Core.Native;
using MazureTools.Services;
using MazureTools.ViewModels;

namespace MazureTools.Views;

public partial class MainWindow : Window
{
    private readonly IThemeService _theme;

    public MainWindow(MainViewModel viewModel, IThemeService theme)
    {
        InitializeComponent();
        DataContext = viewModel;

        _theme = theme;
        SourceInitialized += (_, _) => ApplyTitleBarTheme();
        _theme.ThemeChanged += OnThemeChanged;
        Closed += (_, _) => _theme.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTitleBarTheme();

    private void ApplyTitleBarTheme() => TitleBarTheme.Apply(this);
}

/// <summary>Keeps the native Windows title bar in step with the app theme.</summary>
public static class TitleBarTheme
{
    public static void Apply(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return;

        var dark = Application.Current.TryFindResource("IsDarkTheme") is true;
        var caption = dark ? 0x0B0E12 : 0xFFFFFF; // matches SidebarBrush of each theme
        var text = dark ? 0xE6E9EF : 0x141922;    // matches TextBrush of each theme
        NativeMethods.SetTitleBar(handle, dark, caption, text);
    }
}

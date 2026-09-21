using System.ComponentModel;
using System.Windows;
using MazureTools.Core;

namespace MazureTools.Services;

public interface IWindowController
{
    event EventHandler? HiddenToTray;

    void Show();

    void Show(PageId page);

    void HideToTray();

    /// <summary>Really quits the application (as opposed to hiding it to the tray).</summary>
    void Exit();
}

/// <summary>
/// Owns the show / hide-to-tray / exit behaviour of the main window and tells the navigation service
/// when the window is not visible so pages can stop their timers.
/// </summary>
public sealed class WindowController : IWindowController
{
    private readonly ISettingsService _settings;
    private Window? _window;
    private INavigationService? _navigation;
    private bool _isExiting;

    public WindowController(ISettingsService settings)
    {
        _settings = settings;
    }

    public event EventHandler? HiddenToTray;

    public void Attach(Window window, INavigationService navigation)
    {
        _window = window;
        _navigation = navigation;

        window.Closing += OnClosing;
        window.IsVisibleChanged += (_, _) => UpdateSuspension();
        window.StateChanged += (_, _) => UpdateSuspension();
    }

    public void Show()
    {
        if (_window is null)
            return;

        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
            _window.WindowState = WindowState.Normal;

        _window.Activate();
    }

    public void Show(PageId page)
    {
        _navigation?.Navigate(page);
        Show();
    }

    public void HideToTray()
    {
        _window?.Hide();
        HiddenToTray?.Invoke(this, EventArgs.Empty);
    }

    public void Exit()
    {
        _isExiting = true;
        Application.Current.Shutdown();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_isExiting || !_settings.Current.CloseToTray)
            return;

        e.Cancel = true;
        HideToTray();
    }

    private void UpdateSuspension()
    {
        if (_window is null || _navigation is null)
            return;

        _navigation.SetSuspended(!_window.IsVisible || _window.WindowState == WindowState.Minimized);
    }
}

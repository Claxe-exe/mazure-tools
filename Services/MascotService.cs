using MazureTools.ViewModels;
using MazureTools.Views;

namespace MazureTools.Services;

public interface IMascotService
{
    bool IsEnabled { get; }

    /// <summary>Raised when Mazu is shown or hidden (from Settings or from its own menu).</summary>
    event EventHandler? EnabledChanged;

    /// <summary>Shows or hides Mazu and remembers the choice.</summary>
    void SetEnabled(bool enabled);

    /// <summary>Shows Mazu at startup if the saved setting says so.</summary>
    void ApplySavedSetting();
}

/// <summary>Owns the always-on-top desktop mascot window and its lifetime.</summary>
public sealed class MascotService : IMascotService
{
    private readonly MascotViewModel _viewModel;
    private readonly ISettingsService _settings;
    private MascotWindow? _window;

    public MascotService(MascotViewModel viewModel, ISettingsService settings)
    {
        _viewModel = viewModel;
        _settings = settings;
        _viewModel.HideRequested += (_, _) => SetEnabled(false);
    }

    public bool IsEnabled => _settings.Current.ShowMascot;

    public event EventHandler? EnabledChanged;

    public void ApplySavedSetting()
    {
        if (IsEnabled)
            Show();
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled == IsEnabled && enabled == (_window is not null))
            return;

        _settings.Current.ShowMascot = enabled;
        _settings.Save();

        if (enabled)
            Show();
        else
            Hide();

        EnabledChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Show()
    {
        if (_window is not null)
            return;

        _window = new MascotWindow(_viewModel, _settings);
        _window.Show();
        _viewModel.Start();
    }

    private void Hide()
    {
        _viewModel.Stop();
        _window?.Close();
        _window = null;
    }
}

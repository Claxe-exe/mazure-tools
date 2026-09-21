using System.Reflection;
using System.Windows.Input;
using MazureTools.Core;
using MazureTools.Services;

namespace MazureTools.ViewModels;

public sealed class SettingsViewModel : PageViewModel
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;
    private readonly IStartupService _startup;
    private readonly IElevationService _elevation;
    private readonly IMascotService _mascot;
    private bool _runAtStartup;

    public SettingsViewModel(
        IDialogService dialogs,
        ISettingsService settings,
        IThemeService theme,
        IStartupService startup,
        IElevationService elevation,
        IMascotService mascot)
        : base(dialogs, PageId.Settings, "")
    {
        _settings = settings;
        _theme = theme;
        _startup = startup;
        _elevation = elevation;
        _mascot = mascot;
        _mascot.EnabledChanged += (_, _) => OnPropertyChanged(nameof(ShowMascot)); // also changes via Mazu's own menu

        RestartAsAdminCommand = new RelayCommand(
            () => _elevation.OfferRestartAsAdministrator(Loc.T("Set_RestartReason")),
            () => !_elevation.IsElevated);
    }

    public ICommand RestartAsAdminCommand { get; }

    public bool IsEnglish
    {
        get => _settings.Current.Language == AppLanguage.English;
        set
        {
            if (value)
                ChangeLanguage(AppLanguage.English);
        }
    }

    public bool IsTurkish
    {
        get => _settings.Current.Language == AppLanguage.Turkish;
        set
        {
            if (value)
                ChangeLanguage(AppLanguage.Turkish);
        }
    }

    private void ChangeLanguage(AppLanguage language)
    {
        if (_settings.Current.Language == language && Loc.Current == language)
            return;

        _settings.Current.Language = language;
        _settings.Save();
        Loc.SetLanguage(language); // raises LanguageChanged: every view model and DynamicResource refreshes
    }

    public bool UseDarkTheme
    {
        get => _settings.Current.Theme == ThemeMode.Dark;
        set
        {
            var mode = value ? ThemeMode.Dark : ThemeMode.Light;
            if (_settings.Current.Theme == mode)
                return;

            _settings.Current.Theme = mode;
            _settings.Save();
            _theme.Apply(mode);
            OnPropertyChanged();
        }
    }

    public bool ShowMascot
    {
        get => _mascot.IsEnabled;
        set => _mascot.SetEnabled(value);
    }

    public bool CloseToTray
    {
        get => _settings.Current.CloseToTray;
        set
        {
            if (_settings.Current.CloseToTray == value)
                return;

            _settings.Current.CloseToTray = value;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    public bool RunAtStartup
    {
        get => _runAtStartup;
        set
        {
            if (_runAtStartup == value)
                return;

            try
            {
                _startup.SetEnabled(value);
                _runAtStartup = value;
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }

            OnPropertyChanged();
        }
    }

    public bool IsElevated => _elevation.IsElevated;

    public string PrivilegeText => Loc.T(IsElevated ? "Set_Admin" : "Set_Standard");

    public string DataFolderText => Loc.T("Set_DataFolder", SettingsFolder);

    private static string SettingsFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MazureTools");

    public string VersionText { get; } =
        "Mazure Tools " + (Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0] ?? "0.1.0");

    protected override void OnActivated()
    {
        // The registry is the source of truth (the user may have removed the entry in Task Manager).
        _runAtStartup = _startup.IsEnabled;
        OnPropertyChanged(nameof(RunAtStartup));
    }
}

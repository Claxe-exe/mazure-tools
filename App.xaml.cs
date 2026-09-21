using System.Text;
using System.Windows;
using System.Windows.Threading;
using MazureTools.Core;
using MazureTools.Modules.Network;
using MazureTools.Modules.Processes;
using MazureTools.Modules.Storage;
using MazureTools.Modules.SystemInfo;
using MazureTools.Modules.Utilities;
using MazureTools.Services;
using MazureTools.ViewModels;
using MazureTools.Views;

namespace MazureTools;

public partial class App : Application
{
    private SingleInstanceGuard? _instanceGuard;
    private TrayService? _tray;
    private NetworkService? _networkService;
    private IMascotService? _mascot;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Console tools (ipconfig) print in OEM code pages such as 857; make them decodable.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        DispatcherUnhandledException += OnUnhandledException;

        // The UI is static text and bars updated once a second, so software rendering costs no extra CPU
        // but avoids loading the GPU driver's Direct3D stack (~60 MB). Set MAZURE_HW_RENDER=1 to opt back in.
        if (Environment.GetEnvironmentVariable("MAZURE_HW_RENDER") != "1")
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

        // Language first, so even the "already running" message is shown in the user's language.
        var settings = new SettingsService();
        Loc.SetLanguage(settings.Current.Language);

        // After "Restart as administrator" the previous instance is still shutting down; give it a moment.
        var restarted = e.Args.Contains(ElevationService.RestartedArgument);
        _instanceGuard = new SingleInstanceGuard();
        if (!_instanceGuard.TryAcquire(restarted ? TimeSpan.FromSeconds(10) : TimeSpan.Zero))
        {
            if (!_instanceGuard.SignalExistingInstance())
                MessageBox.Show(Loc.T("App_AlreadyRunning"),
                    "Mazure Tools", MessageBoxButton.OK, MessageBoxImage.Information);

            Shutdown();
            return;
        }

        ComposeAndShow(settings, startMinimized: e.Args.Contains(StartupService.MinimizedArgument));
    }

    /// <summary>Composition root: the only place that knows every concrete type.</summary>
    private void ComposeAndShow(SettingsService settings, bool startMinimized)
    {
        var theme = new ThemeService();
        theme.Apply(settings.Current.Theme);

        var dialogs = new DialogService();
        var windowController = new WindowController(settings);
        var elevation = new ElevationService(dialogs, windowController);
        var commands = new CommandRunner();

        var systemInfo = new SystemInfoService();
        var monitor = new SystemMonitor();
        var storage = new StorageService();
        _networkService = new NetworkService(commands, elevation);
        var processGuard = new ProcessGuard();
        var processService = new ProcessService(processGuard, elevation);

        var mascotService = new MascotService(new MascotViewModel(monitor, storage, _networkService, windowController), settings);
        _mascot = mascotService;

        var pages = new PageViewModel[]
        {
            new DashboardViewModel(dialogs, systemInfo, monitor, storage, _networkService),
            new SystemViewModel(dialogs, systemInfo, monitor, storage),
            new NetworkViewModel(dialogs, _networkService, elevation, new PingToolViewModel(dialogs, new PingService())),
            new ProcessesViewModel(dialogs, processService, processGuard, elevation),
            new StorageViewModel(dialogs, storage),
            new UtilitiesViewModel(
                dialogs,
                new HashToolViewModel(dialogs, new HashService()),
                new PortCheckerViewModel(dialogs, new PortCheckService()),
                new LaunchersViewModel(dialogs, new ShellLauncher(), elevation)),
            new SettingsViewModel(dialogs, settings, theme, new StartupService(), elevation, mascotService)
        };

        var navigation = new NavigationService(pages, PageId.Dashboard);
        var window = new MainWindow(new MainViewModel(navigation, pages, elevation), theme);
        MainWindow = window;
        windowController.Attach(window, navigation);

        _tray = new TrayService(windowController, settings);
        _instanceGuard!.ListenForSignals(() => Dispatcher.BeginInvoke(() => windowController.Show()));

        if (!startMinimized)
            windowController.Show();

        _mascot?.ApplySavedSetting(); // Mazu lives on the desktop even while the main window is in the tray
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _networkService?.Dispose();
        _instanceGuard?.Dispose();
        base.OnExit(e);
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Last line of defence: report instead of crashing, and keep a log for bug reports.
        e.Handled = true;
        try
        {
            var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MazureTools");
            System.IO.Directory.CreateDirectory(folder);
            System.IO.File.AppendAllText(System.IO.Path.Combine(folder, "error.log"), $"[{DateTime.Now:s}] {e.Exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception)
        {
            // Logging must never throw.
        }

        MessageBox.Show(Loc.T("App_UnexpectedError", e.Exception.Message),
            "Mazure Tools", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

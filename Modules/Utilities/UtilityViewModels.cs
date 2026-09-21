using System.Windows.Input;
using MazureTools.Core;
using MazureTools.Services;

namespace MazureTools.Modules.Utilities;

public sealed class UtilitiesViewModel : PageViewModel
{
    public UtilitiesViewModel(
        IDialogService dialogs,
        HashToolViewModel hash,
        PortCheckerViewModel port,
        LaunchersViewModel launchers)
        : base(dialogs, PageId.Utilities, "")
    {
        Hash = hash;
        Port = port;
        Launchers = launchers;
    }

    public HashToolViewModel Hash { get; }

    public PortCheckerViewModel Port { get; }

    public LaunchersViewModel Launchers { get; }

    protected override void OnDeactivated()
    {
        Hash.Cancel();
        Port.Cancel();
    }
}

public sealed class HashToolViewModel : ViewModelBase
{
    private readonly IHashService _hashes;
    private CancellationTokenSource? _cts;
    private string _filePath = string.Empty;
    private string _text = string.Empty;
    private string _expected = string.Empty;
    private bool _isBusy;
    private double _progress;
    private HashResults? _results;
    private string? _sourceFileName; // null = the result came from the text box

    public HashToolViewModel(IDialogService dialogs, IHashService hashes) : base(dialogs)
    {
        _hashes = hashes;
        BrowseCommand = new RelayCommand(Browse, () => !IsBusy);
        HashFileCommand = CreateAsyncCommand(HashFileAsync, () => !IsBusy);
        HashTextCommand = new RelayCommand(HashText, () => !IsBusy);
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
    }

    public ICommand BrowseCommand { get; }

    public ICommand HashFileCommand { get; }

    public ICommand HashTextCommand { get; }

    public ICommand CancelCommand { get; }

    public string FilePath { get => _filePath; set => SetProperty(ref _filePath, value); }

    public string Text { get => _text; set => SetProperty(ref _text, value); }

    public string Expected
    {
        get => _expected;
        set
        {
            if (SetProperty(ref _expected, value))
                OnPropertyChanged(nameof(Comparison));
        }
    }

    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

    public double Progress { get => _progress; private set => SetProperty(ref _progress, value); }

    /// <summary>What was hashed (file name or "text input"), so results are never ambiguous.</summary>
    public string SourceCaption =>
        _results is null ? string.Empty : Loc.T("Hash_ResultFor", _sourceFileName ?? Loc.T("Hash_SourceText"));

    public string Md5 => _results?.Md5 ?? string.Empty;

    public string Sha1 => _results?.Sha1 ?? string.Empty;

    public string Sha256 => _results?.Sha256 ?? string.Empty;

    public bool HasResults => _results is not null;

    /// <summary>Result of comparing the pasted expected hash with the calculated ones.</summary>
    public string Comparison
    {
        get
        {
            if (_results is null || string.IsNullOrWhiteSpace(Expected))
                return string.Empty;

            return _results.FindMatch(Expected) is { } algorithm
                ? Loc.T("Hash_Match", algorithm)
                : Loc.T("Hash_NoMatch");
        }
    }

    public void Cancel() => _cts?.Cancel();

    private void Browse()
    {
        if (Dialogs.PickFile(Loc.T("Hash_PickTitle")) is { } file)
            FilePath = file;
    }

    private async Task HashFileAsync()
    {
        var path = FilePath.Trim().Trim('"');
        if (path.Length == 0)
        {
            Dialogs.ShowWarning(Loc.T("Hash_Dialog"), Loc.T("Hash_ChooseFile"));
            return;
        }

        IsBusy = true;
        Progress = 0;
        _cts = new CancellationTokenSource();
        try
        {
            var progress = new Progress<double>(value => Progress = value * 100);
            SetResults(await _hashes.HashFileAsync(path, progress, _cts.Token), Path.GetFileName(path));
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            IsBusy = false;
        }
    }

    private void HashText()
    {
        if (Text.Length == 0)
        {
            Dialogs.ShowWarning(Loc.T("Hash_Dialog"), Loc.T("Hash_TypeText"));
            return;
        }

        SetResults(_hashes.HashText(Text), sourceFileName: null);
    }

    private void SetResults(HashResults results, string? sourceFileName)
    {
        _results = results;
        _sourceFileName = sourceFileName;
        OnPropertyChanged(nameof(SourceCaption));
        OnPropertyChanged(nameof(Md5));
        OnPropertyChanged(nameof(Sha1));
        OnPropertyChanged(nameof(Sha256));
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(Comparison));
    }
}

public sealed class PortCheckerViewModel : ViewModelBase
{
    // Windows takes ~2 s to report a refused connection (SYN retries); "localhost" tries two addresses.
    private const int TimeoutMs = 5000;

    private readonly IPortCheckService _ports;
    private CancellationTokenSource? _cts;
    private string _host = "127.0.0.1";
    private string _portText = "443";
    private bool _isBusy;
    private LocText _result = LocText.Empty;
    private PortState? _state;

    public PortCheckerViewModel(IDialogService dialogs, IPortCheckService ports) : base(dialogs)
    {
        _ports = ports;
        CheckCommand = CreateAsyncCommand(CheckAsync, () => !IsBusy);
    }

    public ICommand CheckCommand { get; }

    public string Host { get => _host; set => SetProperty(ref _host, value); }

    public string PortText { get => _portText; set => SetProperty(ref _portText, value); }

    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

    public string Result => _result.ToString();

    public PortState? State { get => _state; private set => SetProperty(ref _state, value); }

    public void Cancel() => _cts?.Cancel();

    private void SetResult(LocText text)
    {
        _result = text;
        OnPropertyChanged(nameof(Result));
    }

    private async Task CheckAsync()
    {
        if (!int.TryParse(PortText, out var port))
        {
            Dialogs.ShowWarning(Loc.T("Port_Title"), Loc.T("Port_Invalid"));
            return;
        }

        IsBusy = true;
        State = null;
        SetResult(LocText.Of("Port_Connecting"));
        _cts = new CancellationTokenSource();
        try
        {
            var result = await _ports.CheckAsync(Host, port, TimeoutMs, _cts.Token);
            State = result.State;
            SetResult(result.State switch
            {
                PortState.Open => LocText.Of("Port_Open", result.Port, result.Host,
                    result.Address is null ? string.Empty : $" ({result.Address})", result.ElapsedMs),
                PortState.Closed => LocText.Of("Port_Closed", result.Port, result.Host),
                _ => LocText.Of("Port_TimedOut", result.Host, result.Port, TimeoutMs / 1000)
            });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            SetResult(LocText.Empty);
            Dialogs.ShowWarning(Loc.T("Port_Title"), ex.Message);
        }
        catch
        {
            SetResult(LocText.Empty);
            throw;
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            IsBusy = false;
        }
    }
}

public sealed class LaunchersViewModel : ViewModelBase
{
    private readonly IShellLauncher _launcher;
    private readonly bool _isElevated;

    public LaunchersViewModel(IDialogService dialogs, IShellLauncher launcher, IElevationService elevation) : base(dialogs)
    {
        _launcher = launcher;
        _isElevated = elevation.IsElevated;

        OpenCmdCommand = new RelayCommand(() => Launch(_launcher.OpenCommandPrompt));
        OpenPowerShellCommand = new RelayCommand(() => Launch(_launcher.OpenPowerShell));
        OpenExplorerCommand = new RelayCommand(() => Launch(_launcher.OpenExplorer));
    }

    public string PrivilegeNote => Loc.T(_isElevated ? "Launch_Elevated" : "Launch_Standard");

    public ICommand OpenCmdCommand { get; }

    public ICommand OpenPowerShellCommand { get; }

    public ICommand OpenExplorerCommand { get; }

    private void Launch(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }
}

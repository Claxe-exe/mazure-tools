using System.Collections.ObjectModel;
using MazureTools.Core;
using MazureTools.Modules.Network;
using MazureTools.Modules.Storage;
using MazureTools.Modules.SystemInfo;
using MazureTools.Services;

namespace MazureTools.ViewModels;

public sealed class DashboardViewModel : PageViewModel
{
    private const int DiskRefreshEveryTicks = 15;

    private readonly ISystemInfoService _info;
    private readonly ISystemMonitor _monitor;
    private readonly IStorageService _storage;
    private readonly INetworkService _network;
    private IDisposable? _subscription;
    private SystemProfile? _profile;
    private NetworkOverview? _overview;
    private int _ticks;

    public DashboardViewModel(
        IDialogService dialogs,
        ISystemInfoService info,
        ISystemMonitor monitor,
        IStorageService storage,
        INetworkService network)
        : base(dialogs, PageId.Dashboard, "")
    {
        _info = info;
        _monitor = monitor;
        _storage = storage;
        _network = network;
    }

    public LiveMetricsViewModel Metrics { get; } = new();

    public ObservableCollection<DiskInfo> Disks { get; } = [];

    public string OsText => _profile is null ? "—" : $"{_profile.OsName} · {_profile.OsVersion}";

    public string CpuName => _profile?.CpuName ?? "—";

    public string GpuName => _profile is null ? "—" : string.Join(" + ", _profile.GpuNames);

    public string NetworkStatus => _overview is null ? Loc.T("Net_Checking") : _overview.IsConnected ? Loc.T("Net_Connected") : Loc.T("Net_Disconnected");

    public bool IsNetworkConnected => _overview?.IsConnected == true;

    public string NetworkDetail => _overview switch
    {
        null => string.Empty,
        { IsConnected: false } => Loc.T("Dash_NoConnection"),
        { AdapterName: null } => Loc.T("Dash_NoGateway"),
        var o => $"{o.AdapterName} · {o.LocalIp}"
    };

    protected override void OnActivated()
    {
        _ticks = 0;
        _ = LoadProfileAsync();
        _ = RefreshDisksAsync();
        _ = RefreshNetworkAsync();

        _network.NetworkChanged += OnNetworkChanged;
        _subscription = _monitor.Subscribe(OnMetrics);
    }

    protected override void OnDeactivated()
    {
        _network.NetworkChanged -= OnNetworkChanged;
        _subscription?.Dispose();
        _subscription = null;
    }

    private void OnMetrics(MetricsSnapshot snapshot)
    {
        Metrics.Apply(snapshot);
        if (++_ticks % DiskRefreshEveryTicks == 0)
            _ = RefreshDisksAsync();
    }

    private void OnNetworkChanged(object? sender, EventArgs e) => PostToUi(() => _ = RefreshNetworkAsync());

    private async Task LoadProfileAsync()
    {
        if (_profile is not null)
            return;

        try
        {
            _profile = await Task.Run(_info.GetProfile);
            OnPropertyChanged(nameof(OsText));
            OnPropertyChanged(nameof(CpuName));
            OnPropertyChanged(nameof(GpuName));
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private async Task RefreshDisksAsync()
    {
        try
        {
            var disks = await Task.Run(_storage.GetDisks);
            if (IsActive)
                Disks.ReplaceWith(disks);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private async Task RefreshNetworkAsync()
    {
        try
        {
            _overview = await Task.Run(_network.GetOverview);
        }
        catch (Exception ex) when (ex is System.Net.NetworkInformation.NetworkInformationException)
        {
            _overview = new NetworkOverview(false, null, null, null, []);
        }

        OnPropertyChanged(nameof(NetworkStatus));
        OnPropertyChanged(nameof(IsNetworkConnected));
        OnPropertyChanged(nameof(NetworkDetail));
    }
}

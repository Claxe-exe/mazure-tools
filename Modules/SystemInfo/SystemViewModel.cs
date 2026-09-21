using System.Collections.ObjectModel;
using MazureTools.Core;
using MazureTools.Modules.Storage;
using MazureTools.Services;

namespace MazureTools.Modules.SystemInfo;

public sealed class SystemViewModel : PageViewModel
{
    private const int DiskRefreshEveryTicks = 15;

    private readonly ISystemInfoService _info;
    private readonly ISystemMonitor _monitor;
    private readonly IStorageService _storage;
    private IDisposable? _subscription;
    private SystemProfile? _profile;
    private int _ticks;

    public SystemViewModel(IDialogService dialogs, ISystemInfoService info, ISystemMonitor monitor, IStorageService storage)
        : base(dialogs, PageId.System, "")
    {
        _info = info;
        _monitor = monitor;
        _storage = storage;
    }

    public LiveMetricsViewModel Metrics { get; } = new();

    public ObservableCollection<DiskInfo> Disks { get; } = [];

    public SystemProfile? Profile
    {
        get => _profile;
        private set
        {
            if (SetProperty(ref _profile, value))
                OnPropertyChanged(nameof(GpuNames));
        }
    }

    public string GpuNames => Profile is null ? "—" : string.Join(Environment.NewLine, Profile.GpuNames);

    protected override void OnActivated()
    {
        _ticks = 0;
        _ = LoadProfileAsync();
        _ = RefreshDisksAsync();
        _subscription = _monitor.Subscribe(OnMetrics);
    }

    protected override void OnDeactivated()
    {
        _subscription?.Dispose();
        _subscription = null;
    }

    private void OnMetrics(MetricsSnapshot snapshot)
    {
        Metrics.Apply(snapshot);
        if (++_ticks % DiskRefreshEveryTicks == 0)
            _ = RefreshDisksAsync();
    }

    private async Task LoadProfileAsync()
    {
        if (Profile is not null)
            return;

        try
        {
            Profile = await Task.Run(_info.GetProfile);
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
}

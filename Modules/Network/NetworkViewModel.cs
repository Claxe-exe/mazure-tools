using System.Collections.ObjectModel;
using System.Windows.Input;
using MazureTools.Core;
using MazureTools.Services;

namespace MazureTools.Modules.Network;

public sealed class NetworkViewModel : PageViewModel
{
    private readonly INetworkService _network;
    private readonly IElevationService _elevation;
    private NetworkOverview? _overview;
    private string? _publicIpValue;
    private LocText _publicIpState = LocText.Of("PublicIp_NotLookedUp");

    public NetworkViewModel(
        IDialogService dialogs,
        INetworkService network,
        IElevationService elevation,
        PingToolViewModel ping)
        : base(dialogs, PageId.Network, "")
    {
        _network = network;
        _elevation = elevation;
        Ping = ping;

        RefreshCommand = CreateAsyncCommand(RefreshAsync);
        FetchPublicIpCommand = CreateAsyncCommand(FetchPublicIpAsync);
        FlushDnsCommand = CreateAsyncCommand(FlushDnsAsync);
        RenewIpCommand = CreateAsyncCommand(RenewIpAsync);
    }

    public PingToolViewModel Ping { get; }

    public ObservableCollection<NetworkAdapterInfo> Adapters { get; } = [];

    public ICommand RefreshCommand { get; }

    public ICommand FetchPublicIpCommand { get; }

    public ICommand FlushDnsCommand { get; }

    public ICommand RenewIpCommand { get; }

    public bool IsElevated => _elevation.IsElevated;

    public string ConnectionText => _overview is null ? Loc.T("Net_Checking") : _overview.IsConnected ? Loc.T("Net_Connected") : Loc.T("Net_Disconnected");

    public string ActiveAdapter => _overview?.AdapterName ?? "—";

    public string LocalIp => _overview?.LocalIp ?? "—";

    public string Gateway => _overview?.Gateway ?? "—";

    public string DnsServers => _overview is { DnsServers.Count: > 0 } o ? string.Join(", ", o.DnsServers) : "—";

    public string PublicIp => _publicIpValue ?? _publicIpState.ToString();

    protected override void OnActivated()
    {
        _network.NetworkChanged += OnNetworkChanged;
        RefreshCommand.Execute(null);
    }

    protected override void OnDeactivated()
    {
        _network.NetworkChanged -= OnNetworkChanged;
        Ping.Stop();
    }

    private void OnNetworkChanged(object? sender, EventArgs e) => PostToUi(() => RefreshCommand.Execute(null));

    private async Task RefreshAsync()
    {
        var (overview, adapters) = await Task.Run(() => (_network.GetOverview(), _network.GetAdapters()));
        _overview = overview;
        Adapters.ReplaceWith(adapters);

        OnPropertyChanged(nameof(ConnectionText));
        OnPropertyChanged(nameof(ActiveAdapter));
        OnPropertyChanged(nameof(LocalIp));
        OnPropertyChanged(nameof(Gateway));
        OnPropertyChanged(nameof(DnsServers));
    }

    private async Task FetchPublicIpAsync()
    {
        ShowPublicIp(null, LocText.Of("PublicIp_Looking"));
        try
        {
            ShowPublicIp(await _network.GetPublicIpAsync(CancellationToken.None), default);
        }
        catch
        {
            ShowPublicIp(null, LocText.Of("PublicIp_Failed"));
            throw;
        }
    }

    private void ShowPublicIp(string? value, LocText state)
    {
        _publicIpValue = value;
        _publicIpState = state;
        OnPropertyChanged(nameof(PublicIp));
    }

    private async Task FlushDnsAsync()
    {
        var confirmed = Dialogs.Confirm(Loc.T("Net_FlushDns"), Loc.T("Net_FlushBody"), Loc.T("Net_FlushDns"));
        if (!confirmed)
            return;

        ShowResult(Loc.T("Net_FlushDns"), await _network.FlushDnsCacheAsync());
    }

    private async Task RenewIpAsync()
    {
        if (!_elevation.IsElevated)
        {
            _elevation.OfferRestartAsAdministrator(Loc.T("Net_RenewReason"));
            return;
        }

        var confirmed = Dialogs.Confirm(Loc.T("Net_RenewIp"), Loc.T("Net_RenewBody"), Loc.T("Net_RenewIp"));
        if (!confirmed)
            return;

        ShowResult(Loc.T("Net_RenewIp"), await _network.RenewIpAsync());
        await RefreshAsync();
    }

    private void ShowResult(string title, OperationResult result)
    {
        if (result.RequiresElevation)
            _elevation.OfferRestartAsAdministrator(result.Message);
        else if (result.Success)
            Dialogs.ShowInfo(title, result.Message);
        else
            Dialogs.ShowError(title, result.Message);
    }
}

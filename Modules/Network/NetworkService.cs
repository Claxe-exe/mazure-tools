using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using MazureTools.Core;
using MazureTools.Services;

namespace MazureTools.Modules.Network;

public interface INetworkService
{
    /// <summary>Raised (debounced, on a background thread) when adapters or addresses change. No polling involved.</summary>
    event EventHandler? NetworkChanged;

    IReadOnlyList<NetworkAdapterInfo> GetAdapters();

    NetworkOverview GetOverview();

    /// <summary>Asks an external service for the address the internet sees. Only called on explicit user request.</summary>
    Task<string> GetPublicIpAsync(CancellationToken cancellationToken);

    Task<OperationResult> FlushDnsCacheAsync(CancellationToken cancellationToken = default);

    Task<OperationResult> RenewIpAsync(CancellationToken cancellationToken = default);
}

public sealed class NetworkService : INetworkService, IDisposable
{
    private static readonly string[] PublicIpEndpoints = ["https://api.ipify.org", "https://icanhazip.com"];
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static readonly string IpConfig = Path.Combine(Environment.SystemDirectory, "ipconfig.exe");

    private readonly ICommandRunner _runner;
    private readonly IElevationService _elevation;
    private readonly object _gate = new();
    private EventHandler? _changed;
    private Timer? _debounce;

    public NetworkService(ICommandRunner runner, IElevationService elevation)
    {
        _runner = runner;
        _elevation = elevation;
    }

    public event EventHandler? NetworkChanged
    {
        add
        {
            lock (_gate)
            {
                if (_changed is null)
                {
                    NetworkChange.NetworkAddressChanged += OnSystemNetworkChanged;
                    NetworkChange.NetworkAvailabilityChanged += OnSystemNetworkChanged;
                }

                _changed += value;
            }
        }
        remove
        {
            lock (_gate)
            {
                _changed -= value;
                if (_changed is null)
                {
                    NetworkChange.NetworkAddressChanged -= OnSystemNetworkChanged;
                    NetworkChange.NetworkAvailabilityChanged -= OnSystemNetworkChanged;
                }
            }
        }
    }

    private void OnSystemNetworkChanged(object? sender, EventArgs e)
    {
        // Windows raises bursts of these events; coalesce them into one notification.
        lock (_gate)
        {
            _debounce?.Dispose();
            _debounce = new Timer(_ => _changed?.Invoke(this, EventArgs.Empty), null, TimeSpan.FromMilliseconds(600), Timeout.InfiniteTimeSpan);
        }
    }

    // NDIS filter bindings show up as extra "adapters" named like "Wi-Fi 2-QoS Packet Scheduler-0000".
    private static readonly Regex FilterBindingName = new(@"-\d{4}$", RegexOptions.Compiled);

    public IReadOnlyList<NetworkAdapterInfo> GetAdapters() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback && !FilterBindingName.IsMatch(n.Name))
            .OrderByDescending(n => n.OperationalStatus == OperationalStatus.Up)
            .ThenBy(n => n.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(ToInfo)
            .ToList();

    public NetworkOverview GetOverview()
    {
        var connected = NetworkInterface.GetIsNetworkAvailable();
        var active = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                        && n.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))
            .Select(n => (Nic: n, Props: n.GetIPProperties()))
            .FirstOrDefault(x => IPv4Gateways(x.Props).Any() && IPv4Addresses(x.Props).Any());

        if (active.Nic is null)
            return new NetworkOverview(connected, null, null, null, []);

        return new NetworkOverview(
            connected,
            active.Nic.Name,
            IPv4Addresses(active.Props).FirstOrDefault(),
            IPv4Gateways(active.Props).FirstOrDefault(),
            active.Props.DnsAddresses.Select(a => a.ToString()).ToList());
    }

    public async Task<string> GetPublicIpAsync(CancellationToken cancellationToken)
    {
        Exception? last = null;
        foreach (var endpoint in PublicIpEndpoints)
        {
            try
            {
                var text = (await Http.GetStringAsync(endpoint, cancellationToken)).Trim();
                if (IPAddress.TryParse(text, out var address))
                    return address.ToString();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                last = ex;
            }
        }

        throw new InvalidOperationException(Loc.T("Net_PublicIpFail"), last);
    }

    public async Task<OperationResult> FlushDnsCacheAsync(CancellationToken cancellationToken = default)
    {
        // Recent Windows builds allow this without elevation, older ones do not: try first, escalate only on failure.
        var result = await _runner.RunAsync(IpConfig, "/flushdns", TimeSpan.FromSeconds(15), cancellationToken);
        if (result.ExitCode == 0)
            return OperationResult.Ok(Loc.T("Net_FlushOk"));

        return _elevation.IsElevated
            ? OperationResult.Fail(Loc.T("Net_FlushFail", result.Output))
            : OperationResult.NeedsElevation(Loc.T("Net_FlushNeedsAdmin", result.Output));
    }

    public async Task<OperationResult> RenewIpAsync(CancellationToken cancellationToken = default)
    {
        if (!_elevation.IsElevated)
            return OperationResult.NeedsElevation(Loc.T("Net_RenewNeedsAdmin"));

        var result = await _runner.RunAsync(IpConfig, "/renew", TimeSpan.FromSeconds(90), cancellationToken);
        return result.ExitCode == 0
            ? OperationResult.Ok(Loc.T("Net_RenewOk", result.Output))
            : OperationResult.Fail(Loc.T("Net_RenewFail", result.Output));
    }

    private static NetworkAdapterInfo ToInfo(NetworkInterface nic)
    {
        var props = nic.GetIPProperties();
        bool? dhcp = null;
        try
        {
            dhcp = props.GetIPv4Properties().IsDhcpEnabled;
        }
        catch (NetworkInformationException)
        {
            // Adapter has IPv4 disabled.
        }

        var mac = nic.GetPhysicalAddress().ToString();
        return new NetworkAdapterInfo(
            nic.Name,
            nic.Description,
            nic.NetworkInterfaceType.ToString(),
            nic.OperationalStatus.ToString(),
            mac.Length == 0 ? "—" : string.Join('-', Enumerable.Range(0, mac.Length / 2).Select(i => mac.Substring(i * 2, 2))),
            FormatSpeed(nic.Speed),
            IPv4Addresses(props).ToList(),
            props.UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6).Select(a => a.Address.ToString()).ToList(),
            props.GatewayAddresses.Select(g => g.Address).Where(IsRealGateway).Select(a => a.ToString()).ToList(),
            props.DnsAddresses.Select(a => a.ToString()).ToList(),
            dhcp);
    }

    private static IEnumerable<string> IPv4Addresses(IPInterfaceProperties props) =>
        props.UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork).Select(a => a.Address.ToString());

    private static IEnumerable<string> IPv4Gateways(IPInterfaceProperties props) =>
        props.GatewayAddresses.Select(g => g.Address).Where(a => a.AddressFamily == AddressFamily.InterNetwork && IsRealGateway(a)).Select(a => a.ToString());

    private static bool IsRealGateway(IPAddress address) =>
        !address.Equals(IPAddress.Any) && !address.Equals(IPAddress.IPv6Any);

    private static string FormatSpeed(long bitsPerSecond) => bitsPerSecond switch
    {
        <= 0 => "—",
        >= 1_000_000_000 => $"{bitsPerSecond / 1_000_000_000.0:0.#} Gbps",
        _ => $"{bitsPerSecond / 1_000_000.0:0.#} Mbps"
    };

    public void Dispose()
    {
        lock (_gate)
        {
            _debounce?.Dispose();
        }
    }
}

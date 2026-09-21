using MazureTools.Core;

namespace MazureTools.Modules.Network;

public sealed record NetworkAdapterInfo(
    string Name,
    string Description,
    string Type,
    string Status,
    string MacAddress,
    string Speed,
    IReadOnlyList<string> IPv4Addresses,
    IReadOnlyList<string> IPv6Addresses,
    IReadOnlyList<string> Gateways,
    IReadOnlyList<string> DnsServers,
    bool? DhcpEnabled)
{
    public bool IsUp => Status == "Up";

    public string IPv4Text => Join(IPv4Addresses);

    public string IPv6Text => Join(IPv6Addresses);

    public string GatewayText => Join(Gateways);

    public string DnsText => Join(DnsServers);

    public string DhcpText => DhcpEnabled switch { true => Loc.T("Common_Enabled"), false => Loc.T("Common_Disabled"), null => "—" };

    private static string Join(IReadOnlyList<string> values) => values.Count == 0 ? "—" : string.Join(", ", values);
}

/// <summary>Summary of the adapter Windows is actually using to reach the network.</summary>
public sealed record NetworkOverview(
    bool IsConnected,
    string? AdapterName,
    string? LocalIp,
    string? Gateway,
    IReadOnlyList<string> DnsServers);

public sealed record PingStatistics(int Sent, int Received, long? MinMs, double? AvgMs, long? MaxMs)
{
    public int Lost => Sent - Received;

    public double LossPercent => Sent == 0 ? 0 : Lost * 100.0 / Sent;
}

/// <summary>One line of ping output plus the statistics so far.</summary>
public sealed record PingUpdate(string Line, PingStatistics Statistics);

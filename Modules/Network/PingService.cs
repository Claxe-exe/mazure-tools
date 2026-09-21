using System.Net.NetworkInformation;
using MazureTools.Core;

namespace MazureTools.Modules.Network;

public interface IPingService
{
    /// <summary>Sends <paramref name="count"/> echo requests. Cancelling returns the statistics gathered so far.</summary>
    Task<PingStatistics> RunAsync(string host, int count, int timeoutMs, IProgress<PingUpdate> progress, CancellationToken cancellationToken);
}

public sealed class PingService : IPingService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    public async Task<PingStatistics> RunAsync(string host, int count, int timeoutMs, IProgress<PingUpdate> progress, CancellationToken cancellationToken)
    {
        host = host.Trim();
        if (host.Length == 0 || Uri.CheckHostName(host) == UriHostNameType.Unknown)
            throw new ArgumentException(Loc.T("Ping_HostInvalid"));

        var roundTrips = new List<long>();
        var sent = 0;
        PingStatistics Snapshot() => new(
            sent,
            roundTrips.Count,
            roundTrips.Count == 0 ? null : roundTrips.Min(),
            roundTrips.Count == 0 ? null : roundTrips.Average(),
            roundTrips.Count == 0 ? null : roundTrips.Max());

        using var ping = new Ping();
        try
        {
            for (var i = 1; i <= count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sent++;

                string line;
                try
                {
                    var reply = await ping.SendPingAsync(host, timeoutMs);
                    if (reply.Status == IPStatus.Success)
                    {
                        roundTrips.Add(reply.RoundtripTime);
                        var time = reply.RoundtripTime < 1 ? "<1 ms" : $"{reply.RoundtripTime} ms";
                        line = Loc.T("Ping_Reply", reply.Address, time, reply.Options?.Ttl.ToString() ?? "?");
                    }
                    else
                    {
                        line = reply.Status == IPStatus.TimedOut ? Loc.T("Ping_TimedOut") : Describe(reply.Status);
                    }
                }
                catch (PingException ex)
                {
                    // Typically an unresolvable host name: every further attempt would fail the same way.
                    progress.Report(new PingUpdate(Loc.T("Ping_Failed", ex.InnerException?.Message ?? ex.Message), Snapshot()));
                    break;
                }

                progress.Report(new PingUpdate(line, Snapshot()));
                if (i < count)
                    await Task.Delay(Interval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Stopped by the user: report what we have.
        }

        return Snapshot();
    }

    private static string Describe(IPStatus status) => status switch
    {
        IPStatus.DestinationHostUnreachable => Loc.T("Ping_HostUnreachable"),
        IPStatus.DestinationNetworkUnreachable => Loc.T("Ping_NetUnreachable"),
        IPStatus.TtlExpired => Loc.T("Ping_TtlExpired"),
        IPStatus.BadDestination => Loc.T("Ping_BadDestination"),
        _ => Loc.T("Ping_Failed", status)
    };
}

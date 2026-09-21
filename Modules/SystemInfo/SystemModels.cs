namespace MazureTools.Modules.SystemInfo;

/// <summary>Hardware/OS facts that do not change while the app runs.</summary>
public sealed record SystemProfile(
    string MachineName,
    string CpuName,
    int LogicalProcessors,
    IReadOnlyList<string> GpuNames,
    string OsName,
    string OsVersion,
    string Architecture,
    long TotalMemoryBytes);

public sealed record MemoryInfo(long TotalBytes, long AvailableBytes)
{
    public long UsedBytes => TotalBytes - AvailableBytes;

    public double UsedPercent => TotalBytes == 0 ? 0 : UsedBytes * 100.0 / TotalBytes;
}

/// <param name="CpuPercent">Null until two samples exist (utilisation is a rate).</param>
/// <param name="GpuPercent">Null when this machine exposes no GPU counters or no valid sample yet.</param>
public sealed record MetricsSnapshot(
    double? CpuPercent,
    MemoryInfo Memory,
    double? GpuPercent,
    bool GpuSupported,
    TimeSpan Uptime);

using MazureTools.Core;

namespace MazureTools.Modules.SystemInfo;

/// <summary>Presentation of the live CPU / RAM / GPU / uptime numbers, shared by the Dashboard and System pages.</summary>
public sealed class LiveMetricsViewModel : ObservableObject
{
    private double _cpuPercent;
    private string _cpuText = "—";
    private double _memoryPercent;
    private string _memorySummary = "—";
    private string _memoryUsedText = "—";
    private string _memoryFreeText = "—";
    private string _memoryTotalText = "—";
    private double _gpuPercent;
    private string _gpuText = "—";
    private bool _gpuSupported = true;
    private TimeSpan? _uptime;

    public LiveMetricsViewModel()
    {
        // Durations and the GPU note are formatted on read, so they follow a language switch.
        Loc.LanguageChanged += (_, _) => OnPropertyChanged(string.Empty);
    }

    public double CpuPercent { get => _cpuPercent; private set => SetProperty(ref _cpuPercent, value); }

    public string CpuText { get => _cpuText; private set => SetProperty(ref _cpuText, value); }

    public double MemoryPercent
    {
        get => _memoryPercent;
        private set
        {
            if (SetProperty(ref _memoryPercent, value))
                OnPropertyChanged(nameof(MemoryPercentText));
        }
    }

    public string MemoryPercentText => $"{MemoryPercent:0}%";

    /// <summary>e.g. "12.4 GB / 31.9 GB"</summary>
    public string MemorySummary { get => _memorySummary; private set => SetProperty(ref _memorySummary, value); }

    public string MemoryUsedText { get => _memoryUsedText; private set => SetProperty(ref _memoryUsedText, value); }

    public string MemoryFreeText { get => _memoryFreeText; private set => SetProperty(ref _memoryFreeText, value); }

    public string MemoryTotalText { get => _memoryTotalText; private set => SetProperty(ref _memoryTotalText, value); }

    public double GpuPercent { get => _gpuPercent; private set => SetProperty(ref _gpuPercent, value); }

    public string GpuText { get => _gpuText; private set => SetProperty(ref _gpuText, value); }

    /// <summary>Explains why GPU load is not shown, so "N/A" is never unexplained.</summary>
    public string GpuNote => _gpuSupported ? string.Empty : Loc.T("Gpu_NoCounters");

    public string UptimeText => _uptime is { } uptime ? ByteFormatter.FormatDuration(uptime) : "—";

    public void Apply(MetricsSnapshot snapshot)
    {
        CpuPercent = snapshot.CpuPercent ?? 0;
        CpuText = snapshot.CpuPercent is { } cpu ? $"{cpu:0}%" : "—";

        var memory = snapshot.Memory;
        MemoryPercent = memory.UsedPercent;
        MemorySummary = $"{ByteFormatter.Format(memory.UsedBytes)} / {ByteFormatter.Format(memory.TotalBytes)}";
        MemoryUsedText = ByteFormatter.Format(memory.UsedBytes);
        MemoryFreeText = ByteFormatter.Format(memory.AvailableBytes);
        MemoryTotalText = ByteFormatter.Format(memory.TotalBytes);

        GpuPercent = snapshot.GpuPercent ?? 0;
        GpuText = !snapshot.GpuSupported ? "N/A" : snapshot.GpuPercent is { } gpu ? $"{gpu:0}%" : "—";
        _gpuSupported = snapshot.GpuSupported;
        OnPropertyChanged(nameof(GpuNote));

        _uptime = snapshot.Uptime;
        OnPropertyChanged(nameof(UptimeText));
    }
}

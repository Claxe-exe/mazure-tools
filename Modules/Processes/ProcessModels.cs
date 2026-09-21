using MazureTools.Core;

namespace MazureTools.Modules.Processes;

/// <summary>Immutable measurement of one process at one point in time.</summary>
public sealed record ProcessSample(int Pid, string Name, double CpuPercent, long WorkingSetBytes);

/// <summary>Row shown in the process list; updated in place so selection and sorting survive refreshes.</summary>
public sealed class ProcessEntry : ObservableObject
{
    private double _cpuPercent;
    private long _memoryBytes;

    public ProcessEntry(int pid, string name, ProcessRisk risk)
    {
        Pid = pid;
        Name = name;
        Risk = risk;
    }

    public int Pid { get; }

    public string Name { get; }

    public ProcessRisk Risk { get; }

    public string RiskText => Risk switch
    {
        ProcessRisk.Critical => Loc.T("Risk_Protected"),
        ProcessRisk.System => Loc.T("Risk_System"),
        _ => string.Empty
    };

    public double CpuPercent
    {
        get => _cpuPercent;
        private set
        {
            if (SetProperty(ref _cpuPercent, value))
                OnPropertyChanged(nameof(CpuText));
        }
    }

    public long MemoryBytes
    {
        get => _memoryBytes;
        private set
        {
            if (SetProperty(ref _memoryBytes, value))
                OnPropertyChanged(nameof(MemoryText));
        }
    }

    public string CpuText => CpuPercent < 0.05 ? "0%" : $"{CpuPercent:0.0}%";

    public string MemoryText => ByteFormatter.Format(MemoryBytes);

    public void RefreshLocalization() => OnPropertyChanged(nameof(RiskText));

    public void Update(ProcessSample sample)
    {
        CpuPercent = sample.CpuPercent;
        MemoryBytes = sample.WorkingSetBytes;
    }
}

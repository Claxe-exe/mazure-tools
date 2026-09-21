using System.ComponentModel;
using System.Diagnostics;
using MazureTools.Core;
using MazureTools.Services;

namespace MazureTools.Modules.Processes;

public interface IProcessService
{
    /// <summary>Measures all running processes. CPU values are relative to the previous call.</summary>
    IReadOnlyList<ProcessSample> Sample();

    OperationResult Terminate(int pid, string expectedName);

    OperationResult Restart(int pid, string expectedName);
}

public sealed class ProcessService : IProcessService
{
    private readonly IProcessGuard _guard;
    private readonly IElevationService _elevation;
    private readonly Dictionary<int, (TimeSpan Cpu, long Ticks)> _previous = [];

    public ProcessService(IProcessGuard guard, IElevationService elevation)
    {
        _guard = guard;
        _elevation = elevation;
    }

    public IReadOnlyList<ProcessSample> Sample()
    {
        var now = Stopwatch.GetTimestamp();
        var processors = Environment.ProcessorCount;
        var samples = new List<ProcessSample>();
        var current = new Dictionary<int, (TimeSpan, long)>();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                double cpu = 0;
                try
                {
                    var total = process.TotalProcessorTime;
                    current[process.Id] = (total, now);
                    if (_previous.TryGetValue(process.Id, out var before))
                    {
                        var wall = Stopwatch.GetElapsedTime(before.Ticks, now);
                        if (wall > TimeSpan.Zero && total >= before.Cpu)
                            cpu = Math.Clamp((total - before.Cpu).TotalMilliseconds / (wall.TotalMilliseconds * processors) * 100, 0, 100);
                    }
                }
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or NotSupportedException)
                {
                    // Protected/exited process: CPU time is not readable, show 0 rather than guessing.
                }

                long memory;
                try { memory = process.WorkingSet64; }
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException) { memory = 0; }

                samples.Add(new ProcessSample(process.Id, process.ProcessName, cpu, memory));
            }
        }

        _previous.Clear();
        foreach (var item in current)
            _previous[item.Key] = item.Value;

        return samples;
    }

    public OperationResult Terminate(int pid, string expectedName)
    {
        if (Blocked(pid, expectedName) is { } blocked)
            return blocked;

        try
        {
            using var process = OpenVerified(pid, expectedName);
            process.Kill();
            if (!process.WaitForExit(TimeSpan.FromSeconds(5)))
                return OperationResult.Fail(Loc.T("Proc_StillRunning", expectedName));

            return OperationResult.Ok(Loc.T("Proc_Ended", expectedName, pid));
        }
        catch (ProcessGoneException)
        {
            return OperationResult.Ok(Loc.T("Proc_AlreadyExited", expectedName));
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
        {
            return AccessDenied(expectedName);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return OperationResult.Fail(Loc.T("Proc_EndFailed", expectedName, ex.Message));
        }
    }

    public OperationResult Restart(int pid, string expectedName)
    {
        if (Blocked(pid, expectedName) is { } blocked)
            return blocked;

        if (_guard.Assess(pid, expectedName).Risk != ProcessRisk.Normal)
            return OperationResult.Fail(Loc.T("Proc_NotRestartable", expectedName));

        try
        {
            using var process = OpenVerified(pid, expectedName);
            var path = process.MainModule?.FileName;
            if (string.IsNullOrEmpty(path))
                return OperationResult.Fail(Loc.T("Proc_NoPath", expectedName));

            process.Kill();
            if (!process.WaitForExit(TimeSpan.FromSeconds(5)))
                return OperationResult.Fail(Loc.T("Proc_DidNotExit", expectedName));

            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(path) ?? string.Empty
            });
            return OperationResult.Ok(Loc.T("Proc_Restarted", expectedName));
        }
        catch (ProcessGoneException)
        {
            return OperationResult.Fail(Loc.T("Proc_NotRunning", expectedName));
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
        {
            return AccessDenied(expectedName);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return OperationResult.Fail(Loc.T("Proc_RestartFailed", expectedName, ex.Message));
        }
    }

    /// <summary>Defence in depth: the service refuses critical targets even if a caller skips the confirmation UI.</summary>
    private OperationResult? Blocked(int pid, string name)
    {
        var assessment = _guard.Assess(pid, name);
        return assessment.Risk == ProcessRisk.Critical
            ? OperationResult.Fail(Loc.T("Proc_Blocked", name, assessment.Reason))
            : null;
    }

    private OperationResult AccessDenied(string name) => _elevation.IsElevated
        ? OperationResult.Fail(Loc.T("Proc_AccessDeniedAdmin", name))
        : OperationResult.NeedsElevation(Loc.T("Proc_AccessDenied", name));

    /// <summary>Guards against PID reuse: only acts if the PID still belongs to the process the user selected.</summary>
    private static Process OpenVerified(int pid, string expectedName)
    {
        Process process;
        try
        {
            process = Process.GetProcessById(pid);
        }
        catch (ArgumentException)
        {
            throw new ProcessGoneException();
        }

        if (!string.Equals(process.ProcessName, expectedName, StringComparison.OrdinalIgnoreCase))
        {
            process.Dispose();
            throw new ProcessGoneException();
        }

        return process;
    }

    private sealed class ProcessGoneException : Exception;
}

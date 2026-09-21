using MazureTools.Core;

namespace MazureTools.Modules.Processes;

public enum ProcessRisk
{
    /// <summary>Ordinary user process.</summary>
    Normal,

    /// <summary>Part of Windows or its shell. Ending it can break the desktop, sound, networking or security features.</summary>
    System,

    /// <summary>Ending it would crash or log off Windows. Mazure Tools refuses.</summary>
    Critical
}

public sealed record ProcessAssessment(ProcessRisk Risk, string Reason)
{
    public static readonly ProcessAssessment Safe = new(ProcessRisk.Normal, string.Empty);
}

public interface IProcessGuard
{
    ProcessAssessment Assess(int pid, string name);
}

/// <summary>
/// Name-based safety net against closing essential Windows processes by accident.
/// It is a heuristic (a malicious program can use any name); it exists to protect the user from mis-clicks.
/// Explanations live in the language dictionaries under the key <c>Guard_{name}</c>.
/// </summary>
public sealed class ProcessGuard : IProcessGuard
{
    /// <summary>Ending these crashes or logs off Windows.</summary>
    private static readonly HashSet<string> Critical = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "System Idle Process", "Registry", "Memory Compression", "smss", "csrss", "wininit",
        "winlogon", "services", "lsass", "lsaiso", "Secure System", "Idle"
    };

    /// <summary>Ending these breaks parts of Windows; allowed only after a strong warning.</summary>
    private static readonly HashSet<string> Sensitive = new(StringComparer.OrdinalIgnoreCase)
    {
        "svchost", "explorer", "dwm", "sihost", "ctfmon", "fontdrvhost", "audiodg", "spoolsv", "taskhostw",
        "runtimebroker", "searchhost", "startmenuexperiencehost", "shellexperiencehost", "textinputhost",
        "applicationframehost", "conhost", "wudfhost", "dashost", "wmiprvse", "msmpeng", "nissrv",
        "securityhealthservice", "securityhealthsystray", "smartscreen", "searchindexer", "lsm", "wlanext"
    };

    public ProcessAssessment Assess(int pid, string name)
    {
        if (pid == Environment.ProcessId)
            return new ProcessAssessment(ProcessRisk.Critical, Loc.T("Guard_self"));

        if (pid is 0 or 4)
            return new ProcessAssessment(ProcessRisk.Critical, Loc.T("Guard_kernel"));

        var key = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
        if (Critical.Contains(key))
            return new ProcessAssessment(ProcessRisk.Critical, Loc.T(GuardKey(key)));

        if (Sensitive.Contains(key))
            return new ProcessAssessment(ProcessRisk.System, Loc.T(GuardKey(key)));

        return ProcessAssessment.Safe;
    }

    private static string GuardKey(string name) => "Guard_" + name.Replace(" ", string.Empty).ToLowerInvariant();
}

using Microsoft.Win32;

namespace MazureTools.Services;

public interface IStartupService
{
    bool IsEnabled { get; }

    /// <summary>Adds/removes a per-user (HKCU) autostart entry. Needs no administrator rights.</summary>
    void SetEnabled(bool enabled);
}

public sealed class StartupService : IStartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MazureTools";

    public const string MinimizedArgument = "--minimized";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        if (enabled)
            key.SetValue(ValueName, $"\"{Environment.ProcessPath}\" {MinimizedArgument}");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}

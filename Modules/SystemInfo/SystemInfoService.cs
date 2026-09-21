using System.Runtime.InteropServices;
using MazureTools.Core;
using MazureTools.Core.Native;
using Microsoft.Win32;

namespace MazureTools.Modules.SystemInfo;

public interface ISystemInfoService
{
    SystemProfile GetProfile();
}

/// <summary>Reads static hardware/OS facts from the registry and Win32 (no WMI, no extra packages).</summary>
public sealed class SystemInfoService : ISystemInfoService
{
    private const string DisplayAdapterClassKey = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    public SystemProfile GetProfile()
    {
        var (osName, osVersion) = ReadOs();
        var memory = new NativeMethods.MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>() };
        var totalMemory = NativeMethods.GlobalMemoryStatusEx(ref memory) ? (long)memory.ullTotalPhys : 0;

        return new SystemProfile(
            Environment.MachineName,
            ReadCpuName(),
            Environment.ProcessorCount,
            ReadGpuNames(),
            osName,
            osVersion,
            RuntimeInformation.OSArchitecture.ToString(),
            totalMemory);
    }

    private static string ReadCpuName()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        return (key?.GetValue("ProcessorNameString") as string)?.Trim() is { Length: > 0 } name ? name : Loc.T("Sys_UnknownCpu");
    }

    private static IReadOnlyList<string> ReadGpuNames()
    {
        var names = new List<string>();
        try
        {
            using var classKey = Registry.LocalMachine.OpenSubKey(DisplayAdapterClassKey);
            if (classKey is null)
                return [Loc.T("Sys_UnknownGpu")];

            foreach (var subKeyName in classKey.GetSubKeyNames())
            {
                // Only numeric instance keys ("0000", "0001", ...) describe adapters.
                if (!int.TryParse(subKeyName, out _))
                    continue;

                using var adapter = classKey.OpenSubKey(subKeyName);
                var description = adapter?.GetValue("DriverDesc") as string;
                var deviceId = adapter?.GetValue("MatchingDeviceId") as string;

                // Skip virtual adapters (remote desktop, basic display) that are not PCI devices.
                if (!string.IsNullOrWhiteSpace(description)
                    && deviceId?.StartsWith("pci\\", StringComparison.OrdinalIgnoreCase) == true
                    && !names.Contains(description))
                {
                    names.Add(description);
                }
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            // Fall through to "Unknown GPU".
        }

        return names.Count > 0 ? names : [Loc.T("Sys_UnknownGpu")];
    }

    private static (string Name, string Version) ReadOs()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        var name = key?.GetValue("ProductName") as string ?? "Windows";
        var display = key?.GetValue("DisplayVersion") as string ?? key?.GetValue("ReleaseId") as string;
        var build = key?.GetValue("CurrentBuildNumber") as string ?? Environment.OSVersion.Version.Build.ToString();
        var revision = key?.GetValue("UBR");

        // Windows 11 still reports "Windows 10 ..." in ProductName; the build number tells them apart.
        if (int.TryParse(build, out var buildNumber) && buildNumber >= 22000 && name.StartsWith("Windows 10", StringComparison.Ordinal))
            name = "Windows 11" + name["Windows 10".Length..];

        var version = string.IsNullOrEmpty(display) ? $"Build {build}" : $"{display} (Build {build}{(revision is null ? "" : "." + revision)})";
        return (name, version);
    }
}

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MazureTools.Core;

public sealed record CommandResult(int ExitCode, string Output);

/// <summary>Runs trusted Windows console tools directly (no shell, so no command injection surface).</summary>
public interface ICommandRunner
{
    Task<CommandResult> RunAsync(string executable, string arguments, TimeSpan timeout, CancellationToken cancellationToken = default);
}

public sealed class CommandRunner : ICommandRunner
{
    public async Task<CommandResult> RunAsync(string executable, string arguments, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        // Console tools write in the OEM code page (e.g. 857 on Turkish Windows).
        var oem = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        var startInfo = new ProcessStartInfo(executable, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = oem,
            StandardErrorEncoding = oem
        };

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException(Loc.T("Cmd_StartFailed", Path.GetFileName(executable), ex.Message), ex);
        }

        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            if (cancellationToken.IsCancellationRequested)
                throw;
            throw new TimeoutException(Loc.T("Cmd_Timeout", Path.GetFileName(executable), timeout.TotalSeconds.ToString("0")));
        }

        var output = (await stdout + Environment.NewLine + await stderr).Trim();
        return new CommandResult(process.ExitCode, output);
    }

    private static void TryKill(Process process)
    {
        try { process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { /* already exited */ }
        catch (Win32Exception) { /* cannot kill; nothing more to do */ }
    }
}

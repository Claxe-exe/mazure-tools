using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using MazureTools.Core;

namespace MazureTools.Modules.Utilities;

// ---------------------------------------------------------------- hashing

public sealed record HashResults(string Md5, string Sha1, string Sha256)
{
    /// <summary>Returns the algorithm whose hash equals <paramref name="expected"/>, or null if none does.</summary>
    public string? FindMatch(string expected)
    {
        var normalized = new string(expected.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());
        if (normalized.Length == 0)
            return null;

        if (Sha256.Equals(normalized, StringComparison.OrdinalIgnoreCase)) return "SHA-256";
        if (Sha1.Equals(normalized, StringComparison.OrdinalIgnoreCase)) return "SHA-1";
        if (Md5.Equals(normalized, StringComparison.OrdinalIgnoreCase)) return "MD5";
        return null;
    }
}

public interface IHashService
{
    /// <summary>Hashes a file in one streaming pass (constant memory, works for very large files).</summary>
    Task<HashResults> HashFileAsync(string path, IProgress<double>? progress, CancellationToken cancellationToken);

    HashResults HashText(string text);
}

public sealed class HashService : IHashService
{
    private const int BufferSize = 1 << 20;

    public async Task<HashResults> HashFileAsync(string path, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        // FileShare.ReadWrite lets us hash files another program currently has open.
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize, useAsync: true);
        using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        using var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        var buffer = new byte[BufferSize];
        var length = stream.Length;
        long done = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            md5.AppendData(buffer, 0, read);
            sha1.AppendData(buffer, 0, read);
            sha256.AppendData(buffer, 0, read);
            done += read;
            if (length > 0)
                progress?.Report((double)done / length);
        }

        return new HashResults(
            Convert.ToHexStringLower(md5.GetHashAndReset()),
            Convert.ToHexStringLower(sha1.GetHashAndReset()),
            Convert.ToHexStringLower(sha256.GetHashAndReset()));
    }

    public HashResults HashText(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return new HashResults(
            Convert.ToHexStringLower(MD5.HashData(bytes)),
            Convert.ToHexStringLower(SHA1.HashData(bytes)),
            Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
}

// ---------------------------------------------------------------- port checker

public enum PortState
{
    Open,
    Closed,
    TimedOut
}

public sealed record PortCheckResult(string Host, int Port, PortState State, long ElapsedMs, string? Address);

public interface IPortCheckService
{
    Task<PortCheckResult> CheckAsync(string host, int port, int timeoutMs, CancellationToken cancellationToken);
}

/// <summary>Single-port TCP connect test (a plain connect, no scanning or probing beyond that one port).</summary>
public sealed class PortCheckService : IPortCheckService
{
    public async Task<PortCheckResult> CheckAsync(string host, int port, int timeoutMs, CancellationToken cancellationToken)
    {
        host = host.Trim();
        if (host.Length == 0 || Uri.CheckHostName(host) == UriHostNameType.Unknown)
            throw new ArgumentException(Loc.T("Port_HostInvalid"));
        if (port is < 1 or > 65535)
            throw new ArgumentException(Loc.T("Port_Invalid"));

        using var client = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(timeoutMs);

        var watch = Stopwatch.StartNew();
        try
        {
            await client.ConnectAsync(host, port, timeout.Token);
            var address = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString();
            return new PortCheckResult(host, port, PortState.Open, watch.ElapsedMilliseconds, address);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PortCheckResult(host, port, PortState.TimedOut, watch.ElapsedMilliseconds, null);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
        {
            return new PortCheckResult(host, port, PortState.Closed, watch.ElapsedMilliseconds, null);
        }
        catch (SocketException ex) when (ex.SocketErrorCode is SocketError.HostNotFound or SocketError.TryAgain)
        {
            throw new InvalidOperationException(Loc.T("Port_NotFound", host), ex);
        }
    }
}

// ---------------------------------------------------------------- launchers

public interface IShellLauncher
{
    void OpenCommandPrompt();

    void OpenPowerShell();

    void OpenExplorer();
}

/// <summary>Starts standard Windows tools with the same privileges as Mazure Tools (never silently elevated).</summary>
public sealed class ShellLauncher : IShellLauncher
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public void OpenCommandPrompt() =>
        Start(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "Command Prompt");

    public void OpenPowerShell() =>
        Start(Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"), "PowerShell");

    public void OpenExplorer() =>
        Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), "Windows Explorer");

    private static void Start(string path, string displayName)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, WorkingDirectory = Home });
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException(Loc.T("Launch_Failed", displayName, ex.Message), ex);
        }
    }
}

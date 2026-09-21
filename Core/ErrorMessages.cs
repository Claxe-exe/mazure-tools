using System.ComponentModel;
using System.Net.Sockets;

namespace MazureTools.Core;

/// <summary>Turns low-level exceptions into messages a user can act on.</summary>
public static class ErrorMessages
{
    public static string Describe(Exception ex) => ex switch
    {
        UnauthorizedAccessException => Loc.T("Err_Access"),
        Win32Exception { NativeErrorCode: 5 } => Loc.T("Err_Access"),
        TimeoutException => ex.Message,
        SocketException socket => Loc.T("Err_Network", socket.Message),
        DirectoryNotFoundException => Loc.T("Err_DirNotFound"),
        FileNotFoundException => Loc.T("Err_FileNotFound"),
        IOException => Loc.T("Err_IO", ex.Message),
        _ => ex.Message
    };
}

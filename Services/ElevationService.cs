using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using MazureTools.Core;

namespace MazureTools.Services;

public interface IElevationService
{
    bool IsElevated { get; }

    /// <summary>
    /// Explains why administrator rights are needed and, if the user agrees and accepts the Windows UAC prompt,
    /// relaunches Mazure Tools elevated and exits this instance. Returns true when the restart was started.
    /// </summary>
    bool OfferRestartAsAdministrator(string reason);
}

public sealed class ElevationService : IElevationService
{
    public const string RestartedArgument = "--restarted";

    private const int ErrorCancelledByUser = 1223;

    private readonly IDialogService _dialogs;
    private readonly IWindowController _window;

    public ElevationService(IDialogService dialogs, IWindowController window)
    {
        _dialogs = dialogs;
        _window = window;
    }

    public bool IsElevated { get; } = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    public bool OfferRestartAsAdministrator(string reason)
    {
        if (IsElevated)
        {
            _dialogs.ShowError(Loc.T("Elev_AlreadyTitle"), Loc.T("Elev_Already", reason));
            return false;
        }

        var confirmed = _dialogs.Confirm(
            Loc.T("Elev_Title"),
            Loc.T("Elev_Body", reason),
            Loc.T("Elev_Button"));
        if (!confirmed)
            return false;

        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, RestartedArgument)
            {
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelledByUser)
        {
            _dialogs.ShowInfo(Loc.T("Elev_CancelledTitle"), Loc.T("Elev_Cancelled"));
            return false;
        }
        catch (Win32Exception ex)
        {
            _dialogs.ShowError(Loc.T("Elev_FailedTitle"), Loc.T("Elev_Failed", ex.Message));
            return false;
        }

        _window.Exit();
        return true;
    }
}

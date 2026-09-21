using System.Windows;
using MazureTools.Core;
using MazureTools.Views;
using Microsoft.Win32;

namespace MazureTools.Services;

public sealed class DialogService : IDialogService
{
    public bool Confirm(string title, string message, string confirmText = "OK", bool isDangerous = false) =>
        Show(title, message, isDangerous ? MessageKind.Danger : MessageKind.Question, confirmText, Loc.T("Common_Cancel"));

    public void ShowInfo(string title, string message) => Show(title, message, MessageKind.Info, Loc.T("Common_OK"), null);

    public void ShowWarning(string title, string message) => Show(title, message, MessageKind.Warning, Loc.T("Common_OK"), null);

    public void ShowError(string title, string message) => Show(title, message, MessageKind.Error, Loc.T("Common_OK"), null);

    public string? PickFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title };
        return dialog.ShowDialog(ActiveWindow()) == true ? dialog.FolderName : null;
    }

    public string? PickFile(string title)
    {
        var dialog = new OpenFileDialog { Title = title, CheckFileExists = true };
        return dialog.ShowDialog(ActiveWindow()) == true ? dialog.FileName : null;
    }

    private static bool Show(string title, string message, MessageKind kind, string confirmText, string? cancelText)
    {
        var dialog = new MessageDialog(title, message, kind, confirmText, cancelText) { Owner = ActiveWindow() };
        return dialog.ShowDialog() == true;
    }

    private static Window? ActiveWindow()
    {
        var app = Application.Current;
        var active = app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        var main = app.MainWindow is { IsVisible: true } m ? m : null;
        return active ?? main;
    }
}

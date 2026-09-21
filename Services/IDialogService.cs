namespace MazureTools.Services;

/// <summary>All user prompts go through here so view models never touch WPF windows directly.</summary>
public interface IDialogService
{
    bool Confirm(string title, string message, string confirmText = "OK", bool isDangerous = false);

    void ShowInfo(string title, string message);

    void ShowWarning(string title, string message);

    void ShowError(string title, string message);

    string? PickFolder(string title);

    string? PickFile(string title);
}

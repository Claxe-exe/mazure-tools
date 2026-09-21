using MazureTools.Services;

namespace MazureTools.Core;

public abstract class ViewModelBase : ObservableObject
{
    protected ViewModelBase(IDialogService dialogs)
    {
        Dialogs = dialogs;
        // Texts are computed from state, so "all properties changed" is enough to re-translate the whole view.
        Loc.LanguageChanged += (_, _) => OnPropertyChanged(string.Empty);
    }

    protected IDialogService Dialogs { get; }

    protected AsyncRelayCommand CreateAsyncCommand(Func<Task> execute, Func<bool>? canExecute = null) =>
        new(execute, ReportError, canExecute);

    protected void ReportError(Exception exception) =>
        Dialogs.ShowError(Loc.T("Common_ErrorTitle"), ErrorMessages.Describe(exception));
}

/// <summary>
/// A navigable page. Pages are only "active" while visible, so timers and event
/// subscriptions can be started in <see cref="OnActivated"/> and stopped in <see cref="OnDeactivated"/>
/// (no background polling while the page is hidden or the window sits in the tray).
/// </summary>
public abstract class PageViewModel : ViewModelBase
{
    private bool _isActive;
    private bool _isSelected;
    private SynchronizationContext? _uiContext;

    protected PageViewModel(IDialogService dialogs, PageId id, string icon) : base(dialogs)
    {
        Id = id;
        Icon = icon;
    }

    public PageId Id { get; }

    public string Title => Loc.T($"Nav_{Id}");

    /// <summary>Segoe MDL2 Assets glyph.</summary>
    public string Icon { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    protected bool IsActive => _isActive;

    /// <summary>Runs <paramref name="action"/> on the UI thread; safe to call from background callbacks.</summary>
    protected void PostToUi(Action action)
    {
        if (_uiContext is null)
            action();
        else
            _uiContext.Post(_ => action(), null);
    }

    public void Activate()
    {
        if (_isActive)
            return;

        _uiContext ??= SynchronizationContext.Current;
        _isActive = true;
        OnActivated();
    }

    public void Deactivate()
    {
        if (!_isActive)
            return;

        _isActive = false;
        OnDeactivated();
    }

    protected virtual void OnActivated() { }

    protected virtual void OnDeactivated() { }
}

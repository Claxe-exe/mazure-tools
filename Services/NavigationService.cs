using MazureTools.Core;

namespace MazureTools.Services;

public interface INavigationService
{
    PageViewModel CurrentPage { get; }

    event EventHandler? CurrentPageChanged;

    void Navigate(PageId page);

    /// <summary>Pauses the current page while the window is hidden/minimised so nothing polls in the background.</summary>
    void SetSuspended(bool suspended);
}

public sealed class NavigationService : INavigationService
{
    private readonly IReadOnlyDictionary<PageId, PageViewModel> _pages;
    // Starts suspended: the first page is activated when the window actually becomes visible.
    private bool _suspended = true;

    public NavigationService(IEnumerable<PageViewModel> pages, PageId startPage)
    {
        _pages = pages.ToDictionary(p => p.Id);
        CurrentPage = _pages[startPage];
        CurrentPage.IsSelected = true;
    }

    public PageViewModel CurrentPage { get; private set; }

    public event EventHandler? CurrentPageChanged;

    public void Navigate(PageId page)
    {
        var next = _pages[page];
        if (ReferenceEquals(next, CurrentPage))
            return;

        CurrentPage.Deactivate();
        CurrentPage.IsSelected = false;

        CurrentPage = next;
        CurrentPage.IsSelected = true;
        if (!_suspended)
            CurrentPage.Activate();

        CurrentPageChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetSuspended(bool suspended)
    {
        if (_suspended == suspended)
            return;

        _suspended = suspended;
        if (suspended)
            CurrentPage.Deactivate();
        else
            CurrentPage.Activate();
    }
}

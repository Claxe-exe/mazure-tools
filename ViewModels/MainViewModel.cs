using System.Reflection;
using System.Windows.Input;
using MazureTools.Core;
using MazureTools.Services;

namespace MazureTools.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    public MainViewModel(INavigationService navigation, IReadOnlyList<PageViewModel> pages, IElevationService elevation)
    {
        _navigation = navigation;
        NavigationItems = pages.Where(p => p.Id != PageId.Settings).ToList();
        SettingsPage = pages.First(p => p.Id == PageId.Settings);
        IsElevated = elevation.IsElevated;
        Loc.LanguageChanged += (_, _) => OnPropertyChanged(nameof(PrivilegeText));

        NavigateCommand = new RelayCommand<PageViewModel>(page => _navigation.Navigate(page.Id));
        _navigation.CurrentPageChanged += (_, _) => OnPropertyChanged(nameof(CurrentPage));
    }

    public IReadOnlyList<PageViewModel> NavigationItems { get; }

    public PageViewModel SettingsPage { get; }

    public PageViewModel CurrentPage => _navigation.CurrentPage;

    public ICommand NavigateCommand { get; }

    public string PrivilegeText => Loc.T(IsElevated ? "Priv_Admin" : "Priv_Standard");

    public bool IsElevated { get; }

    public string VersionText { get; } =
        "v" + (Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0] ?? "0.1.0");
}

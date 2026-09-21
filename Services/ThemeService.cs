using System.Windows;

namespace MazureTools.Services;

public interface IThemeService
{
    event EventHandler? ThemeChanged;

    void Apply(ThemeMode theme);
}

/// <summary>Swaps the colour dictionary at index 0 of the application resources; all styles use DynamicResource.</summary>
public sealed class ThemeService : IThemeService
{
    public event EventHandler? ThemeChanged;

    public void Apply(ThemeMode theme)
    {
        var source = new Uri($"pack://application:,,,/Themes/Colors.{theme}.xaml", UriKind.Absolute);
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var replacement = new ResourceDictionary { Source = source };

        var existing = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.Contains("Colors.", StringComparison.Ordinal) == true);
        if (existing is null)
            dictionaries.Insert(0, replacement);
        else
            dictionaries[dictionaries.IndexOf(existing)] = replacement;

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}

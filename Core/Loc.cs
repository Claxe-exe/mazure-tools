using System.Globalization;
using System.Windows;

namespace MazureTools.Core;

public enum AppLanguage
{
    English,
    Turkish
}

/// <summary>
/// Localisation facade. All texts live in <c>Languages/Strings.*.xaml</c>, so XAML uses
/// <c>{DynamicResource Key}</c> (switches live) and code uses <see cref="T(string)"/>.
/// A missing key returns the key itself, which makes gaps easy to spot.
/// </summary>
public static class Loc
{
    private const string DictionaryMarker = "Strings.";

    public static AppLanguage Current { get; private set; } = AppLanguage.English;

    /// <summary>Raised after the dictionary and the culture were switched. View models re-read their texts.</summary>
    public static event EventHandler? LanguageChanged;

    /// <summary>Picks the language for a first run from the Windows display language.</summary>
    public static AppLanguage Detect() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("tr", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Turkish
            : AppLanguage.English;

    public static string T(string key) =>
        (Application.Current?.TryFindResource(key) as string)?.Replace("\\n", "\n") ?? key;

    public static string T(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(key), args);

    public static void SetLanguage(AppLanguage language)
    {
        var app = Application.Current;
        var replacement = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Languages/Strings.{language}.xaml", UriKind.Absolute)
        };

        var dictionaries = app.Resources.MergedDictionaries;
        var existing = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.Contains(DictionaryMarker, StringComparison.Ordinal) == true);
        if (existing is null)
            dictionaries.Add(replacement);
        else
            dictionaries[dictionaries.IndexOf(existing)] = replacement;

        // Numbers, dates and units follow the chosen language too (12,5 GB vs 12.5 GB).
        var culture = CultureInfo.GetCultureInfo(language == AppLanguage.Turkish ? "tr-TR" : "en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        Current = language;
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }
}

/// <summary>A text that is stored as key + arguments and resolved when read, so it follows a language switch.</summary>
public readonly record struct LocText(string Key, object?[] Args)
{
    public static LocText Of(string key, params object?[] args) => new(key, args);

    public static readonly LocText Empty = new(string.Empty, []);

    public override string ToString() => string.IsNullOrEmpty(Key) ? string.Empty : Loc.T(Key, Args);
}

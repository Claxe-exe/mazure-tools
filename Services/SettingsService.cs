using System.Text.Json;
using System.Text.Json.Serialization;
using MazureTools.Core;

namespace MazureTools.Services;

public enum ThemeMode
{
    Dark,
    Light
}

public sealed class AppSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.Dark;

    /// <summary>First run follows the Windows display language; afterwards the user's choice is kept.</summary>
    public AppLanguage Language { get; set; } = Loc.Detect();

    /// <summary>Closing the window hides it to the tray instead of exiting.</summary>
    public bool CloseToTray { get; set; } = true;

    public bool TrayHintShown { get; set; }

    /// <summary>Shows Mazu, the mascot, on the desktop (bottom right, always on top).</summary>
    public bool ShowMascot { get; set; } = true;

    /// <summary>Where the user dragged Mazu to. Null = default corner.</summary>
    public double? MascotLeft { get; set; }

    public double? MascotTop { get; set; }
}

public interface ISettingsService
{
    AppSettings Current { get; }

    void Save();
}

/// <summary>Stores settings as JSON in %AppData%\MazureTools. A missing or corrupt file falls back to defaults.</summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MazureTools", "settings.json");

    public SettingsService()
    {
        Current = Load();
    }

    public AppSettings Current { get; }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(Current, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Settings are a convenience; failing to persist them must never break the app.
        }
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Fall through to defaults.
        }

        return new AppSettings();
    }
}

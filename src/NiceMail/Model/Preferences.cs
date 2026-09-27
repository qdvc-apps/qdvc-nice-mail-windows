using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qdvc.NiceMail.Model;

public enum ToolbarStyle { LabelsBesideIcons, LabelsBelowIcons }

/// <summary>
/// Per-user preferences, stored as JSON in %APPDATA%\QDVC\NiceMail\preferences.json.
/// </summary>
public sealed class Preferences
{
    public ToolbarStyle ToolbarStyle { get; set; } = ToolbarStyle.LabelsBesideIcons;
    public SkinTone SkinTone { get; set; } = SkinTone.None;
    /// <summary>Null means the built-in monospace default.</summary>
    public string? SignatureFontFamily { get; set; }
    public float SignatureFontSize { get; set; } = 10f;
    public bool ReopenLastWorkspace { get; set; } = true;
    public string? LastWorkspace { get; set; }

    // Session state remembered between runs.
    public bool IncludeDisclaimer { get; set; } = true;
    public bool RefOnly { get; set; }
    public string? Profile { get; set; }
    public string NoteAddress { get; set; } = "";
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
    public bool WindowMaximised { get; set; }

    [JsonIgnore]
    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QDVC", "NiceMail", "preferences.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static Preferences Load(string? path = null)
    {
        path ??= DefaultPath;
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path), Options) ?? new Preferences();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // A corrupt or unreadable preferences file should never stop the app starting.
        }
        return new Preferences();
    }

    public void Save(string? path = null)
    {
        path ??= DefaultPath;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, Options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Non-fatal: preferences simply won't persist this time.
        }
    }
}
